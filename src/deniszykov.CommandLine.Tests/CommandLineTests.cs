using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using deniszykov.CommandLine.Annotations;
using deniszykov.CommandLine.Formatting;
using Xunit;
using Xunit.Abstractions;

namespace deniszykov.CommandLine.Tests
{
	public class CommandLineTests
	{
		private const string TEST_API_DESCRIPTION = "This is test api!";
		private const string TEST_HELP_HEADER_TEXT = "##HEADER_TEXT##";
		private const string TEST_HELP_FOOTER_TEXT = "##FOOTER_TEXT##";

		[Flags]
		public enum MyFlags
		{
			Zero = 0x0,
			One = 0x1,
			Two = 0x2,
			Three = 0x2 | 0x1
		}

		public class TestApi
		{
			public static int NoParameters() { return 0; }
			public static int IntParameter(int param1)
			{
				Assert.Equal(1, param1);
				return 0;
			}
			public static int IntNegativeParameter(int param1)
			{
				Assert.Equal(-1, param1);
				return 0;
			}
			public static int IntNegativeStringParameter(int param1, string param2)
			{
				Assert.Equal(-1, param1);
				Assert.Equal("-param2", param2);
				return 0;
			}
			public static int BoolParameter(bool param1)
			{
				Assert.True(param1);
				return 0;
			}
			public static int BoolFalseParameter(bool param1)
			{
				Assert.False(param1);
				return 0;
			}
			public static int StringParameter(string param1)
			{
				Assert.Equal("param", param1);
				return 0;
			}
			public static int StringHyphenParameter(string param1)
			{
				Assert.Equal("-param", param1);
				return 0;
			}
			public static int StringNullParameter(string param1 = null)
			{
				Assert.Null(param1);
				return 0;
			}
			public static int StringEmptyParameter(string param1)
			{
				Assert.Equal("", param1);
				return 0;
			}
			public static int StringArrayTwoParameter(string[] param1)
			{
				Assert.Equal(2, param1.Length);
				Assert.Equal("param0", param1[0]);
				Assert.Equal("param1", param1[1]);
				return 0;
			}
			public static int StringArrayOneParameter(string[] param1)
			{
				Assert.Single(param1);
				Assert.Equal("param0", param1[0]);
				return 0;
			}
			public static int StringArrayZeroParameter(string[] param1)
			{
				Assert.Empty(param1);
				return 0;
			}
			public static int StringArrayNullParameter(string[] param1 = null)
			{
				Assert.Null(param1);
				return 0;
			}
			public static int EnumTwoParameter(MyFlags param1)
			{
				Assert.Equal(MyFlags.Two, param1);
				return 0;
			}
			public static int EnumZeroParameter(MyFlags param1 = 0)
			{
				Assert.Equal(MyFlags.Zero, param1);
				return 0;
			}
			public static int FlagsThreeParameter(MyFlags param1)
			{
				Assert.Equal(MyFlags.Three, param1);
				return 0;
			}
			public static int FlagsTwoParameter(MyFlags param1)
			{
				Assert.Equal(MyFlags.Two, param1);
				return 0;
			}
			public static int FlagsZeroParameter(MyFlags param1 = 0)
			{
				Assert.Equal(MyFlags.Zero, param1);
				return 0;
			}
		}

		internal class TestHelpTextProvider : DefaultHelpTextProvider, IHelpTextProvider
		{
			private readonly string helpHeaderText;
			private readonly string helpFooterText;

			/// <inheritdoc />
			string IHelpTextProvider.HelpHeaderText => this.helpHeaderText;
			/// <inheritdoc />
			string IHelpTextProvider.HelpFooterText => this.helpFooterText;

			public TestHelpTextProvider(string helpHeaderText, string helpFooterText)
			{
				this.helpHeaderText = helpHeaderText;
				this.helpFooterText = helpFooterText;
			}
		}

		[Description(TEST_API_DESCRIPTION)]
		public class DescribeTestApi
		{

