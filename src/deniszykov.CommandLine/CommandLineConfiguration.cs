using System;
using System.Threading;
using JetBrains.Annotations;

namespace deniszykov.CommandLine
{
	public class CommandLineConfiguration
	{
		/// <summary>
		/// Exception handling for <see cref="CommandLine.Run"/> parameterInfo.
		/// </summary>
		public ExceptionEventHandler UnhandledExceptionHandler { get; set; }
		/// <summary>
		/// Try to describe API into <see cref="IConsole.WriteLine"/> when bind error occurs (command name mistype or wrong arguments).
		/// </summary>
		public bool DescribeOnBindFailure { get; set; }
		/// <summary>
		/// Output whole error message to <see cref="IConsole.WriteErrorLine"/> when bind error occurs (command name mistype or wrong arguments).
		/// </summary>
		public bool DetailedBindFailureMessage { get; set; }
		/// <summary>
		/// Exit code returned when unable to find command and help text is displayed instead. Defaults to <c>1</c>.
		/// </summary>
		public int BindFailureExitCode { get; set; }
		/// <summary>
		/// Exit code used by <see cref="CommandLine.Describe"/> method as return value. Defaults to <c>2</c>.
		/// </summary>
		public int DescribeExitCode { get; set; }
		/// <summary>
		/// Set default action name to use if non are passed.
		/// </summary>
		public string DefaultCommandName { get; set; }
		/// <summary>
		/// Set to true to hook <see cref="Console.CancelKeyPress"/> event and redirect it as <see cref="CancellationToken"/> for running command.
		/// </summary>
		public bool HookConsoleCancelKeyPress { get; set; }
		/// <summary>
		/// Character sequence used to indicate new line in current terminal. Defaults to <see cref="Environment.NewLine"/>.
		/// </summary>
		public string NewLine { get; set; }

		public void CopyTo([NotNull]CommandLineConfiguration config)
		{
			if (config == null) throw new ArgumentNullException(nameof(config));

			config.UnhandledExceptionHandler = this.UnhandledExceptionHandler;
			config.DescribeOnBindFailure = this.DescribeOnBindFailure;
			config.DetailedBindFailureMessage = this.DetailedBindFailureMessage;
			config.BindFailureExitCode = this.BindFailureExitCode;
			config.DescribeExitCode = this.DescribeExitCode;
			config.DefaultCommandName = this.DefaultCommandName;
		}
	}
}
