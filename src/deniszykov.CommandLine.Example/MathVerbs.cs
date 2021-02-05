using System;
using System.ComponentModel;
using System.Linq;

namespace deniszykov.CommandLine.Example
{
	public class MathVerbs
	{
		// ### Basic Verb ###
		[Description("Adds two numbers and print result.")]
		public static int Add(int value1, int value2)
		{
			Console.WriteLine(value1 + value2);
			return 0;
		}

		// ### Basic Verb ###
		[Description("Adds numbers and print result.")]
		public static int Add(int[] values)
		{
			Console.WriteLine(values.Sum());
			return 0;
		}

		// ### Help Verb ###
		[Description("Display this help.")]
		public static int Help(VerbExecutionContext context, string verbName = null)
		{
			return CommandLine
				.CreateFromContext(context)
				.Describe(verbName);
		}
	}
}
