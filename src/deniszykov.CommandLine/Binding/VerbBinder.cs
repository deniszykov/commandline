using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using deniszykov.CommandLine.Parsing;
using deniszykov.TypeConversion;

namespace deniszykov.CommandLine.Binding
{
	internal sealed class VerbBinder
	{
		private readonly ITypeConversionProvider typeConversionProvider;
		private readonly IArgumentsParser parser;
		private readonly IServiceProvider serviceProvider;

		public StringComparison LongOptionNameMatchingMode { get; }
		public StringComparison ShortOptionNameMatchingMode { get; }
		public StringComparison VerbNameMatchingMode { get; }

		public VerbBinder(
			 CommandLineConfiguration configuration,
			 ITypeConversionProvider typeConversionProvider,
			 IServiceProvider serviceProvider)
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

		public void ProvideContext(Verb verb, object?[] verArguments, VerbExecutionContext context, CancellationToken cancellationToken)
		{
			if (verb == null) throw new ArgumentNullException(nameof(verb));
			if (verArguments == null) throw new ArgumentNullException(nameof(verArguments));
			if (context == null) throw new ArgumentNullException(nameof(context));

			foreach (var serviceParameter in verb.ServiceParameters)
			{
				var argumentIndex = serviceParameter.ArgumentIndex;
				if (serviceParameter.ValueType.AsType() == typeof(VerbExecutionContext))
				{
					verArguments[argumentIndex] = context;
				}
				if (serviceParameter.ValueType.AsType() == typeof(ICommandLineBuilder))
				{
					verArguments[argumentIndex] = CommandLine.CreateSubVerb(context);
				}
				else if (serviceParameter.ValueType.AsType() == typeof(CancellationToken))
				{
					verArguments[argumentIndex] = cancellationToken;
				}
			}
		}

		public VerbBindingResult Bind(VerbSet verbSet, string? defaultVerbName, string[] arguments)
		{
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));

			var result = default(VerbBindingResult);
			if (this.TryGetVerbName(arguments, out var verbName, out var isHelpRequested) && verbSet.FindVerb(verbName!) != null)
			{
				result = this.FindAndBindVerb(verbSet.Verbs, verbName!, arguments.Skip(1).ToArray());

				if (isHelpRequested && result is VerbBindingResult.FailedToBind)
				{
					return new VerbBindingResult.HelpRequested(verbName!);
				}
			}
			else if (string.IsNullOrEmpty(defaultVerbName) == false && !isHelpRequested)
			{
				result = this.FindAndBindVerb(verbSet.Verbs, defaultVerbName!, arguments);
			}
			else if (isHelpRequested)
			{
				return new VerbBindingResult.HelpRequested(string.IsNullOrEmpty(verbName) ? CommandLine.UNKNOWN_VERB_NAME : verbName!);
			}
			else if (!string.IsNullOrEmpty(verbName))
			{
				return new VerbBindingResult.FailedToBind(verbName!, VerbBindingResult.EmptyFailedMethodBindings);
			}
			else
			{
				return new VerbBindingResult.NoVerbSpecified();
			}

			return result;
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

				var getOptionArity = new Func<string, ValueArity?>(optionName => verb.FindBoundParameter(optionName, optionName.Length > 1 ? this.LongOptionNameMatchingMode : this.ShortOptionNameMatchingMode)?.ValueArity);
				var parsedArguments = this.parser.Parse(arguments, getOptionArity);

				if (parsedArguments.HasHelpOption && !verb.HasSubVerbs)
				{
					return new VerbBindingResult.HelpRequested(verb.Name);
				}

				var boundParameters = verb.BoundParameters;
				var serviceParameters = verb.ServiceParameters;
				var parameterBindings = new ParameterBindingResult[boundParameters.Count];
				var takenValues = new HashSet<int>();
				var isSuccessfulBinding = true;
				foreach (var parameter in boundParameters)
				{
					var bindResult = this.BindParameter(parameter, parsedArguments, takenValues);
					parameterBindings[parameter.Position] = bindResult;
					isSuccessfulBinding = isSuccessfulBinding && bindResult.IsSuccess;
				}

