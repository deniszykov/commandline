using System;
using Xunit;
using Xunit.Abstractions;

namespace ConsoleApp.CommandLine.Tests
{
	public class CommandLineTests
	{
		public class Api
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
		[Flags]
		public enum MyFlags
		{
			Zero = 0x0,
			One = 0x1,
			Two = 0x2,
			Three = 0x2 | 0x1
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
		public void BindTest(string[] commandLineArguments, string method)
		{
			System.CommandLine.UnhandledException += (sender, args) => output.WriteLine(args.Exception.ToString());
			System.CommandLine.DescribeOnBindFailure = false;

			var exitCode = System.CommandLine.Run<Api>(new CommandLineArguments(commandLineArguments), method);

			Assert.Equal(0, exitCode);
		}
	}
}
