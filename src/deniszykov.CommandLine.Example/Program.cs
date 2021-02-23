using System;
using System.ComponentModel;

// ReSharper disable UnusedMember.Global

namespace deniszykov.CommandLine.Example
{
	[Description("This test application. Type /? for help.")]
	public class Program
	{
		[Browsable(false)] // hide it from Describe method
		public static int Main(string[] arguments)
		{
			var result = CommandLine
				.CreateFromArguments(arguments)
				.Use<Program>()
				.Run();
			return result;
		}

		// ### Basic Verb ###
		[Description("Says hello to specified 'name'.")]
		public static int Hello(string name)
		{
			Console.WriteLine("Hello " + name + "!");
			return 0;
		}

		// ### Sub Verb ###
		[Description("Math related verbs.")]
		public static int Math(ICommandLineBuilder subVerbBuilder)
		{
			return subVerbBuilder.Use<MathVerbs>()
				.Run();
		}
	}
}

