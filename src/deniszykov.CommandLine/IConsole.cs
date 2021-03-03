/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System.Threading;
using deniszykov.CommandLine.Binding;
using JetBrains.Annotations;

namespace deniszykov.CommandLine
{
	/// <summary>
	/// A console instance used to output help and errors messages.
	/// </summary>
	[PublicAPI]
	public interface IConsole
	{
		/// <summary>
		/// Interruption token used to provide cancellation during <see cref="Verb"/>'s execution.
		/// </summary>
		CancellationToken InterruptToken { get; }

		/// <summary>
		/// Write specified <paramref name="text"/> to standard output and start new line.
		/// </summary>
		void WriteLine(object? text = null);
		/// <summary>
		/// Write specified <paramref name="text"/> to standard error output and start new line.
		/// </summary>
		void WriteErrorLine(object? text = null);
	}
}
