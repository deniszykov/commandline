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
	internal sealed class CommandRenderer
	{
		[NotNull] private readonly IConsole console;
		[NotNull] private readonly ITypeConversionProvider typeConversionProvider;

		public StringComparison CommandNameMatchingMode { get; }
		public string ShortOptionNamePrefix { get; }
		public string LongOptionNamePrefix { get; }
		public char OptionArgumentSplitter { get; }

		public CommandRenderer(
			[NotNull] CommandLineConfiguration configuration,
			[NotNull] IConsole console,
			[NotNull] ITypeConversionProvider typeConversionProvider)
		{
			if (configuration == null) throw new ArgumentNullException(nameof(configuration));
			if (console == null) throw new ArgumentNullException(nameof(console));
			if (typeConversionProvider == null) throw new ArgumentNullException(nameof(typeConversionProvider));

			this.console = console;
			this.typeConversionProvider = typeConversionProvider;
			this.CommandNameMatchingMode = configuration.CommandNameMatchingMode;
			this.ShortOptionNamePrefix = configuration.ShortOptionNamePrefixes.First();
			this.LongOptionNamePrefix = configuration.LongOptionNamePrefixes.First();
			this.OptionArgumentSplitter = configuration.OptionArgumentSplitters.First();
		}

		public void Render(CommandSet commandSet, Command foundCommand, IEnumerable<Command> enumerable)
		{
			if (foundCommand == null) throw new ArgumentNullException(nameof(foundCommand));
			if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));

			var writer = new IndentedWriter(Environment.NewLine);


			var namePrefix = GetNamePrefix(enumerable, this.CommandNameMatchingMode);
			foreach (var command in commandSet.Commands)
			{
				if (command.Hidden)
				{
					continue;
				}

				if (string.Equals(command.Name, foundCommand.Name, this.CommandNameMatchingMode) == false)
					continue;

				writer.WriteLine("Usage:");
				writer.Write("  ");
				writer.KeepIndent();
				writer.Write($"{namePrefix}{GetCommandName(command.Name, this.CommandNameMatchingMode)} ");
				writer.KeepIndent();
				var parameterUsage = string.Join(" ", command.GetNonHiddenBoundParameter().Select(this.GetParameterUsage));
				writer.WriteLine(parameterUsage);
				writer.RestoreIndent();
				writer.WriteLine();
				writer.RestoreIndent();

				if (!string.IsNullOrEmpty(command.Description))
				{
					writer.Write("  ");
					writer.KeepIndent();

					writer.WriteLine(command.Description);
					writer.WriteLine();

					writer.RestoreIndent();
				}

				var maxNameLength = command.GetNonHiddenBoundParameter().Any() ?
					command.GetNonHiddenBoundParameter().Max(param => this.GetParameterNames(param).Length) : 0;

				writer.WriteLine("Options:");
				writer.Write("  ");
				writer.KeepIndent();
				foreach (var parameter in command.BoundParameters)
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
		public void RenderNotFound(CommandSet commandSet, string commandToDescribe, IEnumerable<Command> enumerable)
		{
			var writer = new IndentedWriter(Environment.NewLine);
			writer.Write(" ");
			writer.KeepIndent();

			writer.WriteLine("Error:");
			writer.Write(" ");
			writer.WriteLine($"Command '{commandToDescribe}' is not found.");
			writer.RestoreIndent();

			var namePrefix = GetNamePrefix(enumerable, this.CommandNameMatchingMode);

			writer.WriteLine("Available commands:");
			writer.Write(" ");
			writer.KeepIndent();
			foreach (var command in commandSet.Commands)
			{
				if (command.Hidden)
				{
					continue;
				}

				writer.WriteLine($"{namePrefix}{GetCommandName(command.Name, this.CommandNameMatchingMode)}");
			}
			writer.RestoreIndent();
			writer.RestoreIndent();

			this.console.WriteLine(writer);
		}
		public void RenderList(CommandSet commandSet, IEnumerable<Command> enumerable, bool includeTypeHelpText)
		{
			var writer = new IndentedWriter(Environment.NewLine);

			writer.Write(" ");
			writer.KeepIndent();

			if (includeTypeHelpText)
			{
				writer.WriteLine(commandSet.Description);
				writer.WriteLine();
			}
			writer.WriteLine("Commands:");

			var namePrefix = GetNamePrefix(enumerable, this.CommandNameMatchingMode);
			writer.Write("  ");
			writer.KeepIndent();
			foreach (var command in commandSet.Commands)
			{
				if (command.Hidden)
				{
					continue;
				}

				writer.Write($"{namePrefix}{GetCommandName(command.Name, this.CommandNameMatchingMode)} ");
				writer.KeepIndent();
				writer.WriteLine(command.Description);
				writer.RestoreIndent();
			}
			writer.RestoreIndent();
			writer.RestoreIndent();

			this.console.WriteLine(writer);
		}

		[NotNull]
		private string GetParameterUsage([NotNull] CommandParameter parameter)
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
		private string GetParameterNames([NotNull] CommandParameter parameter)
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

		private static string GetParameterTypeFriendlyName(CommandParameter parameter)
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

		private static string GetNamePrefix(IEnumerable<Command> enumerable, StringComparison commandNameMatchingMode)
		{
			var namePrefix = string.Join(" ", enumerable.Select(command => GetCommandName(command.Name, commandNameMatchingMode)));
			if (namePrefix != string.Empty)
			{
				namePrefix += " ";
			}

			return namePrefix;
		}

		private static string GetCommandName(string commandName, StringComparison commandNameMatchingMode)
		{
			switch (commandNameMatchingMode)
			{
				case StringComparison.OrdinalIgnoreCase:
				case StringComparison.CurrentCultureIgnoreCase:
#if !NETSTANDARD1_6
				case StringComparison.InvariantCultureIgnoreCase:
#endif
					return commandName.ToUpperInvariant();
#if !NETSTANDARD1_6
				case StringComparison.InvariantCulture:
#endif
				case StringComparison.CurrentCulture:
				case StringComparison.Ordinal:
					return commandName;
				default: throw new ArgumentOutOfRangeException(nameof(commandNameMatchingMode), commandNameMatchingMode, null);
			}
		}
	}
}
