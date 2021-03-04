using System;
using System.ComponentModel;

// ReSharper disable UnusedMember.Global

namespace deniszykov.CommandLine.Example
{
	[Description("This test application. Type /? for help.")]
	public class Program
	{
		private static int Main(string[] arguments)
		{
			var exitCode = CommandLine
				.CreateFromArguments(arguments)
				.Configure(config =>
				{
					config.UnhandledExceptionHandler += (sender, args) => Console.Error.WriteLine(args.Exception.ToString());
				})
				.Use<Program>()
				.Run();
			return exitCode;
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

