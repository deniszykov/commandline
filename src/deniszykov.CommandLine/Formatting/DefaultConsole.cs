using System;
using System.Threading;

namespace deniszykov.CommandLine.Formatting
{
	internal class DefaultConsole : IConsole, IDisposable
	{
		private readonly CancellationTokenSource interruptTokenSource;
		private ConsoleCancelEventHandler? cancelKeyHandler;

		public int Width { get; }
		public CancellationToken InterruptToken => this.interruptTokenSource.Token;

		public DefaultConsole(bool hookCancelKey)
		{

			// Author: @MarcStan
			// fix when output is redirected, assume we can print any length and redirected
			// output takes care of formatting
			try
			{
				this.Width = Console.WindowWidth;
			}
			catch { /*ignore error*/ }

			if (this.Width <= 0) this.Width = int.MaxValue;

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
