using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using deniszykov.CommandLine.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace deniszykov.CommandLine.Tests
{
	public class UnknownOptionsTests
	{
		private const int SUCCESS_EXIT_CODE = 0;
		private const int FAILURE_EXIT_CODE = 11;

		public class StrictTestApi
		{
			public static int Write(string schema, bool dryRun = false, [Alias("f")] bool force = false)
			{
				return SUCCESS_EXIT_CODE;
			}

			public static int List(string[] schema, bool dryRun = false)
			{
				return SUCCESS_EXIT_CODE;
			}

			public static int Lenient(ICommandLineBuilder subVerbBuilder)
			{
				return subVerbBuilder
					.Use<StrictSubVerbApi>()
					.Run();
			}

			public static int Strict(ICommandLineBuilder subVerbBuilder)
			{
				return subVerbBuilder
					.Configure(config => config.FailOnUnknownOptions = true)
					.Use<StrictSubVerbApi>()
					.Run();
			}

			public static int NonStrict(ICommandLineBuilder subVerbBuilder)
			{
				return subVerbBuilder
					.Configure(config => config.FailOnUnknownOptions = false)
					.Use<StrictSubVerbApi>()
					.Run();
			}
		}

		public class StrictSubVerbApi
		{
			public static int Write(string schema, bool dryRun = false)
			{
				return SUCCESS_EXIT_CODE;
			}
		}

		private readonly ITestOutputHelper output;

		public UnknownOptionsTests(ITestOutputHelper output)
		{
			this.output = output;
		}

		[Theory]
		[InlineData("Write", "--schema", "Q", "--dry-run")]
		[InlineData("Write", "--schema", "Q", "--bogus", "1")]
		[InlineData("Write", "--schema", "Q", "-x")]
		[InlineData("Write", "--dry-run", "--schema", "Q")]
		public void DefaultIgnoresUnknownOptionsTest(params string[] arguments)
		{
			var exitCode = this.Run(arguments, failOnUnknownOptions: false, out _, out _);

			Assert.Equal(SUCCESS_EXIT_CODE, exitCode);
		}

		[Theory]
		[InlineData("--dry-run", "Write", "--schema", "Q", "--dry-run")]
		[InlineData("--bogus", "Write", "--schema", "Q", "--bogus", "1")]
		[InlineData("-x", "Write", "--schema", "Q", "-x")]
		[InlineData("--dry-run", "Write", "--dry-run", "--schema", "Q")]
		public void StrictFailsOnUnknownOptionsTest(string unknownOption, params string[] arguments)
		{
			var exitCode = this.Run(arguments, failOnUnknownOptions: true, out var testConsole, out _);

			Assert.Equal(FAILURE_EXIT_CODE, exitCode);
			Assert.Contains(unknownOption, testConsole.Output.ToString(), StringComparison.Ordinal);
		}

		[Theory]
		[InlineData("--dry-run", "Write", "--schema", "Q", "--dry-run")]
		[InlineData("--dry-run", "List", "--schema", "Q", "R", "--dry-run")]
		public void StrictOverridesAllowPrefixesInOptionValuesTest(string unknownOption, params string[] arguments)
		{
			var exitCode = this.Run(arguments, failOnUnknownOptions: true, out var testConsole, out _, allowPrefixesInOptionValues: true);

			Assert.Equal(FAILURE_EXIT_CODE, exitCode);
			Assert.Contains(unknownOption, testConsole.Output.ToString(), StringComparison.Ordinal);
		}

		[Theory]
		[InlineData("Write", "--schema", "-HP=2 +A=1")]
		[InlineData("Write", "--schema=-HP=2")]
		[InlineData("List", "--schema", "Q", "R", "--dryRun")]
		public void StrictKeepsFirstOptionValueVerbatimTest(params string[] arguments)
		{
			var exitCode = this.Run(arguments, failOnUnknownOptions: true, out _, out _, allowPrefixesInOptionValues: true);

			Assert.Equal(SUCCESS_EXIT_CODE, exitCode);
		}

		[Theory]
		[InlineData("Write", "--schema", "Q", "--dryRun")]
		[InlineData("Write", "--schema", "Q", "--dryrun")]
		[InlineData("Write", "--schema", "Q", "-dryRun")]
		[InlineData("Write", "--schema", "Q", "--force")]
		[InlineData("Write", "--schema", "Q", "-f")]
		[InlineData("Write", "--schema=Q", "--dryRun", "true")]
		[InlineData("Write", "Q")]
		public void StrictAcceptsKnownOptionsTest(params string[] arguments)
		{
			var exitCode = this.Run(arguments, failOnUnknownOptions: true, out _, out _);

			Assert.Equal(SUCCESS_EXIT_CODE, exitCode);
		}

		[Fact]
		public void StrictThrowsWhenHelpOnFailureIsOffTest()
		{
			var exitCode = this.Run(["Write", "--schema", "Q", "--dry-run", "-x"], failOnUnknownOptions: true, out _, out var exceptions, outputHelpOnFailure: false);

			Assert.Equal(CommandLine.DOT_NET_EXCEPTION_EXIT_CODE, exitCode);
			var error = Assert.IsType<CommandLineException>(Assert.Single(exceptions));
			Assert.Equal("Write", error.Data["verb"]);
			Assert.Equal(new[] { "--dry-run", "-x" }, (IReadOnlyList<string>?)error.Data["unknownOptions"]);
		}

		[Fact]
		public void StrictHelpRequestShowsHelpTest()
		{
			var exitCode = this.Run(["Write", "--dry-run", "/?"], failOnUnknownOptions: true, out var testConsole, out _);

			Assert.NotEqual(FAILURE_EXIT_CODE, exitCode);
			Assert.DoesNotContain("Unknown option", testConsole.Output.ToString(), StringComparison.Ordinal);
		}

		[Theory]
		[InlineData(SUCCESS_EXIT_CODE, "Lenient", "Write", "--schema", "Q", "--dry-run")]
		[InlineData(FAILURE_EXIT_CODE, "Strict", "Write", "--schema", "Q", "--dry-run")]
		[InlineData(SUCCESS_EXIT_CODE, "Strict", "Write", "--schema", "Q", "--dryRun")]
		[InlineData(SUCCESS_EXIT_CODE, "Write", "--schema", "Q", "--dry-run")]
		public void StrictEnabledForSubVerbsOnlyTest(int expectedExitCode, params string[] arguments)
		{
			var exitCode = this.Run(arguments, failOnUnknownOptions: false, out _, out _);

			Assert.Equal(expectedExitCode, exitCode);
		}

		[Theory]
		[InlineData(SUCCESS_EXIT_CODE, "Lenient", "Write", "--schema", "Q")]
		[InlineData(FAILURE_EXIT_CODE, "Lenient", "Write", "--schema", "Q", "--dry-run")]
		[InlineData(SUCCESS_EXIT_CODE, "NonStrict", "Write", "--schema", "Q", "--dry-run")]
		public void StrictInheritedBySubVerbsTest(int expectedExitCode, params string[] arguments)
		{
			var exitCode = this.Run(arguments, failOnUnknownOptions: true, out _, out _);

			Assert.Equal(expectedExitCode, exitCode);
		}

		private int Run(string[] arguments, bool failOnUnknownOptions, out TestConsole testConsole, out List<Exception> exceptions, bool outputHelpOnFailure = true, bool allowPrefixesInOptionValues = false)
		{
			var console = new TestConsole(this.output);
			var errors = new List<Exception>();
			var exitCode = CommandLine.CreateFromArguments(arguments)
				.Configure(config =>
				{
					config.UnhandledExceptionHandler += (sender, args) =>
					{
						errors.Add(args.Exception);
						this.output.WriteLine(args.Exception.ToString());
					};
					config.OutputHelpOnFailure = outputHelpOnFailure;
					config.HelpExitCode = 1;
					config.FailureExitCode = FAILURE_EXIT_CODE;
					config.LongOptionNamePrefixes = ["--"];
					config.ShortOptionNamePrefixes = ["-"];
					config.AllowPrefixesInOptionValues = allowPrefixesInOptionValues;
					config.FailOnUnknownOptions = failOnUnknownOptions;
				})
				.UseServiceProvider(() =>
				{
					var services = new ServiceContainer();
					services.AddService(typeof(IConsole), console);
					return services;
				})
				.Use<StrictTestApi>()
				.Run();

			testConsole = console;
			exceptions = errors;
			return exitCode;
		}
	}
}
