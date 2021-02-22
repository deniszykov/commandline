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