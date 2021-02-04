using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using deniszykov.CommandLine.Binding;
using deniszykov.TypeConversion;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Renderers
{
	internal sealed class CommandRenderer
	{
		private readonly IConsole console;
		private readonly ITypeConversionProvider typeConversionProvider;

		public CommandRenderer(IConsole console, ITypeConversionProvider typeConversionProvider)
		{
			this.console = console;
			this.typeConversionProvider = typeConversionProvider;
		}

		public void Render(CommandSet commandSet, Command foundCommand, IEnumerable<Command> enumerable)
		{
			if (foundCommand == null) throw new ArgumentNullException(nameof(foundCommand));
			if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));

			var writer = new IndentedWriter(Environment.NewLine);
			writer.Write(" ");
			writer.KeepIndent();

			var namePrefix = GetNamePrefix(enumerable);
			foreach (var command in commandSet.Commands)
			{
				if (command.Hidden)
				{
					continue;
				}

				if (string.Equals(command.Name, foundCommand.Name, StringComparison.OrdinalIgnoreCase) == false)
					continue;

				writer.Write($"{namePrefix}{command.Name.ToUpperInvariant()} ");
				writer.KeepIndent();
				var commandParameters = string.Join(" ",
					command.GetNonHiddenBoundParameter().Select(parameter => string.Format(parameter.IsOptional ? "[--{0} <{1}>]" : "--{0} <{1}>", parameter.Name, GetParameterTypeFriendlyName(parameter))));
				writer.WriteLine(commandParameters);
				writer.RestoreIndent();
				writer.WriteLine();

				if (!string.IsNullOrEmpty(command.Description))
				{
					writer.WriteLine(command.Description);
					writer.WriteLine();
				}

				var maxNameLength = command.GetNonHiddenBoundParameter().Any() ?
					command.GetNonHiddenBoundParameter().Max(param => 2 + param.Name.Length + 1 + (string.IsNullOrEmpty(param.Alias) ? 0 : 4 + param.Alias.Length)) : 0;

				foreach (var parameter in command.BoundParameters)
				{
					if (parameter.IsHidden)
					{
						continue;
					}

					var parameterType = UnwrapType(parameter.ValueType.AsType());

					var options = "";
					var defaultValue = "";
					if (parameterType.GetTypeInfo().IsEnum) options = " Options are " + string.Join(", ", Enum.GetNames(parameterType)) + ".";
					if (parameter.IsOptional && parameter.DefaultValue != null) defaultValue = " Default value is '" + this.typeConversionProvider.ConvertToString(parameter.DefaultValue) + "'.";

					var paramAliasName = string.Empty;
					if (!string.IsNullOrEmpty(parameter.Alias))
					{
						paramAliasName = $"[-{parameter.Alias.ToUpperInvariant()}] ";
					}

					var paramNamePadding = new string(' ', maxNameLength - (paramAliasName.Length + 2 + parameter.Name.Length + 1));

					var paramDescriptionText = parameter.Description;
					if (paramDescriptionText.Length > 0 && paramDescriptionText.EndsWith(".", StringComparison.OrdinalIgnoreCase) == false)
						paramDescriptionText += ".";

					writer.Write($"--{parameter.Name.ToUpperInvariant()} {paramAliasName}{paramNamePadding}");
					writer.KeepIndent();
					writer.WriteLine(paramDescriptionText + defaultValue + options);
					writer.RestoreIndent();
				}
				writer.WriteLine();

			}
			writer.RestoreIndent();

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

			var namePrefix = GetNamePrefix(enumerable);

			writer.WriteLine("Available commands:");
			writer.Write(" ");
			writer.KeepIndent();
			foreach (var command in commandSet.Commands)
			{
				if (command.Hidden)
				{
					continue;
				}

				writer.WriteLine($"{namePrefix}{command.Name}");
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

			var namePrefix = GetNamePrefix(enumerable);
			writer.Write("  ");
			writer.KeepIndent();
			foreach (var command in commandSet.Commands)
			{
				if (command.Hidden)
				{
					continue;
				}

				writer.Write($"{namePrefix}{command.Name} ");
				writer.KeepIndent();
				writer.WriteLine(command.Description);
				writer.RestoreIndent();
			}
			writer.RestoreIndent();
			writer.RestoreIndent();

			this.console.WriteLine(writer);
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

		private static string GetNamePrefix(IEnumerable<Command> enumerable)
		{
			var namePrefix = string.Join(" ", enumerable.Select(command => command.ToString()));
			if (namePrefix != string.Empty)
			{
				namePrefix += " ";
			}

			return namePrefix;
		}
	}
}
