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
	internal sealed class VerbBinder
	{
		[NotNull] private readonly ITypeConversionProvider typeConversionProvider;
		[NotNull] private readonly IArgumentsParser parser;
		[NotNull] private readonly IServiceProvider serviceProvider;

		public StringComparison LongOptionNameMatchingMode { get; }
		public StringComparison ShortOptionNameMatchingMode { get; }
		public StringComparison VerbNameMatchingMode { get; }

		public VerbBinder(
			[NotNull] CommandLineConfiguration configuration,
			[NotNull] ITypeConversionProvider typeConversionProvider,
			[NotNull] IServiceProvider serviceProvider)
		{
			if (configuration == null) throw new ArgumentNullException(nameof(configuration));
			if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
			if (typeConversionProvider == null) throw new ArgumentNullException(nameof(typeConversionProvider));

			this.LongOptionNameMatchingMode = configuration.LongOptionNameMatchingMode;
			this.ShortOptionNameMatchingMode = configuration.ShortOptionNameMatchingMode;
			this.VerbNameMatchingMode = configuration.VerbNameMatchingMode;
			this.typeConversionProvider = typeConversionProvider;
			this.parser = new GetOptParser(configuration);
			this.serviceProvider = serviceProvider;
		}

		public VerbBindingResult Bind(VerbSet verbSet, string defaultVerbName, string[] arguments)
		{
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));

			var verbName = default(string);
			var bestMatchResult = default(VerbBindingResult);
			if (arguments.Length > 0)
			{
				verbName = this.typeConversionProvider.ConvertToString(arguments[0]);
				var result = this.FindAndBindVerb(verbSet.Verbs, verbName, arguments.Skip(1).ToArray());
				if (result.IsSuccess)
					return result;

				bestMatchResult = result;
			}

			if (string.IsNullOrEmpty(defaultVerbName) == false)
			{
				verbName = defaultVerbName;
				var result = this.FindAndBindVerb(verbSet.Verbs, defaultVerbName, arguments);
				if (result.IsSuccess)
				{
					return result;
				}

				bestMatchResult = result;
			}

			if (string.IsNullOrEmpty(verbName))
			{
				verbName = CommandLine.UnknownVerbName;
			}

			bestMatchResult ??= new VerbBindingResult(verbName, VerbBindingResult.EmptyFailedMethodBindings);

			return bestMatchResult;
		}
		public void ProvideContext(VerbBindingResult bindResult, VerbExecutionContext context, CancellationToken cancellationToken)
		{
			if (bindResult == null) throw new ArgumentNullException(nameof(bindResult));
			if (context == null) throw new ArgumentNullException(nameof(context));

			var verb = bindResult.Verb;
			var verArguments = bindResult.Arguments;
			foreach (var serviceParameter in verb.ServiceParameters)
			{
				var argumentIndex = serviceParameter.ArgumentIndex;
				if (serviceParameter.ValueType.AsType() == typeof(VerbExecutionContext))
				{
					verArguments[argumentIndex] = context;
				}
				else if (serviceParameter.ValueType.AsType() == typeof(CancellationToken))
				{
					verArguments[argumentIndex] = cancellationToken;
				}
			}
		}

		private VerbBindingResult FindAndBindVerb(IReadOnlyCollection<Verb> verbs, string verbName, string[] arguments)
		{
			if (verbs == null) throw new ArgumentNullException(nameof(verbs));
			if (verbName == null) throw new ArgumentNullException(nameof(verbName));
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));

			var failedVerbs = new Dictionary<Verb, ParameterBindingResult[]>();
			foreach (var verb in verbs)
			{
				if (!string.Equals(verbName, verb.Name, this.VerbNameMatchingMode))
				{
					continue;
				}

				var getOptionArity = new Func<string, ValueArity?>(optionName => verb.FindBoundParameter(optionName, optionName.Length > 1 ? LongOptionNameMatchingMode : ShortOptionNameMatchingMode)?.ValueArity);
				var parsedArguments = this.parser.Parse(arguments, getOptionArity);
				var boundParameters = verb.BoundParameters;
				var serviceParameters = verb.ServiceParameters;
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
					failedVerbs.Add(verb, parameterBindings);
					continue;
				}

				var target = verb.TargetType != null ? this.ResolveService(verb, verb.TargetType.AsType(), isOptional: false) : null;
				var verbArguments = new object[boundParameters.Count + verb.ServiceParameters.Count];
				for (var i = 0; i < boundParameters.Count; i++)
				{
					var argumentIndex = parameterBindings[i].Parameter.ArgumentIndex;
					verbArguments[argumentIndex] = parameterBindings[i].Value;
				}

				foreach (var serviceParameter in serviceParameters)
				{
					if (serviceParameter.ValueType.AsType() == typeof(VerbExecutionContext) ||
						serviceParameter.ValueType.AsType() == typeof(CancellationToken))
					{
						continue;
					}

					var argumentIndex = serviceParameter.ArgumentIndex;
					verbArguments[argumentIndex] = this.ResolveService(verb, serviceParameter.ValueType.AsType(), serviceParameter.IsOptional);
				}

				return new VerbBindingResult(verb, target, verbArguments);
			}

			return new VerbBindingResult(verbName, failedVerbs);
		}
		private ParameterBindingResult BindParameter(VerbParameter parameter, ParsedArguments parsedArguments)
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
					else
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

				if (value != null && value.GetType() != parameter.ValueType.AsType())
					value = this.typeConversionProvider.Convert(value.GetType(), parameter.ValueType.AsType(), value);
			}
			catch (Exception bindingError)
			{
				return new ParameterBindingResult(parameter, bindingError, value);
			}

			return new ParameterBindingResult(parameter, null, value);
		}

		private object ResolveService(Verb verb, Type serviceType, bool isOptional)
		{
			var instance = this.serviceProvider.GetService(serviceType);
			if (instance == null && !isOptional)
			{
				throw CommandLineException.UnableToResolveService(verb, serviceType);
			}
			return instance;
		}
		private object Convert(object value, Type toType, VerbParameter metadata)
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

		private static bool IsArityMatching(ValueArity parameterValueArity, int rawCount)
		{
			switch (parameterValueArity)
			{
				case ValueArity.Zero: return rawCount == 0;
				case ValueArity.ZeroOrOne: return rawCount <= 1;
				case ValueArity.One: return rawCount == 1;
				case ValueArity.ZeroOrMany: return rawCount >= 0;
				case ValueArity.OneOrMany: return rawCount >= 1;
				default: throw new ArgumentOutOfRangeException(nameof(parameterValueArity), parameterValueArity, null);
			}
		}

	}
}
