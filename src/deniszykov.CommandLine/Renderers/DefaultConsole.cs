using System;
using System.Text;
using System.Threading;

namespace deniszykov.CommandLine.Renderers
{
	internal class DefaultConsole : IConsole, IDisposable
	{
		private readonly string newLine;
		private readonly CancellationTokenSource interruptTokenSource;
		private ConsoleCancelEventHandler cancelKeyHandler;

		public int Width { get; }
		public CancellationToken InterruptToken => this.interruptTokenSource.Token;

		public DefaultConsole(bool hookCancelKey, string newLine)
		{
			if (newLine == null) throw new ArgumentNullException(nameof(newLine));

			// Author: @MarcStan
			// fix when output is redirected, assume we can print any length and redirected
			// output takes care of formatting
			try { Width = Console.WindowWidth; }
			catch { /*ignore error*/ }

			if (Width <= 0)
				Width = int.MaxValue;

			this.newLine = newLine;
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
		public void Indent(int charCount)
		{
			throw new NotImplementedException();
		}
		/// <inheritdoc />
		public void UnIndent(int charCount)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public void WriteLine(object text = null)
		{
			Console.WriteLine(text);
		}
		/// <inheritdoc />
		public void WriteErrorLine(object text = null)
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

		private void AppendLinePadded(StringBuilder builder, string text)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));
			if (text == null) throw new ArgumentNullException(nameof(text));

			var padding = GetPadding(builder);
			var windowWidth = this.Width;
			var chunkSize = windowWidth - padding - Environment.NewLine.Length;

			if (chunkSize <= 0)
			{
				builder.AppendLine(text);
				return;
			}

			for (var position = 0; position < text.Length; position += chunkSize)
			{
				if (position > 0)
				{
					builder.Append(' ', padding);
				}

				builder.Append(text, position, Math.Min(text.Length - position, chunkSize));
				builder.AppendLine();
			}
		}
		private int GetPadding(StringBuilder builder)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			var padding = 0;
			while (padding + 1 < builder.Length && builder[builder.Length - 1 - padding] != '\n')
				padding++;

			return padding;
		}
	}
}
