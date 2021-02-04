using System;
using deniszykov.CommandLine.Binding;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Parsing
{
	public interface IArgumentsParser
	{
		public ParsedArguments Parse([NotNull, ItemNotNull]string[] arguments, [NotNull]Func<string, ParameterValueArity?> getOptionArity);
	}
}