			[Description("This is test verb description. Lorem ipsum dolor sit amet, consectetur adipiscing elit. \r\nNunc sit amet turpis at ex malesuada facilisis sed ac eros. \r\nSuspendisse pretium congue quam non dapibus. Pellentesque vel consequat mi. Vestibulum id bibendum augue, a pellentesque erat. Integer vel tempor lacus. Duis ut sapien non nulla interdum cursus. Maecenas vel laoreet lectus. ")]
			public static int TestVerb()
			{
				return 0;
			}

			[Description("This is test verb description.")]
			public static int TestVerb(int param1)
			{
				Assert.Equal(1, param1);
				return 0;
			}

			[Description("This is test sub verb description.")]
			public static int TestSubVerb(ICommandLineBuilder subVerbBuilder)
			{
				return subVerbBuilder
					.Use<DescribeTestApi>()
					.Run();
			}

			[Description("This is test verb description.")]
			public static int TestVerbOneParam(int param1)
			{
				Assert.Equal(1, param1);
				return 0;
			}


			[Description("This is test verb with multiple params description.")]
			public static int MultiparamVerb(
				[Description("This is parameter1 description. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc sit amet turpis at ex malesuada facilisis sed ac eros.")]
				[Alias("p")]
				[Name("param-renamed")]
				int param1,
				string param2,
				[Description("This is parameter3 description.")]
				MyFlags param3,
				[Description("This is parameter4 description.")]
				MyFlags param4 = MyFlags.One,
				[Description("This is parameter3 description.")]
				string param5 = null,
				[Description("This is parameter3 description."), Hidden]
				string param6Hidden = null,
				[Description("This is rest parameters.")]
				params string[] otherParams
			)
			{
				Assert.Equal(1, param1);
				return 0;
			}

			[Description("This is hidden verb."), Hidden]
			public static int HiddenVerb()
			{
				return 0;
			}
		}

		private readonly ITestOutputHelper output;
		public CommandLineTests(ITestOutputHelper output)
		{
			this.output = output;
		}

