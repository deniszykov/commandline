using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using deniszykov.CommandLine.Annotations;
using deniszykov.CommandLine.Binding;
using deniszykov.TypeConversion;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Renderers
{
	internal sealed class VerbRenderer
	{
		[NotNull] private readonly IConsole console;
		[NotNull] private readonly ITypeConversionProvider typeConversionProvider;

		public StringComparison VerbNameMatchingMode { get; }
		public string ShortOptionNamePrefix { get; }
		public string LongOptionNamePrefix { get; }
		public char OptionArgumentSplitter { get; }

		public VerbRenderer(
			[NotNull] CommandLineConfiguration configuration,
			[NotNull] IConsole console,
			[NotNull] ITypeConversionProvider typeConversionProvider)
		{
			if (configuration == null) throw new ArgumentNullException(nameof(configuration));
			if (console == null) throw new ArgumentNullException(nameof(console));
			if (typeConversionProvider == null) throw new ArgumentNullException(nameof(typeConversionProvider));

			this.console = console;
			this.typeConversionProvider = typeConversionProvider;
			this.VerbNameMatchingMode = configuration.VerbNameMatchingMode;
			this.ShortOptionNamePrefix = configuration.ShortOptionNamePrefixes.First();
			this.LongOptionNamePrefix = configuration.LongOptionNamePrefixes.First();
			this.OptionArgumentSplitter = configuration.OptionArgumentSplitters.First();
		}

		public void Render(VerbSet verbSet, Verb foundVerb, IEnumerable<Verb> enumerable)
		{
			if (foundVerb == null) throw new ArgumentNullException(nameof(foundVerb));
			if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));

			var writer = new IndentedWriter(Environment.NewLine);


			var namePrefix = GetNamePrefix(enumerable, this.VerbNameMatchingMode);
			foreach (var verb in verbSet.Verbs)
			{
				if (verb.Hidden)
				{
					continue;
				}

				if (string.Equals(verb.Name, foundVerb.Name, this.VerbNameMatchingMode) == false)
					continue;

				writer.WriteLine("Usage:");
				writer.Write("  ");
				writer.KeepIndent();
				writer.Write($"{namePrefix}{GetCommandName(verb.Name, this.VerbNameMatchingMode)} ");
				writer.KeepIndent();
				var parameterUsage = string.Join(" ", verb.GetNonHiddenBoundParameter().Select(this.GetParameterUsage));
				writer.WriteLine(parameterUsage);
				writer.RestoreIndent();
				writer.WriteLine();
				writer.RestoreIndent();

				if (!string.IsNullOrEmpty(verb.Description))
				{
					writer.Write("  ");
					writer.KeepIndent();

					writer.WriteLine(verb.Description);
					writer.WriteLine();

					writer.RestoreIndent();
				}

				var maxNameLength = verb.GetNonHiddenBoundParameter().Any() ?
					verb.GetNonHiddenBoundParameter().Max(param => this.GetParameterNames(param).Length) : 0;

				writer.WriteLine("Options:");
				writer.Write("  ");
				writer.KeepIndent();
				foreach (var parameter in verb.BoundParameters)
				{
					if (parameter.IsHidden)
					{
						continue;
					}

					var parameterType = UnwrapType(parameter.ValueType.AsType());

					var variantsText = "";
					var defaultValueText = "";
					if (parameterType.GetTypeInfo().IsEnum && parameter.ValueType.IsFlags())
					{
						variantsText = " Combination of '" + string.Join("', '", Enum.GetNames(parameterType)) + "'.";
					}
					else if (parameterType.GetTypeInfo().IsEnum)
					{
						variantsText = " One of '" + string.Join("', '", Enum.GetNames(parameterType)) + "'.";
					}

					if (parameter.IsOptional && parameter.DefaultValue != null)
					{
						defaultValueText = " Default value is '" + this.typeConversionProvider.ConvertToString(parameter.DefaultValue) + "'.";
					}

					var paramName = this.GetParameterNames(parameter);
					var paramNamePadding = new string(' ', maxNameLength - paramName.Length);

					var paramDescriptionText = parameter.Description;
					if (paramDescriptionText.Length > 0 && paramDescriptionText.EndsWith(".", StringComparison.OrdinalIgnoreCase) == false)
						paramDescriptionText += ".";

					writer.Write($"{paramName}{paramNamePadding}    ");
					writer.KeepIndent();
					writer.WriteLine(paramDescriptionText + defaultValueText + variantsText);
					writer.RestoreIndent();
				}
				writer.RestoreIndent();
				writer.WriteLine();

			}

			this.console.WriteLine(writer);
		}
		public void RenderNotFound(VerbSet verbSet, string verbToDescribe, IEnumerable<Verb> enumerable)
		{
			var writer = new IndentedWriter(Environment.NewLine);
			writer.Write(" ");
			writer.KeepIndent();

			writer.WriteLine("Error:");
			writer.Write(" ");
			writer.WriteLine($"Command '{verbToDescribe}' is not found.");
			writer.RestoreIndent();

			var namePrefix = GetNamePrefix(enumerable, this.VerbNameMatchingMode);

			writer.WriteLine("Available verbs:");
			writer.Write(" ");
			writer.KeepIndent();
			foreach (var verb in verbSet.Verbs)
			{
				if (verb.Hidden)
				{
					continue;
				}

				writer.WriteLine($"{namePrefix}{GetCommandName(verb.Name, this.VerbNameMatchingMode)}");
			}
			writer.RestoreIndent();
			writer.RestoreIndent();

			this.console.WriteLine(writer);
		}
		public void RenderList(VerbSet verbSet, IEnumerable<Verb> enumerable, bool includeTypeHelpText)
		{
			var writer = new IndentedWriter(Environment.NewLine);

			writer.Write(" ");
			writer.KeepIndent();

			if (includeTypeHelpText)
			{
				writer.WriteLine(verbSet.Description);
				writer.WriteLine();
			}
			writer.WriteLine("Verbs:");

			var namePrefix = GetNamePrefix(enumerable, this.VerbNameMatchingMode);
			writer.Write("  ");
			writer.KeepIndent();
			foreach (var verb in verbSet.Verbs)
			{
				if (verb.Hidden)
				{
					continue;
				}

				writer.Write($"{namePrefix}{GetCommandName(verb.Name, this.VerbNameMatchingMode)} ");
				writer.KeepIndent();
				writer.WriteLine(verb.Description);
				writer.RestoreIndent();
			}
			writer.RestoreIndent();
			writer.RestoreIndent();

			this.console.WriteLine(writer);
		}

		[NotNull]
		private string GetParameterUsage([NotNull] VerbParameter parameter)
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
				case ParameterValueArity.Zero:
					break;
				case ParameterValueArity.ZeroOrOne:
				case ParameterValueArity.ZeroOrMany:
				case ParameterValueArity.One:
				case ParameterValueArity.OneOrMany:
					parameterUsage.Append(this.OptionArgumentSplitter);
					if (parameter.ValueArity == ParameterValueArity.ZeroOrMany || parameter.ValueArity == ParameterValueArity.ZeroOrOne)
					{
						parameterUsage.Append('[');
					}
					parameterUsage.Append("<");
					parameterUsage.Append(GetParameterTypeFriendlyName(parameter).ToUpperInvariant());
					parameterUsage.Append(">");
					if (parameter.ValueArity == ParameterValueArity.ZeroOrMany || parameter.ValueArity == ParameterValueArity.ZeroOrOne)
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
		[NotNull]
		private string GetParameterNames([NotNull] VerbParameter parameter)
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
				return $"{this.GetPrefixedOptionName(parameter.Alias)}, {this.GetPrefixedOptionName(parameter.Name)}";
			}
		}
		[NotNull]
		private string GetPrefixedOptionName([NotNull] string optionName)
		{
			if (optionName == null) throw new ArgumentNullException(nameof(optionName));

			if (optionName.Length == 1)
			{
				return this.ShortOptionNamePrefix + optionName;
			}
			else
			{
				return this.LongOptionNamePrefix + optionName;
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

		[NotNull]
		private static Type UnwrapType([NotNull] Type type)
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
			var namePrefix = string.Join(" ", enumerable.Select(command => GetCommandName(command.Name, verbNameMatchingMode)));
			if (namePrefix != string.Empty)
			{
				namePrefix += " ";
			}

			return namePrefix;
		}

		private static string GetCommandName(string verbName, StringComparison verbNameMatchingMode)
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
	}
}
