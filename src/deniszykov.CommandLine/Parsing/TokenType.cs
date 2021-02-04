namespace deniszykov.CommandLine.Parsing
{
	public enum TokenType
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