/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;
#if !NETSTANDARD1_6
using System.ComponentModel;
#endif
using System.Linq;
using System.Reflection;
using System.Threading;

namespace deniszykov.CommandLine.Annotations
{
	internal static class AnnotationUtils
	{
		public static bool IsHidden(this ICustomAttributeProvider customAttributeProvider)
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
		public static bool IsFlags(this ICustomAttributeProvider customAttributeProvider)
		{
			if (customAttributeProvider == null) throw new ArgumentNullException(nameof(customAttributeProvider));

			return customAttributeProvider.GetCustomAttributes(typeof(FlagsAttribute), true).Any();
		}
		public static bool IsServiceParameter(this ParameterInfo parameterInfo)
		{
			if (parameterInfo == null) throw new ArgumentNullException(nameof(parameterInfo));

			var isServiceDependency = parameterInfo.GetCustomAttributes(typeof(FromServiceAttribute), true).FirstOrDefault() is FromServiceAttribute;
			return isServiceDependency ||
				parameterInfo.ParameterType == typeof(VerbExecutionContext) ||
				parameterInfo.ParameterType == typeof(ICommandLineBuilder) ||
				parameterInfo.ParameterType == typeof(CancellationToken);
		}
		public static string? GetName(this ICustomAttributeProvider customAttributeProvider)
		{
			if (customAttributeProvider == null) throw new ArgumentNullException(nameof(customAttributeProvider));

			if (customAttributeProvider.GetCustomAttributes(typeof(NameAttribute), true).FirstOrDefault() is NameAttribute nameAttribute &&
				!string.IsNullOrEmpty(nameAttribute.Name))
			{
				return nameAttribute.Name;
			}
			return null;
		}
		public static string? GetAlias(this ICustomAttributeProvider customAttributeProvider)
		{
			if (customAttributeProvider == null) throw new ArgumentNullException(nameof(customAttributeProvider));

			if (customAttributeProvider.GetCustomAttributes(typeof(AliasAttribute), true).FirstOrDefault() is AliasAttribute aliasAttribute &&
				!string.IsNullOrEmpty(aliasAttribute.Alias))
			{
				return aliasAttribute.Alias;
			}
			return null;
		}
		public static string? GetDescription(this ICustomAttributeProvider customAttributeProvider)
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