		[Theory]
		[InlineData(new string[0], nameof(TestApi.NoParameters))]
		[InlineData(new[] { "--param1", "1" }, nameof(TestApi.IntParameter))]
		[InlineData(new[] { "--param1", "-1" }, nameof(TestApi.IntNegativeParameter))]
		[InlineData(new[] { "--", "-1" }, nameof(TestApi.IntNegativeParameter))]
		[InlineData(new[] { "--param1", "true" }, nameof(TestApi.BoolParameter))]
		[InlineData(new string[0], nameof(TestApi.BoolFalseParameter))]
		[InlineData(new[] { "--param1", "false" }, nameof(TestApi.BoolFalseParameter))]
		[InlineData(new[] { "--param1", "param" }, nameof(TestApi.StringParameter))]
		[InlineData(new[] { "--", "-param" }, nameof(TestApi.StringHyphenParameter))]
		[InlineData(new string[0], nameof(TestApi.StringNullParameter))]
		[InlineData(new[] { "--param1", "" }, nameof(TestApi.StringEmptyParameter))]
		[InlineData(new[] { "--param1", "param0", "param1" }, nameof(TestApi.StringArrayTwoParameter))]
		[InlineData(new[] { "--param1", "param0" }, nameof(TestApi.StringArrayOneParameter))]
		[InlineData(new[] { "--param1" }, nameof(TestApi.StringArrayZeroParameter))]
		[InlineData(new string[0], nameof(TestApi.StringArrayNullParameter))]
		[InlineData(new[] { "--param1", "2" }, nameof(TestApi.EnumTwoParameter))]
		[InlineData(new[] { "--param1", "Two" }, nameof(TestApi.EnumTwoParameter))]
		[InlineData(new[] { "--param1", "0" }, nameof(TestApi.EnumZeroParameter))]
		[InlineData(new string[0], nameof(TestApi.EnumZeroParameter))]
		[InlineData(new[] { "--param1", "3" }, nameof(TestApi.FlagsThreeParameter))]
		[InlineData(new[] { "--param1", "One,Two" }, nameof(TestApi.FlagsThreeParameter))]
		[InlineData(new[] { "--param1=One,Two" }, nameof(TestApi.FlagsThreeParameter))]
		[InlineData(new[] { "--param1", "2" }, nameof(TestApi.FlagsTwoParameter))]
		[InlineData(new[] { "--param1", "Two" }, nameof(TestApi.FlagsTwoParameter))]
		[InlineData(new[] { "--param1", "0" }, nameof(TestApi.FlagsZeroParameter))]
		[InlineData(new string[0], nameof(TestApi.FlagsZeroParameter))]
		[InlineData(new[] { "--", "-1", "-param2" }, nameof(TestApi.IntNegativeStringParameter))]
		[InlineData(new[] { "--param1", "-1", "--param2", "-param2" }, nameof(TestApi.IntNegativeStringParameter))]
		[InlineData(new[] { "-1", "--param2", "-param2" }, nameof(TestApi.IntNegativeStringParameter))]
		[InlineData(new[] { "-1", "--", "-param2" }, nameof(TestApi.IntNegativeStringParameter))]
		[InlineData(new[] { "--", "-1", "-param2", "--" }, nameof(TestApi.IntNegativeStringParameter))]
		[InlineData(new[] { "--", "-1", "-param2", "--" }, nameof(TestApi.IntNegativeStringParameter))]
		[InlineData(new[] { "--", "-1", "-param2", "--" }, nameof(TestApi.IntNegativeStringParameter))]
		public void BindTest(string[] arguments, string verbName)
		{
			var exitCode = CommandLine.CreateFromArguments(arguments)
				.Configure(config =>
				{
					config.UnhandledExceptionHandler += (sender, args) => this.output.WriteLine(args.Exception.ToString());
					config.WriteHelpOfFailure = false;
					config.DefaultVerbName = verbName;
				})
				.UseServiceProvider(() =>
				{
					var testConsole = new TestConsole(this.output);
					var services = new ServiceContainer();
					services.AddService(typeof(IConsole), testConsole);
					return services;
				})
				.Use<TestApi>()
				.Run();

			Assert.Equal(0, exitCode);
		}

