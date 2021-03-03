/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using deniszykov.CommandLine.Annotations;
using deniszykov.CommandLine.Binding;
using deniszykov.TypeConversion;

namespace deniszykov.CommandLine.Formatting
{
	internal sealed class HelpFormatter // TODO support XML Documentation lookup
	{
		private static readonly string[] EmptyValues = new string[0];
		private static readonly char[] EmptyChars = new char[0];

		private readonly IConsole console;
		private readonly IHelpTextProvider helpTextProvider;
		private readonly ITypeConversionProvider typeConversionProvider;

		private readonly StringComparison verbNameMatchingMode;
		private readonly string shortOptionNamePrefix;
		private readonly string longOptionNamePrefix;
		private readonly char optionArgumentSplitter;
		private readonly bool detailedBindFailureMessage;
		private readonly int maxOutputWidth;

		public HelpFormatter(
			 CommandLineConfiguration configuration,
			 IConsole console,
			 IHelpTextProvider helpTextProvider,
			 ITypeConversionProvider typeConversionProvider)
		{
			if (configuration == null) throw new ArgumentNullException(nameof(configuration));
			if (console == null) throw new ArgumentNullException(nameof(console));
			if (helpTextProvider == null) throw new ArgumentNullException(nameof(helpTextProvider));
			if (typeConversionProvider == null) throw new ArgumentNullException(nameof(typeConversionProvider));

			this.console = console;
			this.helpTextProvider = helpTextProvider;
			this.typeConversionProvider = typeConversionProvider;
			this.verbNameMatchingMode = configuration.VerbNameMatchingMode;
			this.shortOptionNamePrefix = (configuration.ShortOptionNamePrefixes ?? EmptyValues).DefaultIfEmpty("").First();
			this.longOptionNamePrefix = (configuration.LongOptionNamePrefixes ?? EmptyValues).DefaultIfEmpty("").First();
			this.optionArgumentSplitter = (configuration.OptionArgumentSplitters ?? EmptyChars).DefaultIfEmpty(' ').First();
			this.detailedBindFailureMessage = configuration.OutputDetailedErrors;
			this.maxOutputWidth = configuration.MaxOutputWidth;
		}

