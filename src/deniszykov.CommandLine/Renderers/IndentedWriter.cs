using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Renderers
{
	internal class IndentedWriter
	{
		[NotNull] private readonly StringBuilder output;
		[NotNull] private readonly Stack<int> indents;
		[NotNull] private readonly string newLine;
		private int indent;
		
		public IndentedWriter([NotNull] string newLine)
		{
			if (newLine == null) throw new ArgumentNullException(nameof(newLine));

			this.newLine = newLine;
			this.output = new StringBuilder();
			this.indents = new Stack<int>();
			this.indent = 0;
		}

		public void Write(object textObj = null)
		{
			this.EnsureIndent();

			this.output.Append(FormatTextAndCountIndent(textObj));
		}
		public void WriteLine(object textObj = null)
		{
			this.EnsureIndent();

			this.output.Append(FormatTextAndCountIndent(textObj));
			this.output.Append(this.newLine);

			this.indent = 0;
		}

		public void KeepIndent()
		{
			this.indents.Push(this.indent);
		}
		public void RestoreIndent()
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
		}
		private string FormatTextAndCountIndent(object text)
		{
			var str = Convert.ToString(text, CultureInfo.InvariantCulture) ?? string.Empty;
			this.indent += str.Length;
			return str;
		}

		/// <inheritdoc />
		public override string ToString() => this.output.ToString();
	}
}
