using System;
using System.ComponentModel;
using System.Linq;

namespace deniszykov.CommandLine.Example
{
	public class MathCommands
	{
		// ### Basic Command ###
		[Description("Adds two numbers and print result.")]
		public static int Add(int value1, int value2)
		{
			Console.WriteLine(value1 + value2);
			return 0;
		}

		// ### Basic Command ###
		[Description("Adds numbers and print result.")]
		public static int Add(int[] values)
		{
			Console.WriteLine(values.Sum());
			return 0;
		}

		// ### Help Command ###
		[Description("Display this help.")]
		public static int Help(CommandExecutionContext context, string commandToDescribe = null)
		{
			return CommandLine
				.CreateFromContext(context)
				.Describe(commandToDescribe);
		}
	}
}