		public void VerbDescription(VerbSet verbSet, Verb foundVerb, IList<Verb> callChain)
		{
			if (verbSet == null) throw new ArgumentNullException(nameof(verbSet));
			if (foundVerb == null) throw new ArgumentNullException(nameof(foundVerb));
			if (callChain == null) throw new ArgumentNullException(nameof(callChain));

			var writer = new IndentedWriter(Environment.NewLine, this.maxOutputWidth);
			if (!string.IsNullOrEmpty(this.helpTextProvider.HelpHeaderText))
			{
				writer.WriteLine(this.helpTextProvider.HelpHeaderText);
				writer.WriteLine();
			}
			using (writer.KeepIndent("  "))
			{
				var namePrefix = GetNamePrefix(callChain, this.verbNameMatchingMode);
				foreach (var verb in verbSet.GetNonHiddenVerbs())
				{
					if (string.Equals(verb.Name, foundVerb.Name, this.verbNameMatchingMode) == false)
						continue;

					writer.WriteLine(this.helpTextProvider.VerbUsageText);
					using (writer.KeepIndent("  "))
					{
						writer.Write($"{namePrefix}{GetVerbName(verb.Name, this.verbNameMatchingMode)} ");
						using (writer.KeepIndent())
						{
							var parameterUsage = string.Join(" ", verb.GetNonHiddenBoundParameter().Select(this.GetParameterUsage));
							writer.WriteLine(parameterUsage);
						}

						writer.WriteLine();
					}

					if (!string.IsNullOrEmpty(verb.Description))
					{
						using (writer.KeepIndent("  "))
						{
							writer.WriteLine(verb.Description);
							writer.WriteLine();
						}
					}

					if (verb.GetNonHiddenBoundParameter().Any())
					{
						var maxNameLength = verb.GetNonHiddenBoundParameter().Max(param => this.GetParameterNames(param).Length);

						writer.WriteLine(this.helpTextProvider.VerbOptionsText);
						using (writer.KeepIndent("  "))
						{
							foreach (var parameter in verb.GetNonHiddenBoundParameter())
							{
								var parameterType = UnwrapType(parameter.ValueType.AsType());

								var variantsText = "";
								var defaultValueText = "";
								if (parameterType.GetTypeInfo().IsEnum && parameter.ValueType.IsFlags())
								{
									variantsText = string.Format(this.helpTextProvider.ParameterTypeCombinationOfText,
										string.Join("', '", Enum.GetNames(parameterType)));
								}
								else if (parameterType.GetTypeInfo().IsEnum)
								{
									variantsText = string.Format(this.helpTextProvider.ParameterTypeOneOfText, string.Join("', '", Enum.GetNames(parameterType)));
								}

								if (parameter.IsOptional && parameter.DefaultValue != null)
								{
									defaultValueText = string.Format(this.helpTextProvider.ParameterTypeDefaultValueText,
										this.typeConversionProvider.ConvertToString(parameter.DefaultValue));
								}

								var paramName = GetPaddedString(this.GetParameterNames(parameter), maxNameLength);
								var paramDescriptionText = parameter.Description;
								if (paramDescriptionText.Length > 0 && paramDescriptionText.EndsWith(".", StringComparison.OrdinalIgnoreCase) == false)
									paramDescriptionText += ".";

								writer.Write(paramName);
								using (writer.KeepIndent("    "))
								{
									writer.WriteLine(paramDescriptionText + defaultValueText + variantsText);
								}
							}
						}
						writer.WriteLine();
					}
				}
			}
			if (!string.IsNullOrEmpty(this.helpTextProvider.HelpFooterText))
			{
				writer.WriteLine(this.helpTextProvider.HelpFooterText);
			}
			this.console.WriteLine(writer);
		}
		public void VerbNotFound(VerbSet verbSet, string verbToDescribe, IList<Verb> callChain)
		{
			var writer = new IndentedWriter(Environment.NewLine, this.maxOutputWidth);
			if (!string.IsNullOrEmpty(this.helpTextProvider.HelpHeaderText))
			{
				writer.WriteLine(this.helpTextProvider.HelpHeaderText);
				writer.WriteLine();
			}
			using (writer.KeepIndent("  "))
			{
				writer.WriteLine(this.helpTextProvider.NotFoundErrorText);
				using (writer.KeepIndent("  "))
				{
					writer.WriteLine(string.Format(this.helpTextProvider.NotFoundMessageText, verbToDescribe));
					writer.WriteLine();
				}

				var namePrefix = GetNamePrefix(callChain.Take(Math.Max(0, callChain.Count - 1)), this.verbNameMatchingMode);
				var currentVerb = callChain.LastOrDefault();

				if (verbSet.GetNonHiddenVerbs().Any(verb => verb != currentVerb))
				{
					writer.WriteLine(this.helpTextProvider.NotFoundAvailableVerbsText);
					using (writer.KeepIndent("  "))
					{
						var maxVerbNameLength = verbSet.GetNonHiddenVerbs().Max(verb => verb.Name.Length);
						foreach (var verb in verbSet.GetNonHiddenVerbs())
						{
							if (verb == currentVerb)
							{
								continue;
							}

							var verbName = GetPaddedString(GetVerbName(verb.Name, this.verbNameMatchingMode), maxVerbNameLength);
							writer.Write($"{namePrefix}{verbName}");
							using (writer.KeepIndent("    "))
							{
								writer.WriteLine(verb.Description);
							}
						}
					}
					writer.WriteLine();
				}
			}
			if (!string.IsNullOrEmpty(this.helpTextProvider.HelpFooterText))
			{
				writer.WriteLine(this.helpTextProvider.HelpFooterText);
			}
			this.console.WriteLine(writer);
		}
		public void VerbList(VerbSet verbSet, IList<Verb> callChain, bool includeTypeHelpText)
		{
			var writer = new IndentedWriter(Environment.NewLine, this.maxOutputWidth);
			if (!string.IsNullOrEmpty(this.helpTextProvider.HelpHeaderText))
			{
				writer.WriteLine(this.helpTextProvider.HelpHeaderText);
				writer.WriteLine();
			}
			using (writer.KeepIndent("  "))
			{
				if (includeTypeHelpText)
				{
					writer.WriteLine(verbSet.Description);
					writer.WriteLine();
				}

				if (verbSet.GetNonHiddenVerbs().Any())
				{
					writer.WriteLine(this.helpTextProvider.VerbSetAvailableVerbsText);

					var verbPrefix = GetNamePrefix(callChain, this.verbNameMatchingMode);
					var maxVerbNameLength = verbSet.GetNonHiddenVerbs().Max(verb => verb.Name.Length);

					using (writer.KeepIndent("  "))
					{
						foreach (var verb in verbSet.GetNonHiddenVerbs())
						{
							var verbName = GetPaddedString(GetVerbName(verb.Name, this.verbNameMatchingMode), maxVerbNameLength);
							writer.Write($"{verbPrefix}{verbName}");
							using (writer.KeepIndent("    "))
							{
								writer.WriteLine(verb.Description);
							}
						}
					}
					writer.WriteLine();
				}
			}
			if (!string.IsNullOrEmpty(this.helpTextProvider.HelpFooterText))
			{
				writer.WriteLine(this.helpTextProvider.HelpFooterText);
			}
			this.console.WriteLine(writer);
		}
		public void InvalidVerbParameters(Verb verb, ParameterBindingResult[] parameters, Exception error)
		{
			var writer = new IndentedWriter(Environment.NewLine, this.maxOutputWidth);
			if (!string.IsNullOrEmpty(this.helpTextProvider.HelpHeaderText))
			{
				writer.WriteLine(this.helpTextProvider.HelpHeaderText);
				writer.WriteLine();
			}
			using (writer.KeepIndent("  "))
			{
				writer.WriteLine(this.helpTextProvider.InvalidVerbOptionsErrorText);

				using (writer.KeepIndent("  "))
				{
					writer.WriteLine(string.Format(this.helpTextProvider.InvalidVerbOptionsMessageText, verb.Name));
				}

				var failedBindParameters = parameters.Where(result => !result.IsSuccess).ToList();
				if (failedBindParameters.Count > 0)
				{
					var maxNameLength = failedBindParameters.Max(bindingResult => this.GetParameterNames(bindingResult.Parameter).Length);

					writer.WriteLine();
					writer.WriteLine(this.helpTextProvider.InvalidVerbOptionsText);
					using (writer.KeepIndent("  "))
					{
						foreach (var bindingResult in failedBindParameters)
						{
							var paramName = GetPaddedString(this.GetParameterNames(bindingResult.Parameter), maxNameLength);
							writer.Write(paramName);
							using (writer.KeepIndent("    "))
							{
								writer.WriteLine(bindingResult.Error?.Message);
							}
						}
					}
					writer.WriteLine();
				}
			}
			if (!string.IsNullOrEmpty(this.helpTextProvider.HelpFooterText))
			{
				writer.WriteLine(this.helpTextProvider.HelpFooterText);
			}

			this.console.WriteLine(writer);

			if (this.detailedBindFailureMessage)
			{
				this.console.WriteErrorLine(error);
				this.console.WriteErrorLine();
			}
		}

