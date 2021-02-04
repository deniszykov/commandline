using System;
#if !NETSTANDARD1_6
using System.ComponentModel;
#endif
using System.Linq;
using System.Reflection;
using System.Threading;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Annotations
{
	public static class AnnotationUtils
	{
		public static bool IsHidden([NotNull] this ICustomAttributeProvider customAttributeProvider)
		{
			if (customAttributeProvider == null) throw new ArgumentNullException(nameof(customAttributeProvider));

			if (customAttributeProvider.GetCustomAttributes(typeof(HiddenAttribute), true).Any())
				return true;

#if NETSTANDARD1_6
			return false;
#else
			// ReSharper disable once IdentifierTypo
			var browsableAttributes = customAttributeProvider.GetCustomAttributes(typeof(BrowsableAttribute), true).Cast<BrowsableAttribute>();
			return browsableAttributes.Any(a => a.Browsable == false);
#endif
		}
		public static bool IsFlags([NotNull] this ICustomAttributeProvider customAttributeProvider)
		{
			if (customAttributeProvider == null) throw new ArgumentNullException(nameof(customAttributeProvider));

			return customAttributeProvider.GetCustomAttributes(typeof(FlagsAttribute), true).Any();
		}
		public static bool IsServiceParameter([NotNull] this ParameterInfo parameterInfo)
		{
			if (parameterInfo == null) throw new ArgumentNullException(nameof(parameterInfo));

			var isServiceDependency = parameterInfo.GetCustomAttributes(typeof(FromServiceAttribute), true).FirstOrDefault() is FromServiceAttribute;
			return isServiceDependency ||
				parameterInfo.ParameterType == typeof(CommandExecutionContext) ||
				parameterInfo.ParameterType == typeof(CancellationToken);
		}
		[CanBeNull]
		public static string GetName([NotNull] this ICustomAttributeProvider customAttributeProvider)
		{
			if (customAttributeProvider == null) throw new ArgumentNullException(nameof(customAttributeProvider));

			if (customAttributeProvider.GetCustomAttributes(typeof(NameAttribute), true).FirstOrDefault() is NameAttribute commandAttribute &&
				!string.IsNullOrEmpty(commandAttribute.Name))
			{
				return commandAttribute.Name;
			}
			return null;
		}
		[CanBeNull]
		public static string GetAlias([NotNull] this ICustomAttributeProvider customAttributeProvider)
		{
			if (customAttributeProvider == null) throw new ArgumentNullException(nameof(customAttributeProvider));

			if (customAttributeProvider.GetCustomAttributes(typeof(AliasAttribute), true).FirstOrDefault() is AliasAttribute commandAttribute &&
				!string.IsNullOrEmpty(commandAttribute.Alias))
			{
				return commandAttribute.Alias;
			}
			return null;
		}
		[CanBeNull]
		public static string GetDescription([NotNull] this ICustomAttributeProvider customAttributeProvider)
		{
			if (customAttributeProvider == null) throw new ArgumentNullException(nameof(customAttributeProvider));

#if !NETSTANDARD1_6
			if (customAttributeProvider.GetCustomAttributes(typeof(DescriptionAttribute), true).FirstOrDefault() is DescriptionAttribute descriptionAttribute &&
				!string.IsNullOrEmpty(descriptionAttribute.Description))
			{
				return descriptionAttribute.Description;
			}
#endif
			if (customAttributeProvider.GetCustomAttributes(typeof(HelpTextAttribute), true).FirstOrDefault() is HelpTextAttribute helpTextAttribute &&
			 !string.IsNullOrEmpty(helpTextAttribute.Text))
			{
				return helpTextAttribute.Text;
			}
			return null;
		}
	}
}
