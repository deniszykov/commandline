using System;
using System.ComponentModel;

namespace MyAppExample
{
	[Description("Usage: myApp SayHelloTo [--name] Mike")]
	public class Program
	{
		[Browsable(false)] // hide it from CommandLine.Describe method
		public static int Main()
		{
			var result = CommandLine.Run<Program>(CommandLine.Arguments, defaultCommandName: "Help");
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
			return CommandLine.Run<MathCommands>(arguments, "Help");
		}

		[Description("Display this help.")]
		public static int Help(string actionToDescribe = null)
		{
			return CommandLine.Describe<Program>(actionToDescribe);
		}
	}
}

