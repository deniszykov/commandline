using System;
using System.Text;
using System.Threading;
using Xunit.Abstractions;

namespace deniszykov.CommandLine.Tests
{
	public class TestConsole : IConsole
	{
		private readonly ITestOutputHelper testOutputHelper;

		public readonly StringBuilder Output;
		public readonly StringBuilder Error;

		/// <inheritdoc />
		public CancellationToken InterruptToken => CancellationToken.None;

		public TestConsole(ITestOutputHelper testOutputHelper)
		{
			if (testOutputHelper == null) throw new ArgumentNullException(nameof(testOutputHelper));

			this.testOutputHelper = testOutputHelper;
			this.Output = new StringBuilder();
			this.Error = new StringBuilder();
		}


		/// <inheritdoc />
		public void WriteLine(object? text = null)
		{
			var textStr = Convert.ToString(text) ?? string.Empty;
			this.testOutputHelper.WriteLine(textStr);
			this.Output.AppendLine(textStr);
		}

		/// <inheritdoc />
		public void WriteErrorLine(object? text = null)
		{
			var textStr = Convert.ToString(text) ?? string.Empty;
			this.testOutputHelper.WriteLine(textStr);
			this.Error.AppendLine(textStr);
		}
	}
}
