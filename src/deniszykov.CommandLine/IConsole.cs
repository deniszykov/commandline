using System.Threading;

namespace deniszykov.CommandLine
{
	public interface IConsole
	{
		CancellationToken InterruptToken { get; }

		void WriteLine(object text = null);
		void WriteErrorLine(object text = null);
	}
}
