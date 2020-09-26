/*
	Copyright (c) 2020 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System.Collections.Generic;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace System
{
	internal static class ReflectionUtils
	{
#if NETSTANDARD
		public static IEnumerable<MethodInfo> GetAllMethods(this TypeInfo type)
		{
			if (type == null) throw new ArgumentNullException("type");

			do
			{
				foreach (var method in type.DeclaredMethods)
				{
					yield return method;
				}
				type = type.BaseType == null || type.BaseType == typeof(object) ? null : type.BaseType.GetTypeInfo();
			} while (type != null);
		}
#else
		public static Type GetTypeInfo(this Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return type;
		}

		public static IEnumerable<MethodInfo> GetAllMethods(this Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
		}
#endif
	}
}
