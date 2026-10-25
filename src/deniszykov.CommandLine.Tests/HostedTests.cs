#if NET
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using deniszykov.CommandLine.Hosted;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace deniszykov.CommandLine.Tests
{
	public class HostedTests
	{
		public class CaptureApi
		{
			public static string[]? CapturedArguments;

			public static int Capture(VerbExecutionContext context, params string[] values)
			{
				CapturedArguments = context.Arguments;
				return 0;
			}
		}

		[Fact]
		public async Task ProcessArgumentsExcludeExecutablePathTest()
		{
			CaptureApi.CapturedArguments = null;
			using var host = new HostBuilder()
				.ConfigureCommandLineHost(builder => builder
					.Configure(config =>
					{
						config.DefaultVerbName = nameof(CaptureApi.Capture);
						config.TreatUnknownOptionsAsValues = true;
					})
					.Use<CaptureApi>())
				.Build();

			var exitCode = await host.Services.GetRequiredService<CommandLine>().RunAsync(CancellationToken.None);

			Assert.Equal(0, exitCode);
			Assert.Equal(Environment.GetCommandLineArgs().Skip(1).ToArray(), CaptureApi.CapturedArguments);
		}
	}
}
#endif
