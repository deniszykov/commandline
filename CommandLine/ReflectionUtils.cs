
using System.Collections.Generic;
using System.Linq;
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
