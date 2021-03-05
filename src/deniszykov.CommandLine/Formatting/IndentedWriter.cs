/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

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
		private readonly int maxWidth;
		private int indent;

		public IndentedWriter(string newLine, int maxWidth = int.MaxValue)
		{
			if (newLine == null) throw new ArgumentNullException(nameof(newLine));
			if (maxWidth <= newLine.Length + 1) throw new ArgumentOutOfRangeException(nameof(maxWidth));

			this.newLine = newLine;
			this.maxWidth = maxWidth - newLine.Length;
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
			var textValue = Convert.ToString(text, CultureInfo.InvariantCulture) ?? string.Empty;
			var offset = 0;
			var newLineIndex = textValue.IndexOf(this.newLine, offset, StringComparison.Ordinal);
			var endOfText = false;
			do
			{
				if (newLineIndex < 0)
				{
					newLineIndex = textValue.Length;
					endOfText = true;
				}

				this.EnsureIndent();

				var length = newLineIndex - offset;
				var limited = false;
				if (length > this.maxWidth - this.indent)
				{
					length = this.maxWidth - this.indent;
					limited = true;
				}

				this.output.Append(textValue, offset, length);
				this.indent += length;
				offset += length;

				if (endOfText && offset >= textValue.Length)
				{
					break;
				}
				else
				{
					this.output.Append(this.newLine);
					this.indent = 0;
					if (!limited)
					{
						offset += this.newLine.Length;
					}
				}
			} while ((newLineIndex = textValue.IndexOf(this.newLine, offset, StringComparison.Ordinal)) < textValue.Length);
		}

		/// <inheritdoc />
		public override string ToString() => this.output.ToString();
	}
}
