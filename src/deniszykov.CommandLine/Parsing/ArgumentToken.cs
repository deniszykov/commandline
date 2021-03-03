/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;

namespace deniszykov.CommandLine.Parsing
{
	internal readonly struct ArgumentToken
	{
		public readonly TokenType Type;
		public readonly string Value;

		public ArgumentToken(TokenType type, string value)
		{
			if (value == null) throw new ArgumentNullException(nameof(value));

			this.Type = type;
			this.Value = value;
		}
	}
}
