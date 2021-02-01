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
					config.DefaultCommandName = nameof(Help);
				})
				.Use<Program>()
				.Run();

			Console.ReadKey();
			return result;
		}

		// ### Basic Command ###
		[Description("Says hello to specified 'name'.")]
		public static int SayHelloTo(string name)
		{
			Console.WriteLine("Hello " + name + "!");
			return 0;
		}

		// ### Sub Command ###
		[Description("Math related commands.")]
		public static int Math(CommandExecutionContext context)
		{
			return CommandLine
				.CreateFromContext(context)
				.Use<MathCommands>()
				.Run();
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