		[Fact]
		public void FailedToBindTest()
		{
			var expectedExitCode = 11;
			var testConsole = new TestConsole(this.output);
			var helpTextProvider = new TestHelpTextProvider(TEST_HELP_HEADER_TEXT, TEST_HELP_FOOTER_TEXT);
			var exitCode = CommandLine.CreateFromArguments(new[] { nameof(DescribeTestApi.TestVerbOneParam), "aaa" })
				.Configure(config =>
				{
					config.UnhandledExceptionHandler += (sender, args) => this.output.WriteLine(args.Exception.ToString());
					config.WriteHelpOfFailure = true;
					config.HelpExitCode = 1;
					config.FailureExitCode = expectedExitCode;
				})
				.UseServiceProvider(() =>
				{
					var services = new ServiceContainer();
					services.AddService(typeof(IConsole), testConsole);
					services.AddService(typeof(IHelpTextProvider), helpTextProvider);
					return services;
				})
				.Use<DescribeTestApi>()
				.Run();

			Assert.Contains(TEST_HELP_HEADER_TEXT, testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
			Assert.Contains(TEST_HELP_FOOTER_TEXT, testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
			Assert.Equal(expectedExitCode, exitCode);
		}

		[Theory]
		[InlineData(nameof(DescribeTestApi.TestVerbOneParam))]
		[InlineData(nameof(DescribeTestApi.MultiparamVerb))]
		public void DescribeVerbHelpTest(string name)
		{

			var testConsole = new TestConsole(this.output);
			var helpTextProvider = new TestHelpTextProvider(TEST_HELP_HEADER_TEXT, TEST_HELP_FOOTER_TEXT);
			var expectedExitCode = 11;
			var exitCode = CommandLine.CreateFromArguments(new[] { name, "/?" })
				.Configure(config =>
				{
					config.UnhandledExceptionHandler += (sender, args) => this.output.WriteLine(args.Exception.ToString());
					config.WriteHelpOfFailure = false;
					config.HelpExitCode = expectedExitCode;
				})
				.UseServiceProvider(() =>
				{
					var services = new ServiceContainer();
					services.AddService(typeof(IConsole), testConsole);
					services.AddService(typeof(IHelpTextProvider), helpTextProvider);
					return services;
				})
				.Use<DescribeTestApi>()
				.Run();

			Assert.Equal(expectedExitCode, exitCode);
			Assert.Contains(name, testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
			Assert.Contains(TEST_HELP_HEADER_TEXT, testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
			Assert.Contains(TEST_HELP_FOOTER_TEXT, testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("param6Hidden", testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public void HelpSubVerbTest()
		{
			var testConsole = new TestConsole(this.output);
			var helpTextProvider = new TestHelpTextProvider(TEST_HELP_HEADER_TEXT, TEST_HELP_FOOTER_TEXT);
			var expectedExitCode = 11;
			var exitCode = CommandLine.CreateFromArguments(new[] { nameof(DescribeTestApi.TestSubVerb), nameof(DescribeTestApi.TestVerb), "/?" })
				.Configure(config =>
				{
					config.UnhandledExceptionHandler += (sender, args) => this.output.WriteLine(args.Exception.ToString());
					config.WriteHelpOfFailure = false;
					config.HelpExitCode = expectedExitCode;
				})
				.UseServiceProvider(() =>
				{
					var services = new ServiceContainer();
					services.AddService(typeof(IConsole), testConsole);
					services.AddService(typeof(IHelpTextProvider), helpTextProvider);
					return services;
				})
				.Use<DescribeTestApi>()
				.Run();

			Assert.Equal(expectedExitCode, exitCode);
			Assert.Contains(TEST_HELP_HEADER_TEXT, testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
			Assert.Contains(TEST_HELP_FOOTER_TEXT, testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
			Assert.Contains(nameof(DescribeTestApi.TestSubVerb) + " " + nameof(DescribeTestApi.TestVerb), testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public void ListVerbsHelpTest()
		{
			var testConsole = new TestConsole(this.output);
			var helpTextProvider = new TestHelpTextProvider(TEST_HELP_HEADER_TEXT, TEST_HELP_FOOTER_TEXT);
			var expectedExitCode = 11;
			var exitCode = CommandLine.CreateFromArguments(new[] { "/?" })
				.Configure(config =>
				{
					config.UnhandledExceptionHandler += (sender, args) => this.output.WriteLine(args.Exception.ToString());
					config.WriteHelpOfFailure = false;
					config.HelpExitCode = expectedExitCode;
				})
				.UseServiceProvider(() =>
				{
					var services = new ServiceContainer();
					services.AddService(typeof(IConsole), testConsole);
					services.AddService(typeof(IHelpTextProvider), helpTextProvider);
					return services;
				})
				.Use<DescribeTestApi>()
				.Run();

			Assert.Equal(expectedExitCode, exitCode);
			Assert.Contains(TEST_API_DESCRIPTION, testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
			Assert.Contains(TEST_HELP_HEADER_TEXT, testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
			Assert.Contains(TEST_HELP_FOOTER_TEXT, testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
			Assert.Contains(nameof(DescribeTestApi.TestSubVerb), testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
			Assert.Contains(nameof(DescribeTestApi.TestVerb), testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
			Assert.Contains(nameof(DescribeTestApi.TestVerbOneParam), testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain(nameof(DescribeTestApi.HiddenVerb), testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public void ListSubVerbsHelpTest()
		{
			var testConsole = new TestConsole(this.output);
			var helpTextProvider = new TestHelpTextProvider(TEST_HELP_HEADER_TEXT, TEST_HELP_FOOTER_TEXT);
			var expectedExitCode = 11;
			var exitCode = CommandLine.CreateFromArguments(new[] { nameof(DescribeTestApi.TestSubVerb), "/?" })
				.Configure(config =>
				{
					config.UnhandledExceptionHandler += (sender, args) => this.output.WriteLine(args.Exception.ToString());
					config.WriteHelpOfFailure = false;
					config.HelpExitCode = expectedExitCode;
				})
				.UseServiceProvider(() =>
				{
					var services = new ServiceContainer();
					services.AddService(typeof(IConsole), testConsole);
					services.AddService(typeof(IHelpTextProvider), helpTextProvider);
					return services;
				})
				.Use<DescribeTestApi>()
				.Run();

			Assert.Equal(expectedExitCode, exitCode);
			Assert.Contains(TEST_HELP_HEADER_TEXT, testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
			Assert.Contains(TEST_HELP_FOOTER_TEXT, testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
			Assert.Contains(nameof(DescribeTestApi.TestSubVerb) + " " + nameof(DescribeTestApi.TestVerb), testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain(nameof(DescribeTestApi.HiddenVerb), testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public void NoVerbSpecifiedTest()
		{
			var expectedExitCode = 11;
			var testConsole = new TestConsole(this.output);
			var helpTextProvider = new TestHelpTextProvider(TEST_HELP_HEADER_TEXT, TEST_HELP_FOOTER_TEXT);
			var exitCode = CommandLine.CreateFromArguments(new string[0])
				.Configure(config =>
				{
					config.UnhandledExceptionHandler += (sender, args) => this.output.WriteLine(args.Exception.ToString());
					config.WriteHelpOfFailure = true;
					config.HelpExitCode = 1;
					config.FailureExitCode = expectedExitCode;
				})
				.UseServiceProvider(() =>
				{
					var services = new ServiceContainer();
					services.AddService(typeof(IConsole), testConsole);
					services.AddService(typeof(IHelpTextProvider), helpTextProvider);
					return services;
				})
				.Use<DescribeTestApi>()
				.Run();

			Assert.Contains(TEST_HELP_HEADER_TEXT, testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
			Assert.Contains(TEST_HELP_FOOTER_TEXT, testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
			Assert.Equal(expectedExitCode, exitCode);
		}

		[Fact]
		public void VerbNotFoundHelpTest()
		{
			const string NOT_EXISTENT_VERB_NAME = "__XXX__";
			var expectedExitCode = 11;
			var testConsole = new TestConsole(this.output);
			var helpTextProvider = new TestHelpTextProvider(TEST_HELP_HEADER_TEXT, TEST_HELP_FOOTER_TEXT);
			var exitCode = CommandLine.CreateFromArguments(new[] { NOT_EXISTENT_VERB_NAME })
				.Configure(config =>
				{
					config.UnhandledExceptionHandler += (sender, args) => this.output.WriteLine(args.Exception.ToString());
					config.WriteHelpOfFailure = true;
					config.HelpExitCode = 1;
					config.FailureExitCode = expectedExitCode;
				})
				.UseServiceProvider(() =>
				{
					var services = new ServiceContainer();
					services.AddService(typeof(IConsole), testConsole);
					services.AddService(typeof(IHelpTextProvider), helpTextProvider);
					return services;
				})
				.Use<DescribeTestApi>()
				.Run();

			Assert.Contains(NOT_EXISTENT_VERB_NAME, testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
			Assert.Contains(TEST_HELP_HEADER_TEXT, testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
			Assert.Contains(TEST_HELP_FOOTER_TEXT, testConsole.Output.ToString(), StringComparison.OrdinalIgnoreCase);
			Assert.Equal(expectedExitCode, exitCode);
		}
	}
}
