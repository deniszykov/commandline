/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;
using deniszykov.CommandLine.Binding;

namespace deniszykov.CommandLine.Parsing
{
	internal interface IArgumentsParser
	{
		public ParsedArguments Parse(string[] arguments, Func<string, ValueArity?> getOptionArity);
	}
}