		public void VerbNotSpecified(VerbSet verbSet, IList<Verb> callChain)
		{
			var writer = new IndentedWriter(Environment.NewLine, this.maxOutputWidth);
			if (!string.IsNullOrEmpty(this.helpTextProvider.HelpHeaderText))
			{
				writer.WriteLine(this.helpTextProvider.HelpHeaderText);
				writer.WriteLine();
			}
			using (writer.KeepIndent("  "))
			{
				writer.WriteLine(this.helpTextProvider.VerbNotSpecifiedErrorText);
				using (writer.KeepIndent("  "))
				{
					writer.WriteLine(this.helpTextProvider.VerbNotSpecifiedMessageText);
				}
				writer.WriteLine();

				if (verbSet.GetNonHiddenVerbs().Any())
				{
					writer.WriteLine(this.helpTextProvider.VerbSetAvailableVerbsText);

					var namePrefix = GetNamePrefix(callChain, this.verbNameMatchingMode);
					var maxVerbNameLength = verbSet.GetNonHiddenVerbs().Max(verb => verb.Name.Length);

					using (writer.KeepIndent("  "))
					{
						foreach (var verb in verbSet.GetNonHiddenVerbs())
						{
							var verbName = GetPaddedString(GetVerbName(verb.Name, this.verbNameMatchingMode), maxVerbNameLength);
							writer.Write($"{namePrefix}{verbName}");
							using (writer.KeepIndent("    "))
							{
								writer.WriteLine(verb.Description);
							}
						}
					}
					writer.WriteLine();
				}
			}
			if (!string.IsNullOrEmpty(this.helpTextProvider.HelpFooterText))
			{
				writer.WriteLine(this.helpTextProvider.HelpFooterText);
			}
			this.console.WriteLine(writer);
		}

