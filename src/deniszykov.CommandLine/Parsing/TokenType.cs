/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

namespace deniszykov.CommandLine.Parsing
{
	internal enum TokenType
	{
		Value,
		ShortOption,
		LongOption,
		UnknownOption,
		OptionArgument,
		OptionBreak,
		HelpOption
	}
}