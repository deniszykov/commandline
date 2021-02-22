using System;
using deniszykov.CommandLine.Binding;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Parsing
{
	internal interface IArgumentsParser
	{
		public ParsedArguments Parse([NotNull, ItemNotNull]string[] arguments, [NotNull]Func<string, ValueArity?> getOptionArity);
	}
}
