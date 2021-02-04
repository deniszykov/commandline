using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Xunit.Abstractions;

namespace deniszykov.CommandLine.Tests
{
	public class TestConsole : IConsole
	{
		private readonly ITestOutputHelper testOutputHelper;

		public int OutWritten;
		public int ErrorWritten;

		/// <inheritdoc />
		public CancellationToken InterruptToken => CancellationToken.None;

		public TestConsole(ITestOutputHelper testOutputHelper)
		{
			if (testOutputHelper == null) throw new ArgumentNullException(nameof(testOutputHelper));

			this.testOutputHelper = testOutputHelper;
		}


		/// <inheritdoc />
		public void WriteLine(object text = null)
		{
			var textStr = Convert.ToString(text) ?? string.Empty;
			this.testOutputHelper.WriteLine(textStr);
			this.OutWritten += textStr.Length;
		}

		/// <inheritdoc />
		public void WriteErrorLine(object text = null)
		{
			var textStr = Convert.ToString(text) ?? string.Empty;
			this.testOutputHelper.WriteLine(textStr);
			this.ErrorWritten += textStr.Length;
		}
	}
}
