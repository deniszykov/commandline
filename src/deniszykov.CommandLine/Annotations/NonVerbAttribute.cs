using System;
using System.Reflection;
using deniszykov.CommandLine.Binding;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Annotations
{

	/// <summary>
	/// Excludes <see cref="MethodInfo"/> from discovering by <see cref="VerbSet"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	[PublicAPI]
	public class NonVerbAttribute : Attribute
	{
	}
}
