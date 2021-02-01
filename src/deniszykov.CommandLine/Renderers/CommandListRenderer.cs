using System;
using System.Linq;
using System.Text;
using deniszykov.CommandLine.Binding;
using deniszykov.TypeConversion;

namespace deniszykov.CommandLine.Renderers
{
	class CommandListRenderer
	{
		private IConsole console;
		private ITypeConversionProvider typeConversionProvider;

		public CommandListRenderer(IConsole console, ITypeConversionProvider typeConversionProvider)
		{
			this.console = console;
			this.typeConversionProvider = typeConversionProvider;
		}
		/*
		private static string GetTypeDescription(Type type, bool includeTypeHelpText)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			var result = new StringBuilder();
			var basePadding = 1;

			if (includeTypeHelpText)
			{
				var descriptionText = default(string);
				var helpText = ReflectionUtils.GetTypeInfo(type).GetCustomAttributes(typeof(HelpTextAttribute), true).Cast<HelpTextAttribute>().FirstOrDefault();
				if (helpText != null)
				{
					descriptionText = helpText.Description;
				}
#if !NETSTANDARD1_3
				var description = type.GetCustomAttributes(typeof(DescriptionAttribute), true).Cast<DescriptionAttribute>().FirstOrDefault();
				if (description != null)
				{
					descriptionText = descriptionText ?? description.Description;
				}
#endif
				result.Append(' ', basePadding).AppendLine(descriptionText).AppendLine();
			}
			result.Append(' ', basePadding).AppendLine("Commands:");

			foreach (var method in ReflectionUtils.GetTypeInfo(type).GetAllMethods().Where(IsValidCommandSignature))
			{
				if (IsHidden(method))
					continue;

				result.AppendFormat("  {0} ", method.Name.ToUpper());

				var methodDescriptionText = default(string);
				var methodHelpText = method.GetCustomAttributes(typeof(HelpTextAttribute), true).Cast<HelpTextAttribute>().FirstOrDefault();
				if (methodHelpText != null)
					methodDescriptionText = methodHelpText.Description;
#if !NETSTANDARD1_3
				var methodDescription = method.GetCustomAttributes(typeof(DescriptionAttribute), true).Cast<DescriptionAttribute>().FirstOrDefault();
				if (methodDescription != null)
					methodDescriptionText = methodDescriptionText ?? methodDescription.Description;
#endif
				if (string.IsNullOrEmpty(methodDescriptionText) == false)
					AppendLinePadded(result, methodDescriptionText);
				else
					result.AppendLine();
			}

			return result.ToString();
		}
		*/
		public void RenderNotFound(CommandSet commandSet, string commandToDescribe)
		{
			throw new NotImplementedException();
		}
		public void Render(CommandSet commandSet)
		{
			throw new NotImplementedException();
		}
	}
}
