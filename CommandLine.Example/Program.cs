/*
	Copyright (c) 2020 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;
using System.ComponentModel;

namespace ConsoleApp.CommandLine.Example
{
	[Description("Usage: myApp SayHelloTo [--name] Mike")]
	public class Program
	{
		[Browsable(false)] // hide it from CommandLine.Describe method
		public static int Main()
		{
			var result = System.CommandLine.Run<Program>(System.CommandLine.Arguments, defaultCommandName: "Help");
			Console.ReadKey();
			return result;
		}

		[Description("Says hello to specified 'name'.")]
		public static int SayHelloTo(string name)
		{
			Console.WriteLine("Hello " + name + "!");
			return 0;
		}

		public static int Math(CommandLineArguments arguments)
		{
			return System.CommandLine.Run<MathCommands>(arguments, "Help");
		}

		[Description("Display this help.")]
		public static int Help(string commandToDescribe = null)
		{
			return System.CommandLine.Describe<Program>(commandToDescribe);
		}
	}
}