				if (isSuccessfulBinding == false)
				{
					failedVerbs.Add(verb, parameterBindings);
					continue;
				}

				var target = verb.TargetType != null ? this.ResolveService(verb, verb.TargetType.AsType(), isOptional: true) ?? Activator.CreateInstance(verb.TargetType.AsType()) : null;
				var verbArguments = new object?[boundParameters.Count + verb.ServiceParameters.Count];
				for (var i = 0; i < boundParameters.Count; i++)
				{
					var argumentIndex = parameterBindings[i].Parameter.ArgumentIndex;
					verbArguments[argumentIndex] = parameterBindings[i].Value;
				}

				foreach (var serviceParameter in serviceParameters)
				{
					if (serviceParameter.ValueType.AsType() == typeof(VerbExecutionContext) ||
						serviceParameter.ValueType.AsType() == typeof(ICommandLineBuilder) ||
						serviceParameter.ValueType.AsType() == typeof(CancellationToken))
					{
						continue; // late bound/special services
					}

					var argumentIndex = serviceParameter.ArgumentIndex;
					verbArguments[argumentIndex] = this.ResolveService(verb, serviceParameter.ValueType.AsType(), serviceParameter.IsOptional);
				}

				return new VerbBindingResult.Bound(verb, target, verbArguments, parsedArguments.HasHelpOption);
			}

			return new VerbBindingResult.FailedToBind(verbName, failedVerbs);
		}
		private ParameterBindingResult BindParameter(VerbParameter parameter, ParsedArguments parsedArguments, HashSet<int> takenValues)
		{
			if (parameter == null) throw new ArgumentNullException(nameof(parameter));
			if (takenValues == null) throw new ArgumentNullException(nameof(takenValues));

			var value = default(object);
			try
			{
				if (parsedArguments.TryGetLongOption(parameter.Name, out var optionValue) ||
					parsedArguments.TryGetShortOption(parameter.Alias ?? string.Empty, out optionValue) ||
					TryGetPositionalArgument(parameter, parsedArguments, takenValues, out optionValue))
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

		private bool TryGetVerbName(string[] arguments, out string? verbName, out bool isHelpRequested)
		{
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));

			var getOptionArity = new Func<string, ValueArity?>(name => ValueArity.ZeroOrOne);
			var parsedArguments = this.parser.Parse(arguments, getOptionArity);

			isHelpRequested = parsedArguments.HasHelpOption;

			if (parsedArguments.Values.Count > 0 &&
				arguments.Length > 0 &&
				string.Equals(parsedArguments.Values.First(), arguments[0], StringComparison.Ordinal))
			{
				verbName = arguments[0];
				return true;
			}

			verbName = default;
			return false;
		}
		private object? ResolveService(Verb verb, Type serviceType, bool isOptional)
		{
			var instance = this.serviceProvider.GetService(serviceType);
			if (instance == null && !isOptional)
			{
				throw CommandLineException.UnableToResolveService(verb, serviceType);
			}
			return instance;
		}
		// ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
		private object? Convert(object? value, Type toType, VerbParameter metadata)
		{
			if (toType == null) throw new ArgumentNullException(nameof(toType));
			if (metadata == null) throw new ArgumentNullException(nameof(metadata));

			var conversionError = default(Exception);
#if !NETSTANDARD1_6
			try
			{
				if (value == null)
				{
					return null;
				}
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

		private static bool TryGetPositionalArgument(VerbParameter parameter, ParsedArguments parsedArguments, HashSet<int> takenValues, out OptionValue optionValue)
		{
			if (parameter.IsValueCollector)
			{
				var restValues = parsedArguments.Values.Where((value, index) => !takenValues.Contains(index)).ToArray();
				optionValue = new OptionValue(restValues, 1);
				return true;
			}

			if (parsedArguments.TryGetValue(parameter.Position, out optionValue))
			{
				takenValues.Add(parameter.Position);
				return true;
			}

			return false;
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
