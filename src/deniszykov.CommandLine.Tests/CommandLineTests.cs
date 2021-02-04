using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using deniszykov.CommandLine.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace deniszykov.CommandLine.Tests
{
	public class CommandLineTests
	{
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

		[Description("This is test api!")]
		public class DescribeTestApi
		{

			[Description("This is test command description. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc sit amet turpis at ex malesuada facilisis sed ac eros. Suspendisse pretium congue quam non dapibus. Pellentesque vel consequat mi. Vestibulum id bibendum augue, a pellentesque erat. Integer vel tempor lacus. Duis ut sapien non nulla interdum cursus. Maecenas vel laoreet lectus. ")]
			public static int TestCommand()
			{
				return 0;
			}
			[Description("This is test command description.")]
			public static int TestCommand(int param1)
			{
				Assert.Equal(1, param1);
				return 0;
			}

			[Description("This is test command with multiple params description.")]
			public static int MultiparamCommand(
				[Description("This is parameter1 description. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc sit amet turpis at ex malesuada facilisis sed ac eros.")]
				[Alias("p1")]
				[Name("param1Renamed")]
				int param1,
				string param2,
				[Description("This is parameter3 description.")]
				MyFlags param3,
				[Description("This is parameter4 description.")]
				MyFlags param4 = MyFlags.One,
				[Description("This is parameter3 description.")]
				string param5 = null,
				[Description("This is parameter3 description."), Hidden]
				string param6Hidden = null
			)
			{
				Assert.Equal(1, param1);
				return 0;
			}

			[Description("This is hidden command.")]
			public static int HiddenCommand()
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
		[InlineData(new string[0], "NoParameters")]
		[InlineData(new[] { "--param1", "1" }, "IntParameter")]
		[InlineData(new[] { "--param1", "-1" }, "IntNegativeParameter")]
		[InlineData(new[] { "--", "-1" }, "IntNegativeParameter")]
		[InlineData(new[] { "--param1", "true" }, "BoolParameter")]
		[InlineData(new string[0], "BoolFalseParameter")]
		[InlineData(new[] { "--param1", "false" }, "BoolFalseParameter")]
		[InlineData(new[] { "--param1", "param" }, "StringParameter")]
		[InlineData(new[] { "-", "-param" }, "StringHyphenParameter")]
		[InlineData(new string[0], "StringNullParameter")]
		[InlineData(new[] { "--param1" }, "StringNullParameter")]
		[InlineData(new[] { "--param1", "param0", "param1" }, "StringArrayTwoParameter")]
		[InlineData(new[] { "--param1", "param0" }, "StringArrayOneParameter")]
		[InlineData(new[] { "--param1" }, "StringArrayZeroParameter")]
		[InlineData(new string[0], "StringArrayNullParameter")]
		[InlineData(new[] { "--param1", "2" }, "EnumTwoParameter")]
		[InlineData(new[] { "--param1", "Two" }, "EnumTwoParameter")]
		[InlineData(new[] { "--param1" }, "EnumZeroParameter")]
		[InlineData(new string[0], "EnumZeroParameter")]
		[InlineData(new[] { "--param1", "3" }, "FlagsThreeParameter")]
		[InlineData(new[] { "--param1", "One,Two" }, "FlagsThreeParameter")]
		[InlineData(new[] { "--param1", "One", "Two" }, "FlagsThreeParameter")]
		[InlineData(new[] { "--param1", "2" }, "FlagsTwoParameter")]
		[InlineData(new[] { "--param1", "Two" }, "FlagsTwoParameter")]
		[InlineData(new[] { "--param1" }, "FlagsZeroParameter")]
		[InlineData(new string[0], "FlagsZeroParameter")]
		[InlineData(new[] { "--", "-1", "-param2" }, "IntNegativeStringParameter")]
		[InlineData(new[] { "-", "-1", "-param2" }, "IntNegativeStringParameter")]
		[InlineData(new[] { "--param1", "-1", "-", "--param2", "-param2" }, "IntNegativeStringParameter")]
		[InlineData(new[] { "-1", "-", "--param2", "-param2" }, "IntNegativeStringParameter")]
		[InlineData(new[] { "-1", "--", "-param2" }, "IntNegativeStringParameter")]
		[InlineData(new[] { "--", "-1", "-param2", "--" }, "IntNegativeStringParameter")]
		[InlineData(new[] { "--", "-1", "-param2", "-" }, "IntNegativeStringParameter")]
		[InlineData(new[] { "-", "--", "-1", "-param2", "-" }, "IntNegativeStringParameter")]
		public void BindTest(string[] commandLineArguments, string command)
		{
			var exitCode = CommandLine.CreateFromArguments(commandLineArguments)
				.Configure(config =>
				{
					config.UnhandledExceptionHandler += (sender, args) => this.output.WriteLine(args.Exception.ToString());
					config.DescribeOnBindFailure = false;
					config.DefaultCommandName = command;
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

		[Theory]
		[InlineData(nameof(DescribeTestApi.TestCommand))]
		[InlineData(nameof(DescribeTestApi.MultiparamCommand))]
		public void DescribeTest(string command)
		{
			var exitCode = CommandLine.CreateFromArguments(command)
				.Configure(config =>
				{
					config.UnhandledExceptionHandler += (sender, args) => this.output.WriteLine(args.Exception.ToString());
					config.DescribeOnBindFailure = false;
					config.DefaultCommandName = command;
					config.DescribeExitCode = 0;
				})
				.UseServiceProvider(() =>
				{
					var testConsole = new TestConsole(this.output);
					var services = new ServiceContainer();
					services.AddService(typeof(IConsole), testConsole);
					return services;
				})
				.Use<DescribeTestApi>()
				.Describe(command);

			Assert.Equal(0, exitCode);
		}
	}
}
