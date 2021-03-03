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
	/// Attribute used to hide member from help option (/?, --help, -h, etc...).
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method)]
	[PublicAPI]
	public sealed class HiddenAttribute : Attribute
	{
	}
}
