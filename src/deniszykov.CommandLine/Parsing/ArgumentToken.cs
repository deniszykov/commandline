namespace deniszykov.CommandLine.Parsing
{
	internal struct ArgumentToken
	{
		public readonly TokenType Type;
		public readonly string Value;

		public ArgumentToken(TokenType type, string value)
		{
			this.Type = type;
			this.Value = value;
		}
	}
}
