using System;
using deniszykov.CommandLine.Binding;

namespace deniszykov.CommandLine.Parsing
{
	internal interface IArgumentsParser
	{
		public ParsedArguments Parse(string[] arguments, Func<string, ValueArity?> getOptionArity);
	}
}
