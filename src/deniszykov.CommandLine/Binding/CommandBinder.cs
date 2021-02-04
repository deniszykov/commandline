using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using deniszykov.CommandLine.Parsing;
using deniszykov.TypeConversion;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Binding
{
	internal sealed class CommandBinder
	{
		[NotNull] private readonly ITypeConversionProvider typeConversionProvider;
		[NotNull] private readonly IArgumentsParser parser;
		[NotNull] private readonly IServiceProvider serviceProvider;

		public StringComparison LongOptionNameMatchingMode { get; }
		public StringComparison ShortOptionNameMatchingMode { get; }

		public CommandBinder(
			[NotNull] CommandLineConfiguration configuration,
			[NotNull] ITypeConversionProvider typeConversionProvider,
			[NotNull] IArgumentsParser parser,
			[NotNull] IServiceProvider serviceProvider)
		{
			if (configuration == null) throw new ArgumentNullException(nameof(configuration));
			if (parser == null) throw new ArgumentNullException(nameof(parser));
			if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
			if (typeConversionProvider == null) throw new ArgumentNullException(nameof(typeConversionProvider));

			this.LongOptionNameMatchingMode = configuration.LongOptionNameMatchingMode;
			this.ShortOptionNameMatchingMode = configuration.ShortOptionNameMatchingMode;
			this.typeConversionProvider = typeConversionProvider;
			this.parser = parser;
			this.serviceProvider = serviceProvider;
		}

		public CommandBindingResult Bind(CommandSet commandSet, string defaultCommandName, string[] arguments)
		{
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));

			var commandName = default(string);
			var bestMatchResult = default(CommandBindingResult);
			if (arguments.Length > 0)
			{
				commandName = this.typeConversionProvider.ConvertToString(arguments[0]);
				var result = this.FindAndBindCommand(commandSet.Commands, commandName, arguments.Skip(1).ToArray());
				if (result.IsSuccess)
					return result;

				bestMatchResult = result;
			}

			if (string.IsNullOrEmpty(defaultCommandName) == false)
			{
				commandName = defaultCommandName;
				var result = this.FindAndBindCommand(commandSet.Commands, defaultCommandName, arguments);
				if (result.IsSuccess)
				{
					return result;
				}

				bestMatchResult ??= result;
			}

			if (string.IsNullOrEmpty(commandName))
			{
				commandName = CommandLine.UnknownMethodName;
			}

			bestMatchResult ??= new CommandBindingResult(commandName, CommandBindingResult.EmptyFailedMethodBindings);

			return bestMatchResult;
		}
		public void ProvideContext(CommandBindingResult bindResult, CommandExecutionContext context, CancellationToken cancellationToken)
		{
			if (bindResult == null) throw new ArgumentNullException(nameof(bindResult));
			if (context == null) throw new ArgumentNullException(nameof(context));

			var command = bindResult.Command;
			var commandArguments = bindResult.Arguments;
			foreach (var serviceParameter in command.ServiceParameters)
			{
				var argumentIndex = serviceParameter.ArgumentIndex;
				if (serviceParameter.ValueType.AsType() == typeof(CommandExecutionContext))
				{
					commandArguments[argumentIndex] = context;
				}
				else if (serviceParameter.ValueType.AsType() == typeof(CancellationToken))
				{
					commandArguments[argumentIndex] = cancellationToken;
				}
			}
		}


		private CommandBindingResult FindAndBindCommand(IReadOnlyCollection<Command> commands, string commandName, string[] arguments)
		{
			if (commands == null) throw new ArgumentNullException(nameof(commands));
			if (commandName == null) throw new ArgumentNullException(nameof(commandName));
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));

			var failedCommands = new Dictionary<Command, ParameterBindingResult[]>();
			foreach (var command in commands)
			{
				if (!string.Equals(commandName, command.Name))
				{
					continue;
				}

				var getOptionArity = new Func<string, ParameterValueArity?>(optionName => command.FindBoundParameter(optionName, optionName.Length > 1 ? LongOptionNameMatchingMode : ShortOptionNameMatchingMode)?.ValueArity);
				var parsedArguments = this.parser.Parse(arguments, getOptionArity);
				var boundParameters = command.BoundParameters;
				var serviceParameters = command.ServiceParameters;
				var parameterBindings = new ParameterBindingResult[boundParameters.Count];
				var isSuccessfulBinding = true;
				foreach (var parameter in boundParameters)
				{
					var bindResult = BindParameter(parameter, parsedArguments);
					parameterBindings[parameter.Position] = bindResult;
					isSuccessfulBinding = isSuccessfulBinding && bindResult.IsSuccess;
				}

				if (isSuccessfulBinding == false)
				{
					failedCommands.Add(command, parameterBindings);
					continue;
				}

				var target = command.TargetType != null ? this.ResolveService(command, command.TargetType.AsType(), isOptional: false) : null;
				var commandArguments = new object[boundParameters.Count + command.ServiceParameters.Count];
				for (var i = 0; i < boundParameters.Count; i++)
				{
					var argumentIndex = parameterBindings[i].Parameter.ArgumentIndex;
					commandArguments[argumentIndex] = parameterBindings[i].Value;
				}

				foreach (var serviceParameter in serviceParameters)
				{
					if (serviceParameter.ValueType.AsType() == typeof(CommandExecutionContext) ||
						serviceParameter.ValueType.AsType() == typeof(CancellationToken))
					{
						continue;
					}

					var argumentIndex = serviceParameter.ArgumentIndex;
					commandArguments[argumentIndex] = this.ResolveService(command, serviceParameter.ValueType.AsType(), serviceParameter.IsOptional);
				}

				return new CommandBindingResult(command, target, commandArguments);
			}

			return new CommandBindingResult(commandName, failedCommands);
		}
		private ParameterBindingResult BindParameter(CommandParameter parameter, ParsedArguments parsedArguments)
		{
			var value = default(object);
			try
			{
				if (parsedArguments.TryGetLongOption(parameter.Name, out var optionValue) ||
					parsedArguments.TryGetShortOption(parameter.Alias ?? string.Empty, out optionValue) ||
					parsedArguments.TryGetValue(parameter.Position, out optionValue))
				{
					var raw = optionValue.Raw;

					if (IsArityMatching(parameter.ValueArity, raw.Count) == false)
					{
						switch (raw.Count)
						{
							case 0: throw new InvalidOperationException("Option requires an argument.");
							case 1: throw new InvalidOperationException("Option requires no arguments.");
							default: throw new InvalidOperationException("Option requires at most one argument.");
						}
					}

					if (parameter.ValueType.AsType() == typeof(OptionCount))
					{
						value = new OptionCount(optionValue.Count);
					}
					else if (parameter.ValueType.IsInstanceOfType(raw))
					{
						value = raw;
					}
					else if (parameter.ValueType.IsGenericType && parameter.ValueType.GetGenericTypeDefinition() == typeof(List<>))
					{
						var elementType = parameter.ValueType.GetGenericArguments()[0];
						var values = (IList)Activator.CreateInstance(parameter.ValueType.AsType());
						for (var i = 0; i < values.Count; i++)
						{
							var item = this.Convert(raw.ElementAt(i), elementType, parameter);
							values.Add(item);
						}
						value = values;
					}
					else if (parameter.ValueType.IsArray)
					{
						var elementType = parameter.ValueType.GetElementType() ?? parameter.ValueType.AsType();
						var values = Array.CreateInstance(elementType, raw.Count);
						for (var i = 0; i < values.Length; i++)
						{
							var item = this.Convert(raw.ElementAt(i), elementType, parameter);
							values.SetValue(item, i);
						}
						value = values;
					}
					else if (raw.Count == 0 && parameter.ValueType.AsType() == typeof(bool))
					{
						value = true;
					}
					else if (raw.Count == 0)
					{
						value = parameter.DefaultValue;
					}
					else if (parameter.ValueType.IsEnum)
					{
						value = string.Join(",", raw);
					}
					else if (parameter.ValueType.AsType() == typeof(string))
					{
						value = string.Join(" ", raw);
					}
				}
				else if (parameter.IsOptional)
				{
					value = parameter.DefaultValue;
				}
				else if (parameter.ValueType.AsType() == typeof(bool))
				{
					value = false;
				}
				else if (parameter.ValueType.AsType() == typeof(OptionCount))
				{
					value = new OptionCount(0);
				}
				else
				{
					throw new InvalidOperationException("Missing required option.");
				}
			}
			catch (Exception bindingError)
			{
				return new ParameterBindingResult(parameter, bindingError, value);
			}

			if (value != null && value.GetType() != parameter.ValueType.AsType())
				value = this.typeConversionProvider.Convert(value.GetType(), parameter.ValueType.AsType(), value);

			return new ParameterBindingResult(parameter, null, value);
		}

		private object ResolveService(Command command, Type serviceType, bool isOptional)
		{
			var instance = this.serviceProvider.GetService(serviceType);
			if (instance == null && !isOptional)
			{
				throw CommandLineException.UnableToResolveService(command, serviceType);
			}
			return instance;
		}
		private object Convert(object value, Type toType, CommandParameter metadata)
		{
			if (toType == null) throw new ArgumentNullException(nameof(toType));
			if (metadata == null) throw new ArgumentNullException(nameof(metadata));

			var conversionError = default(Exception);
#if !NETSTANDARD1_6
			try
			{
				if (metadata.TypeConverter != null)
				{
					if (metadata.TypeConverter.CanConvertFrom(value.GetType()))
						return metadata.TypeConverter.ConvertFrom(value);
				}
			}
			catch (Exception error)
			{
				conversionError = error;
			}
#endif

			if (value != null && this.typeConversionProvider.TryConvert(value.GetType(), toType, value, out var result))
			{
				return result;
			}

			if (conversionError != null)
			{
				throw new InvalidOperationException($"Unable to convert '{value ?? "<null>"}' to type '{toType.FullName}': {conversionError.Message}", conversionError);
			}
			else
			{
				throw new InvalidOperationException($"Unable to convert '{value ?? "<null>"}' to type '{toType.FullName}'.");
			}
		}

		private static bool IsArityMatching(ParameterValueArity parameterValueArity, int rawCount)
		{
			switch (parameterValueArity)
			{
				case ParameterValueArity.Zero: return rawCount == 0;
				case ParameterValueArity.ZeroOrOne: return rawCount <= 1;
				case ParameterValueArity.One: return rawCount == 1;
				case ParameterValueArity.ZeroOrMany: return rawCount > 0;
				case ParameterValueArity.OneOrMany: return rawCount > 1;
				default: throw new ArgumentOutOfRangeException(nameof(parameterValueArity), parameterValueArity, null);
			}
		}

	}
}
