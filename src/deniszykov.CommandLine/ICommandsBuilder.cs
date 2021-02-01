using deniszykov.CommandLine.Binding;

namespace deniszykov.CommandLine
{
	public interface ICommandsBuilder
	{
		CommandSet Build();
	}
}