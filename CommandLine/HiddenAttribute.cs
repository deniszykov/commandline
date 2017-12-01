/*
	Copyright (c) 2017 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

// ReSharper disable once CheckNamespace
namespace System
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method)]
    public sealed class HiddenAttribute : Attribute
    {

    }
}
