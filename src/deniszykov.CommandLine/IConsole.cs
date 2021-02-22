using System.Threading;
using deniszykov.CommandLine.Binding;

namespace deniszykov.CommandLine
{
	/// <summary>
	/// A console instance used to output help and errors messages.
	/// </summary>
	public interface IConsole
	{
		/// <summary>
		/// Interruption token used to provide cancellation during <see cref="Verb"/>'s execution.
		/// </summary>
		CancellationToken InterruptToken { get; }

		/// <summary>
		/// Write specified <paramref name="text"/> to standard output and start new line.
		/// </summary>
		void WriteLine(object text = null);
		/// <summary>
		/// Write specified <paramref name="text"/> to standard error output and start new line.
		/// </summary>
		void WriteErrorLine(object text = null);
	}
}
