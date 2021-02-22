using System;

namespace deniszykov.CommandLine.Annotations
{
	/// <summary>
	/// Resolved service marker. Apply this attribute to method's parameter to mark it as resolved service and remove it from 'option' binding.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter)]
	public class FromServiceAttribute : Attribute
	{

	}
}