		private string GetParameterUsage(VerbParameter parameter)
		{
			if (parameter.IsValueCollector)
			{
				return $"<{parameter.Name.ToUpperInvariant()}>";
			}

			var parameterUsage = new StringBuilder();
			if (parameter.IsOptional)
			{
				parameterUsage.Append('[');
			}
			parameterUsage.Append(this.GetPrefixedOptionName(parameter.Name));
			switch (parameter.ValueArity)
			{
				case ValueArity.Zero:
					break;
				case ValueArity.ZeroOrOne:
				case ValueArity.ZeroOrMany:
				case ValueArity.One:
				case ValueArity.OneOrMany:
					parameterUsage.Append(this.optionArgumentSplitter);
					if (parameter.ValueArity == ValueArity.ZeroOrMany || parameter.ValueArity == ValueArity.ZeroOrOne)
					{
						parameterUsage.Append('[');
					}
					parameterUsage.Append("<");
					if (!this.helpTextProvider.TryGetParameterTypeFriendlyName(parameter, out var parameterTypeFriendlyName))
						parameterTypeFriendlyName = GetParameterTypeFriendlyName(parameter);
					parameterUsage.Append((parameterTypeFriendlyName ?? parameter.Name).ToUpperInvariant());
					parameterUsage.Append(">");
					if (parameter.ValueArity == ValueArity.ZeroOrMany || parameter.ValueArity == ValueArity.ZeroOrOne)
					{
						parameterUsage.Append(']');
					}
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			if (parameter.IsOptional)
			{
				parameterUsage.Append(']');
			}
			return parameterUsage.ToString();
		}
		private string GetParameterNames(VerbParameter parameter)
		{
			if (parameter == null) throw new ArgumentNullException(nameof(parameter));

			if (parameter.IsValueCollector)
			{
				return $"<{parameter.Name.ToUpperInvariant()}>";
			}

			if (string.IsNullOrEmpty(parameter.Alias) ||
				string.Equals(parameter.Alias, parameter.Name, StringComparison.Ordinal))
			{
				return this.GetPrefixedOptionName(parameter.Name);
			}
			else
			{
				return $"{this.GetPrefixedOptionName(parameter.Alias!)}, {this.GetPrefixedOptionName(parameter.Name)}";
			}
		}
		private string GetPrefixedOptionName(string optionName)
		{
			if (optionName == null) throw new ArgumentNullException(nameof(optionName));

			if (optionName.Length == 1)
			{
				return this.shortOptionNamePrefix + optionName;
			}
			else
			{
				return this.longOptionNamePrefix + optionName;
			}
		}

		private static string GetParameterTypeFriendlyName(VerbParameter parameter)
		{
			if (parameter == null) throw new ArgumentNullException(nameof(parameter));

			var parameterType = UnwrapType(parameter.ValueType.AsType());

			if (parameterType == typeof(int) || parameterType == typeof(sbyte) || parameterType == typeof(short) || parameterType == typeof(long) ||
				parameterType == typeof(uint) || parameterType == typeof(byte) || parameterType == typeof(ushort) || parameterType == typeof(ulong))
			{
				return "Integer";
			}
			else if (parameterType == typeof(double) || parameterType == typeof(float) || parameterType == typeof(decimal))
			{
				return "Number";
			}
			else if (parameterType == typeof(string))
			{
				return "Text";
			}
			else if (parameterType == typeof(char))
			{
				return "Symbol";
			}
			else if (parameterType == typeof(bool))
			{
				return "true/false";
			}
			else
			{
				return parameterType.Name;
			}
		}

		private static Type UnwrapType(Type type)
		{
			if (type.IsArray)
			{
				type = type.GetElementType() ?? type;
			}

			type = Nullable.GetUnderlyingType(type) ?? type;

			return type;
		}

		private static string GetNamePrefix(IEnumerable<Verb> enumerable, StringComparison verbNameMatchingMode)
		{
			var namePrefix = string.Join(" ", enumerable.Select(command => GetVerbName(command.Name, verbNameMatchingMode)));
			if (namePrefix != string.Empty)
			{
				namePrefix += " ";
			}

			return namePrefix;
		}

		private static string GetVerbName(string verbName, StringComparison verbNameMatchingMode)
		{
			switch (verbNameMatchingMode)
			{
				case StringComparison.OrdinalIgnoreCase:
				case StringComparison.CurrentCultureIgnoreCase:
#if !NETSTANDARD1_6
				case StringComparison.InvariantCultureIgnoreCase:
#endif
					return verbName.ToUpperInvariant();
#if !NETSTANDARD1_6
				case StringComparison.InvariantCulture:
#endif
				case StringComparison.CurrentCulture:
				case StringComparison.Ordinal:
					return verbName;
				default: throw new ArgumentOutOfRangeException(nameof(verbNameMatchingMode), verbNameMatchingMode, null);
			}
		}

		private static string GetPaddedString(string value, int length)
		{
			if (value.Length < length)
			{
				return value + new string(' ', length - value.Length);
			}
			return value;
		}
	}
}
