using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using deniszykov.TypeConversion;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Binding
{
	internal sealed class CommandBinder
	{
		[NotNull] private readonly ITypeConversionProvider typeConversionProvider;
		[NotNull] private readonly IServiceProvider serviceProvider;

		public CommandBinder(
			[NotNull] ITypeConversionProvider typeConversionProvider,
			[NotNull] IServiceProvider serviceProvider)
		{
			if (typeConversionProvider == null) throw new ArgumentNullException(nameof(typeConversionProvider));

			this.typeConversionProvider = typeConversionProvider;
			this.serviceProvider = serviceProvider;
		}

		public CommandBindingResult Bind(CommandSet commandSet, string defaultMethodName, CommandLineArguments arguments)
		{
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));

			var methodName = default(string);
			var bestMatchResult = default(CommandBindingResult);
			if (arguments.ContainsKey("0"))
			{
				var newArguments = new CommandLineArguments(this.typeConversionProvider, arguments);
				methodName = this.typeConversionProvider.ConvertToString(newArguments["0"]);
				newArguments.RemoveAt(0);
				var result = this.FindAndBindCommand(commandSet.Commands, methodName, newArguments);
				if (result.IsSuccess)
					return result;

				bestMatchResult = result;
			}

			if (string.IsNullOrEmpty(defaultMethodName) == false)
			{
				methodName = defaultMethodName;
				var result = this.FindAndBindCommand(commandSet.Commands, defaultMethodName, arguments);
				if (result.IsSuccess)
				{
					return result;
				}

				bestMatchResult ??= result;
			}

			if (string.IsNullOrEmpty(methodName))
			{
				methodName = CommandLine.UnknownMethodName;
			}

			bestMatchResult ??= new CommandBindingResult(methodName, CommandBindingResult.EmptyFailedMethodBindings);

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
				if (serviceParameter.ValueType == typeof(CommandExecutionContext))
				{
					commandArguments[argumentIndex] = context;
				}
				else if (serviceParameter.ValueType == typeof(CancellationToken))
				{
					commandArguments[argumentIndex] = cancellationToken;
				}
			}
		}


		private CommandBindingResult FindAndBindCommand(IReadOnlyCollection<Command> commands, string methodName, CommandLineArguments arguments)
		{
			if (commands == null) throw new ArgumentNullException(nameof(commands));
			if (methodName == null) throw new ArgumentNullException(nameof(methodName));
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));

			var failedCommands = new Dictionary<Command, ParameterBindingResult[]>();
			foreach (var command in commands)
			{
				var boundParameters = command.BoundParameters;
				var serviceParameters = command.ServiceParameters;
				var parameterBindings = new ParameterBindingResult[boundParameters.Count];
				var isSuccessfulBinding = true;
				foreach (var parameter in boundParameters)
				{
					var bindResult = BindParameter(parameter, arguments);
					parameterBindings[parameter.Position] = bindResult;
					isSuccessfulBinding = isSuccessfulBinding && bindResult.IsSuccess;
				}

				if (isSuccessfulBinding == false)
				{
					failedCommands.Add(command, parameterBindings);
					continue;
				}

				var target = command.TargetType != null ? this.ResolveService(command, command.TargetType, isOptional: false) : null;
				var commandArguments = new object[boundParameters.Count + command.ServiceParameters.Count];
				for (var i = 0; i < boundParameters.Count; i++)
				{
					var argumentIndex = parameterBindings[i].Parameter.ArgumentIndex;
					commandArguments[argumentIndex] = parameterBindings[i].Value;
				}

				foreach (var serviceParameter in serviceParameters)
				{
					if (serviceParameter.ValueType == typeof(CommandExecutionContext) ||
						serviceParameter.ValueType == typeof(CancellationToken))
					{
						continue;
					}

					var argumentIndex = serviceParameter.ArgumentIndex;
					commandArguments[argumentIndex] = this.ResolveService(command, serviceParameter.ValueType, serviceParameter.IsOptional);
				}

				return new CommandBindingResult(command, target, commandArguments);
			}

			return new CommandBindingResult(methodName, failedCommands);
		}
		private ParameterBindingResult BindParameter(CommandParameter parameter, CommandLineArguments arguments)
		{
			var expectedType = parameter.ValueType;
			var value = default(object);
			try
			{
				if (expectedType == typeof(CommandLineArguments))
				{
					value = new CommandLineArguments(this.typeConversionProvider, arguments);
				}
				else if (arguments.TryGetValue(parameter.Name, out value) || arguments.TryGetValue((parameter.Position).ToString(), out value))
				{
					var valueStringList = value as IList<string>;
					if (expectedType.IsArray)
					{
						var elemType = expectedType.GetElementType();
						Debug.Assert(elemType != null, "elemType != null");

						if (valueStringList != null)
						{
							var values = Array.CreateInstance(elemType, valueStringList.Count);
							for (var i = 0; i < valueStringList.Count; i++)
							{
								value = valueStringList[i]; // used on failure in exception block
								values.SetValue(Convert(valueStringList[i], elemType, parameter), i);
							}
							value = values;
						}
						else if (value != null)
						{
							var values = Array.CreateInstance(elemType, 1);
							values.SetValue(Convert(value, elemType, parameter), 0);
							value = values;
						}
						else
						{
							var values = Array.CreateInstance(elemType, 0);
							value = values;
						}
					}
					else if (expectedType == typeof(bool) && value == null)
					{
						value = true;
					}
					else if (value == null)
					{
						value = parameter.IsOptional ? parameter.DefaultValue : expectedType.IsValueType ? Activator.CreateInstance(expectedType) : null;
					}
					else if (valueStringList != null && expectedType.IsEnum)
					{
						var values = new object[valueStringList.Count];
						for (var i = 0; i < valueStringList.Count; i++)
						{
							value = valueStringList[i]; // used on failure in exception block
							values[i] = Convert(value, expectedType, parameter);
						}

						if (Enum.GetUnderlyingType(expectedType).IsSignedNumber())
						{
							var combinedValue = 0L;
							foreach (var enumValue in values)
							{
								value = enumValue; // used on failure in exception block
								combinedValue |= this.typeConversionProvider.Convert<object, long>(value);
							}

							value = Enum.ToObject(expectedType, combinedValue);
						}
						else
						{
							var combinedValue = 0UL;
							foreach (var enumValue in values)
							{
								value = enumValue; // used on failure in exception block
								combinedValue |= this.typeConversionProvider.Convert<object, ulong>(enumValue);
							}
							value = Enum.ToObject(expectedType, combinedValue);
						}
					}
					else if (valueStringList != null && expectedType.IsAssignableFrom(typeof(IList<string>)) == false)
					{
						throw new FormatException($"Parameter has {valueStringList.Count} values while only one is expected.");
					}
					else
					{
						value = Convert(value, expectedType, parameter);
					}
				}
				else if (parameter.IsOptional)
				{
					value = parameter.DefaultValue;
				}
				else if (expectedType == typeof(bool))
				{
					value = false;
				}
				else
				{
					throw new InvalidOperationException("Missing parameter value.");
				}
			}
			catch (Exception bindingError)
			{
				return new ParameterBindingResult(parameter, bindingError, value);
			}

			if (value != null && value.GetType() != parameter.ValueType)
				value = this.typeConversionProvider.Convert(value.GetType(), parameter.ValueType, value);

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
#if !NETSTANDARD1_3
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

	}
}
