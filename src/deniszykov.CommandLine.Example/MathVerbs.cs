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
	}
}
