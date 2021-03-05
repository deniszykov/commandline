/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;
using System.Collections.Generic;
using System.Reflection;

namespace deniszykov.CommandLine.Binding
{
	internal static class ReflectionUtils
	{
#if NETSTANDARD
		public static IEnumerable<MethodInfo> GetAllMethods(this TypeInfo type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			var baseType = default(TypeInfo?);
			do
			{
				foreach (var method in type.DeclaredMethods)
				{
					yield return method;
				}
				baseType = baseType?.BaseType == null || baseType.BaseType == typeof(object) ? null : baseType.BaseType.GetTypeInfo();
			} while (baseType != null);
		}
#else
		public static IEnumerable<MethodInfo> GetAllMethods(this Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			return type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
		}
#endif

		public static bool IsInstantiationOf(this TypeInfo typeInfo, TypeInfo genericTypeDefinitionInfo)
		{
			if (typeInfo == null) throw new ArgumentNullException(nameof(typeInfo), "type != null");
			if (genericTypeDefinitionInfo == null) throw new ArgumentNullException(nameof(genericTypeDefinitionInfo), "genericTypeDefinition != null");

			if (genericTypeDefinitionInfo.IsGenericType && !genericTypeDefinitionInfo.IsGenericTypeDefinition) throw new ArgumentException($"Type '{genericTypeDefinitionInfo}' should be open generic type.", nameof(genericTypeDefinitionInfo));

			if (typeInfo.IsGenericType)
			{
				var typeDefinition = typeInfo.IsGenericTypeDefinition ? typeInfo : typeInfo.GetGenericTypeDefinition().GetTypeInfo();

				if (Equals(typeDefinition, genericTypeDefinitionInfo) || typeDefinition.IsSubclassOf(genericTypeDefinitionInfo.AsType()))
				{
					return true;
				}
			}

			if (genericTypeDefinitionInfo.IsInterface)
			{
				// check interfaces
				foreach (var interfaceType in typeInfo.GetInterfaces())
				{
					var interfaceTypeInfo = interfaceType.GetTypeInfo();

					if (interfaceTypeInfo.IsGenericType == false) continue;

					var interfaceTypeDefinition = interfaceTypeInfo.IsGenericTypeDefinition ? interfaceTypeInfo : interfaceType.GetGenericTypeDefinition().GetTypeInfo();

					if (Equals(interfaceTypeDefinition, genericTypeDefinitionInfo) || interfaceTypeDefinition.IsSubclassOf(genericTypeDefinitionInfo.AsType()))
					{
						return true;
					}
				}
			}

			if (typeInfo.BaseType != null && typeInfo.BaseType != typeof(object))
			{
				return typeInfo.BaseType.GetTypeInfo().IsInstantiationOf(genericTypeDefinitionInfo);
			}

			return false;
		}
	}
}
