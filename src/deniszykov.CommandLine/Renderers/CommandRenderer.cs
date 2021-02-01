using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using deniszykov.CommandLine.Binding;
using deniszykov.TypeConversion;

namespace deniszykov.CommandLine.Renderers
{
	class CommandRenderer
	{
		private IConsole console;
		private ITypeConversionProvider typeConversionProvider;

		public CommandRenderer(IConsole console, ITypeConversionProvider typeConversionProvider)
		{
			this.console = console;
			this.typeConversionProvider = typeConversionProvider;
		}

		/*

		private static string GetCommandDescription(Type type, string commandName)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));
			if (commandName == null) throw new ArgumentNullException(nameof(commandName));

			var result = new StringBuilder();
			var basePadding = 1;

			foreach (var method in ReflectionUtils.GetTypeInfo(type).GetAllMethods().Where(IsValidCommandSignature))
			{
				if (string.Equals(method.Name, commandName, StringComparison.OrdinalIgnoreCase) == false)
					continue;

				if (IsHidden(method))
					continue;

				result.Append(' ', basePadding).AppendLine("Command:");
				result.Append(' ', basePadding + 1).Append(method.Name.ToUpper()).Append(" ");

				var paramsArr = method.GetParameters().Where(p => !IsHidden(p)).Select(parameter => string.Format(parameter.IsOptional ? "[--{0} <{1}>] " : "--{0} <{1}> ", parameter.Name, GetParameterTypeFriendlyName(parameter))).ToArray();
				var paramsDesc = string.Concat(paramsArr);
				AppendLinePadded(result, paramsDesc);
				result.AppendLine();

				var methodDescriptionText = default(string);
				var methodHelpText = method.GetCustomAttributes(typeof(HelpTextAttribute), true).Cast<HelpTextAttribute>().FirstOrDefault();
				if (methodHelpText != null)
					methodDescriptionText = methodHelpText.Description;
#if !NETSTANDARD1_3
				var methodDescription = method.GetCustomAttributes(typeof(DescriptionAttribute), true).Cast<DescriptionAttribute>().FirstOrDefault();
				if (methodDescription != null)
					methodDescriptionText = methodDescriptionText ?? methodDescription.Description;
#endif
				methodDescriptionText = (methodDescriptionText ?? string.Empty).Trim();

				if (string.IsNullOrEmpty(methodDescriptionText) == false)
				{
					result.Append(' ', basePadding).AppendLine("Description:");
					result.Append(' ', basePadding + 1);

					if (methodDescriptionText.EndsWith(".", StringComparison.OrdinalIgnoreCase) == false)
						methodDescriptionText += ".";

					AppendLinePadded(result, methodDescriptionText);
				}
				else
				{
					result.AppendLine();
				}

				result.AppendLine();
				result.Append(' ', basePadding).AppendLine("Parameters:");
				foreach (var parameter in method.GetParameters())
				{
					if (IsHidden(parameter))
						continue;

					if (parameter.ParameterType == typeof(CommandLineArguments))
						continue;

					var parameterType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
					var parameterTypeName = parameterType.Name;
					var options = "";
					var defaultValue = "";
					if (ReflectionUtils.GetTypeInfo(parameterType).IsEnum) options = " Options are " + string.Join(", ", Enum.GetNames(parameterType)) + ".";
					if (parameter.IsOptional && parameter.DefaultValue != null) defaultValue = " Default value is '" + TypeConvert.Convert(parameter.DefaultValue, parameterType) + "'.";

					var paramDescriptionText = default(string);
					var paramHelpText = parameter.GetCustomAttributes(typeof(HelpTextAttribute), true).Cast<HelpTextAttribute>().FirstOrDefault();
					if (paramHelpText != null)
						paramDescriptionText = paramHelpText.Description;
#if !NETSTANDARD1_3
					var paramDescription = parameter.GetCustomAttributes(typeof(DescriptionAttribute), true).Cast<DescriptionAttribute>().FirstOrDefault();
					if (paramDescription != null)
						paramDescriptionText = paramDescriptionText ?? paramDescription.Description;
#endif

					paramDescriptionText = (paramDescriptionText ?? string.Empty).Trim();

					if (paramDescriptionText.Length > 0 && paramDescriptionText.EndsWith(".", StringComparison.OrdinalIgnoreCase) == false)
						paramDescriptionText += ".";

					result.Append(' ', basePadding + 2);
					result.Append(parameter.Name.ToUpperInvariant()).Append(" ");
					AppendLinePadded(result, string.Format("({0}) {1}{2}{3}", parameterTypeName, paramDescriptionText ?? string.Empty, defaultValue, options));
				}
			}

			return result.ToString();
		}

		*/

		public void Render(Command command)
		{
			throw new NotImplementedException();
		}
	}
}
