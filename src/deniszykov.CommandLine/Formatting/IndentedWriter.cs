using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace deniszykov.CommandLine.Formatting
{
	internal class IndentedWriter
	{
		private class Indent : IDisposable
		{
			private IndentedWriter? indentedWriter;

			public Indent(IndentedWriter indentedWriter)
			{
				this.indentedWriter = indentedWriter;
			}

			/// <inheritdoc />
			public void Dispose()
			{
				this.indentedWriter?.RestoreIndent();
				this.indentedWriter = null;
			}
		}

		private readonly StringBuilder output;
		private readonly Stack<int> indents;
		private readonly string newLine;
		private int indent;

		public IndentedWriter(string newLine)
		{
			if (newLine == null) throw new ArgumentNullException(nameof(newLine));

			this.newLine = newLine;
			this.output = new StringBuilder();
			this.indents = new Stack<int>();
			this.indent = 0;
		}

		public void Write(object? textObj = null)
		{
			this.EnsureIndent();

			this.AppendTextAndCountIndent(textObj);
		}
		public void WriteLine(object? textObj = null)
		{
			this.EnsureIndent();

			this.AppendTextAndCountIndent(textObj);
			this.output.Append(this.newLine);

			this.indent = 0;
		}

		public IDisposable KeepIndent(string? padding = null)
		{
			if (!string.IsNullOrEmpty(padding))
			{
				this.Write(padding);
			}
			this.indents.Push(this.indent);
			return new Indent(this);
		}
		private void RestoreIndent()
		{
			if (this.indents.Count == 0)
			{
				throw new InvalidOperationException("Invalid keep/restore indent balance.");
			}
			this.indents.Pop();
		}

		private void EnsureIndent()
		{
			if (this.indents.Count <= 0)
			{
				return;
			}

			var requiredIndent = this.indents.Peek() - this.indent;
			if (requiredIndent <= 0)
			{
				return;
			}

			this.output.Append(' ', requiredIndent);
			this.indent += requiredIndent;
		}
		private void AppendTextAndCountIndent(object? text)
		{
			var str = Convert.ToString(text, CultureInfo.InvariantCulture) ?? string.Empty;
			var offset = 0;
			var newLineIndex = -1;
			while ((newLineIndex = str.IndexOf(this.newLine, offset, StringComparison.Ordinal)) >= 0)
			{
				this.EnsureIndent();

				this.output.Append(str, offset, newLineIndex - offset);
				this.output.Append(this.newLine);
				this.indent = 0;
				offset = newLineIndex + this.newLine.Length;
			}

			this.EnsureIndent();
			this.indent += str.Length - offset;
			this.output.Append(str, offset, str.Length - offset);
		}

		/// <inheritdoc />
		public override string ToString() => this.output.ToString();
	}
}
