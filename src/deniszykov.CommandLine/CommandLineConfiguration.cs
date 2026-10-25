/*
	Copyright (c) 2024 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;
using System.Threading;
using JetBrains.Annotations;

namespace deniszykov.CommandLine
{
	/// <summary>
	/// Configuration for <see cref="CommandLine"/>.
	/// </summary>
	[PublicAPI]
	public sealed class CommandLineConfiguration
	{
		/// <summary>
		/// Exception handling for <see cref="CommandLine.RunAsync"/>.
		/// </summary>
		public ExceptionEventHandler? UnhandledExceptionHandler { get; set; }
		/// <summary>
		/// Try to describe API into <see cref="IConsole.WriteLine"/> when bind error occurs (verb name mistype or wrong arguments).
		/// </summary>
		public bool OutputHelpOnFailure { get; set; }
		/// <summary>
		/// Output title help text for sub-verbs. Usually it is taken from <see cref="Type"/> where verbs are declared.
		/// </summary>
		public bool OutputSubVerbHelpTitle { get; set; }
		/// <summary>
		/// Output whole error message to <see cref="IConsole.WriteErrorLine"/> when bind error occurs (verb name mistype or wrong arguments).
		/// </summary>
		public bool OutputDetailedErrors { get; set; }
		/// <summary>
		/// Exit code returned when binding fails (unknown verb, invalid or unknown options, no verb specified) and <see cref="OutputHelpOnFailure"/> is <code>true</code>. Defaults to <c>1</c>.
		/// </summary>
		public int FailureExitCode { get; set; }
		/// <summary>
		/// Exit code returned when help is requested with one of <see cref="HelpOptions"/>. Defaults to <c>2</c>.
		/// </summary>
		public int HelpExitCode { get; set; }
		/// <summary>
		/// Name of the verb to run when none is passed.
		/// </summary>
		public string? DefaultVerbName { get; set; }
		/// <summary>
		/// Get maximum width of terminal window in characters. Used to limit width of help text.
		/// </summary>
		public int MaxOutputWidth { get; set; }
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
		/// Set to <code>true</code> to treat unknown options as values. Otherwise they will be ignored during binding.
		/// </summary>
		public bool TreatUnknownOptionsAsValues { get; set; }
		/// <summary>
		/// Set to <code>true</code> to fail binding when an option matches no parameter of the verb, instead of ignoring it.
		/// Verbs with sub-verbs are not checked. Sub-verbs inherit this value; set it on the sub-verb's <see cref="ICommandLineBuilder.Configure"/> to enable it for one sub-verb set only.
		/// When enabled, <see cref="AllowPrefixesInOptionValues"/> is treated as <code>false</code>, so an unknown option is never captured as a value of the preceding option.
		/// </summary>
		public bool FailOnUnknownOptions { get; set; }
		/// <summary>
		/// List of prefixes for short options. Defaults to: "-", "/". Should be at least one prefix for short options.
		/// </summary>
		public string[]? ShortOptionNamePrefixes { get; set; }
		/// <summary>
		/// List of prefixes for long options. Defaults to: "--", "/". Should be at least one prefix for long options.
		/// </summary>
		public string[]? LongOptionNamePrefixes { get; set; }
		/// <summary>
		/// List of strings used to separate options from values. Defaults to: "--". Could be empty list.
		/// </summary>
		public string[]? OptionsBreaks { get; set; }
		/// <summary>
		/// List of options used to request help for verb/verbs. Defaults to: "-h", "/h", "--help", "-?", "/?". Could be empty list.
		/// </summary>
		public string[]? HelpOptions { get; set; }
		/// <summary>
		/// Splitter chars used to separate option's name from value. Defaults to: ' ', '='. Could be empty list.
		/// </summary>
		public char[]? OptionArgumentSplitters { get; set; }
		/// <summary>
		/// Allow option values starting with LongOptionNamePrefixes ('--', '/') prefixes.
		/// If set to false these values will be captured as unknown options. Defaults to <code>true</code>.
		/// </summary>
		public bool AllowPrefixesInOptionValues { get; set; }

		/// <summary>
		/// Create new instance of <see cref="CommandLineConfiguration"/> populated with default values.
		/// </summary>
		public CommandLineConfiguration()
		{
			this.OutputHelpOnFailure = true;
			this.FailureExitCode = 1;
			this.HelpExitCode = 2;
			this.ShortOptionNameMatchingMode = StringComparison.Ordinal;
			this.LongOptionNameMatchingMode = StringComparison.OrdinalIgnoreCase;
			this.VerbNameMatchingMode = StringComparison.OrdinalIgnoreCase;
			this.AllowPrefixesInOptionValues = true; 
			this.ShortOptionNamePrefixes = new[] { "-", "/" };
			this.LongOptionNamePrefixes = new[] { "--", "/" };
			this.OptionsBreaks = new[] { "--" };
			this.OptionArgumentSplitters = new[] { ' ', '=' };
			this.HelpOptions = new[] { "-h", "/h", "--help", "-?", "/?" };

			// Author: @MarcStan
			// fix when output is redirected, assume we can print any length and redirected
			// output takes care of formatting
			try
			{
				this.MaxOutputWidth = Console.WindowWidth;
			}
			catch { /*ignore error*/ }

			if (this.MaxOutputWidth <= 0) this.MaxOutputWidth = int.MaxValue;
		}

		internal void CopyTo(CommandLineConfiguration config)
		{
			if (config == null) throw new ArgumentNullException(nameof(config));

			config.UnhandledExceptionHandler = this.UnhandledExceptionHandler ?? config.UnhandledExceptionHandler;
			config.OutputHelpOnFailure = this.OutputHelpOnFailure;
			config.OutputDetailedErrors = this.OutputDetailedErrors;
			config.OutputSubVerbHelpTitle = this.OutputSubVerbHelpTitle;
			config.FailureExitCode = this.FailureExitCode;
			config.HelpExitCode = this.HelpExitCode;
			config.DefaultVerbName = this.DefaultVerbName ?? config.DefaultVerbName;
			config.HookConsoleCancelKeyPress = this.HookConsoleCancelKeyPress;
			config.LongOptionNameMatchingMode = this.LongOptionNameMatchingMode;
			config.ShortOptionNameMatchingMode = this.ShortOptionNameMatchingMode;
			config.VerbNameMatchingMode = this.VerbNameMatchingMode;
			config.TreatUnknownOptionsAsValues = this.TreatUnknownOptionsAsValues;
			config.FailOnUnknownOptions = this.FailOnUnknownOptions;
			config.ShortOptionNamePrefixes = this.ShortOptionNamePrefixes;
			config.LongOptionNamePrefixes = this.LongOptionNamePrefixes;
			config.OptionsBreaks = this.OptionsBreaks;
			config.HelpOptions = this.HelpOptions;
			config.MaxOutputWidth = this.MaxOutputWidth;
			config.OptionArgumentSplitters = this.OptionArgumentSplitters;
			config.AllowPrefixesInOptionValues = this.AllowPrefixesInOptionValues;
		}
	}
}
