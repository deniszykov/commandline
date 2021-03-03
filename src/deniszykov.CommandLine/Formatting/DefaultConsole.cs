/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;
using System.Threading;

namespace deniszykov.CommandLine.Formatting
{
	internal class DefaultConsole : IConsole, IDisposable
	{
		private readonly CancellationTokenSource interruptTokenSource;
		private ConsoleCancelEventHandler? cancelKeyHandler;

		/// <inheritdoc />
		public CancellationToken InterruptToken => this.interruptTokenSource.Token;

		public DefaultConsole(bool hookCancelKey)
		{

			this.interruptTokenSource = new CancellationTokenSource();
			if (hookCancelKey)
			{
				Console.CancelKeyPress += this.cancelKeyHandler = this.Console_CancelKeyPress;
			}
		}

		private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs cancelEventArgs)
		{
			cancelEventArgs.Cancel = true;
			this.interruptTokenSource.Cancel();
		}

		/// <inheritdoc />
		public void WriteLine(object? text = null)
		{
			Console.Out.WriteLine(text);
		}

		/// <inheritdoc />
		public void WriteErrorLine(object? text = null)
		{
			Console.Error.WriteLine(text);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			this.interruptTokenSource?.Dispose();
			if (this.cancelKeyHandler != null)
			{
				Console.CancelKeyPress -= this.cancelKeyHandler;
				this.cancelKeyHandler = null;
			}
		}
	}
}
