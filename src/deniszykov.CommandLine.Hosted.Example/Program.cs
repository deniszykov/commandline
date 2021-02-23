using System.Threading.Tasks;
using deniszykov.CommandLine.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace deniszykov.CommandLine.Hosted.Example
{
	internal class Program
	{
		private static async Task Main(string[] args)
		{
			await CreateHostBuilder(args).Build().RunAsync();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureServices((context, services) =>
				{
					services.Configure<CommandLineConfiguration>(context.Configuration.GetSection("CommandLine"));
				})
				.ConfigureCommandLineHost(args, builder =>
				{
					builder.Use<Program>();
				});

		/// <summary>
		/// This is a default verb. It is used then no verb is specified in command line arguments. Name of default verb is specified in appsettings.json and could be changed in <see cref="CommandLineConfiguration"/>.
		/// </summary>
		/// <param name="console">This is <see cref="IConsole"/> service which is injected from DI container.</param>
		/// <param name="context">This is special "contextual" type which is injected by <see cref="CommandLine"/> class.</param>
		/// <returns>Exit code.</returns>
		public int Default([FromService] IConsole console, VerbExecutionContext context)
		{
			// avoid using real System.Console because it is can't be mocked for tests
			console.WriteLine("Type '/?' for help.");

			// exit code is taken from config
			return context.Configuration.HelpExitCode;
		}
	}
}
