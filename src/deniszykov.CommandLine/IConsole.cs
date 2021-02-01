using System.Threading;

namespace deniszykov.CommandLine
{
	public interface IConsole
	{
		CancellationToken InterruptToken { get; }

		void Indent(int charCount);
		void UnIndent(int charCount);

		void WriteLine(object text = null);
		void WriteErrorLine(object text = null);
	}
}
