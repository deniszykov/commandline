using System;
using System.ComponentModel;

namespace deniszykov.CommandLine.Example
{
	[Description("Usage: myApp SayHelloTo [--name] Mike")]
	public class Program
	{
		[Browsable(false)] // hide it from Describe method
		public static int Main(string[] arguments)
		{
			var result = CommandLine
				.CreateFromArguments(arguments)
				.Configure(config =>
				{
					config.DefaultVerbName = nameof(Help);
				})
				.Use<Program>()
				.Run();
			return result;
		}

		// ### Basic Verb ###
		[Description("Says hello to specified 'name'.")]
		public static int SayHelloTo(string name)
		{
			Console.WriteLine("Hello " + name + "!");
			return 0;
		}

		// ### Sub Verb ###
		[Description("Math related verbs.")]
		public static int Math(VerbExecutionContext context)
		{
			return CommandLine
				.CreateFromContext(context)
				.Use<MathVerbs>()
				.Run();
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

