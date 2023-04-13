/*
	Copyright (c) 2020 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;
using System.ComponentModel;
using System.Linq;

namespace deniszykov.CommandLine.Example
{
	[Description("Some math functions. (!) Set 'CommandLineConfiguration.OutputSubVerbHelpTitle=true' to display this text with --help.")]
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
		[Description("Multiplies numbers and print result.")]
		public static int Multiply(int[] values)
		{
			Console.WriteLine(values.Aggregate(1, (x,y) => x * y));
			return 0;
		}
	}
}
