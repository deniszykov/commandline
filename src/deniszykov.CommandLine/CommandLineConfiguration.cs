using System;
using System.Threading;
using JetBrains.Annotations;

namespace deniszykov.CommandLine
{
	/// <summary>
	/// Configuration for <see cref="CommandLine"/>.
	/// </summary>
	public sealed class CommandLineConfiguration
	{
		/// <summary>
		/// Exception handling for <see cref="CommandLine.Run"/>.
		/// </summary>
		[CanBeNull] public ExceptionEventHandler UnhandledExceptionHandler { get; set; }
		/// <summary>
		/// Try to describe API into <see cref="IConsole.WriteLine"/> when bind error occurs (verb name mistype or wrong arguments).
		/// </summary>
		public bool WriteHelpOfFailure { get; set; }
		/// <summary>
		/// Output whole error message to <see cref="IConsole.WriteErrorLine"/> when bind error occurs (verb name mistype or wrong arguments).
		/// </summary>
		public bool WriteFailureErrors { get; set; }
		/// <summary>
		/// Exit code returned when unable to find verb and help text is displayed instead. Defaults to <c>1</c>.
		/// </summary>
		public int FailureExitCode { get; set; }
		/// <summary>
		/// Exit code used by <see cref="CommandLine.WriteHelp"/> method as return value. Defaults to <c>2</c>.
		/// </summary>
		public int HelpExitCode { get; set; }
		/// <summary>
		/// Set default action name to use if non are passed.
		/// </summary>
		[CanBeNull] public string DefaultVerbName { get; set; }
		/// <summary>
		/// Set to true to hook <see cref="Console.CancelKeyPress"/> event and redirect it as <see cref="CancellationToken"/> for running verb.
		/// </summary>
		public bool HookConsoleCancelKeyPress { get; set; }
		/// <summary>
		/// String matching mode for long option's names.
		/// </summary>
		public StringComparison LongOptionNameMatchingMode { get; set; }
		/// <summary>
		/// String matching mode for short option's names.
		/// </summary>
		public StringComparison ShortOptionNameMatchingMode { get; set; }
		/// <summary>
		/// String matching mode for verb's names.
		/// </summary>
		public StringComparison VerbNameMatchingMode { get; set; }
		/// <summary>
		/// Set to <code>true</code> to threat unknown options as values. Over-wise they will be ignored during binding.
		/// </summary>
		public bool TreatUnknownOptionsAsValues { get; set; }
		/// <summary>
		/// List of prefixes for short options. Should be at least one prefix for short options.
		/// </summary>
		[CanBeNull, ItemNotNull] public string[] ShortOptionNamePrefixes { get; set; }
		/// <summary>
		/// List of prefixes for short options. Should be at least one prefix for long options.
		/// </summary>
		[CanBeNull, ItemNotNull] public string[] LongOptionNamePrefixes { get; set; }
		/// <summary>
		/// List of string used to separate options from values. Could be empty list.
		/// </summary>
		[CanBeNull, ItemNotNull] public string[] OptionsBreaks { get; set; }
		/// <summary>
		/// List of options used to request help. Could be empty list.
		/// </summary>
		[CanBeNull, ItemNotNull] public string[] HelpOptions { get; set; }
		/// <summary>
		/// Splitter chars used to separate option's name from value. Could be empty list.
		/// </summary>
		[CanBeNull] public char[] OptionArgumentSplitters { get; set; }

		internal void CopyTo([NotNull]CommandLineConfiguration config)
		{
			if (config == null) throw new ArgumentNullException(nameof(config));

			config.UnhandledExceptionHandler = this.UnhandledExceptionHandler;
			config.WriteHelpOfFailure = this.WriteHelpOfFailure;
			config.WriteFailureErrors = this.WriteFailureErrors;
			config.FailureExitCode = this.FailureExitCode;
			config.HelpExitCode = this.HelpExitCode;
			config.DefaultVerbName = this.DefaultVerbName;
			config.HookConsoleCancelKeyPress = this.HookConsoleCancelKeyPress;
			config.LongOptionNameMatchingMode = this.LongOptionNameMatchingMode;
			config.ShortOptionNameMatchingMode = this.ShortOptionNameMatchingMode;
			config.VerbNameMatchingMode = this.VerbNameMatchingMode;
			config.TreatUnknownOptionsAsValues = this.TreatUnknownOptionsAsValues;
			config.ShortOptionNamePrefixes = this.ShortOptionNamePrefixes;
			config.LongOptionNamePrefixes = this.LongOptionNamePrefixes;
			config.OptionsBreaks = this.OptionsBreaks;
			config.HelpOptions = this.HelpOptions;
			config.OptionArgumentSplitters = this.OptionArgumentSplitters;
		}

		internal void SetToDefault()
		{
			this.WriteHelpOfFailure = true;
			this.FailureExitCode = 1;
			this.HelpExitCode = 2;
			this.ShortOptionNameMatchingMode = StringComparison.Ordinal;
			this.LongOptionNameMatchingMode = StringComparison.OrdinalIgnoreCase;
			this.VerbNameMatchingMode = StringComparison.OrdinalIgnoreCase;
			this.ShortOptionNamePrefixes = new[] { "-", "/" };
			this.LongOptionNamePrefixes = new[] { "--", "/" };
			this.OptionsBreaks = new[] { "--" };
			this.OptionArgumentSplitters = new[] { ' ', '=' };
			this.HelpOptions = new[] { "-h", "/h", "--help", "-?", "/?" };
		}
	}
}
