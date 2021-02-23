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
