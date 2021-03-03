/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Annotations
{
	/// <summary>
	/// Resolved service marker. Apply this attribute to method's parameter to mark it as resolved service and remove it from 'option' binding.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter)]
	[PublicAPI]
	public class FromServiceAttribute : Attribute
	{

	}
}
