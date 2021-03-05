using System;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace deniszykov.CommandLine.Hosted
{
	// ReSharper disable once InconsistentNaming
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public static class IHostBuilderExtensions
	{
		public static IHostBuilder ConfigureCommandLineHost(this IHostBuilder hostBuilder, Action<ICommandLineBuilder> buildDelegate)
		{
			if (hostBuilder == null) throw new ArgumentNullException(nameof(hostBuilder));
			if (buildDelegate == null) throw new ArgumentNullException(nameof(buildDelegate));

			return ConfigureCommandLineHost(hostBuilder, null, buildDelegate);
		}
		public static IHostBuilder ConfigureCommandLineHost(this IHostBuilder hostBuilder, string[]? arguments, Action<ICommandLineBuilder> buildDelegate)
		{
			if (hostBuilder == null) throw new ArgumentNullException(nameof(hostBuilder));
			if (buildDelegate == null) throw new ArgumentNullException(nameof(buildDelegate));

			arguments ??= Environment.GetCommandLineArgs();

			hostBuilder.UseConsoleLifetime(options =>
			{
				options.SuppressStatusMessages = true;
			});
			hostBuilder.ConfigureServices(services =>
			{
				services.AddSingleton(serviceProvider =>
				{
					var commandLineBuilder = CommandLine
						.CreateFromArguments(arguments)
						.UseServiceProvider(() => serviceProvider);

					if (serviceProvider.GetService(typeof(CommandLineConfiguration)) is CommandLineConfiguration commandLineConfiguration)
					{
						commandLineBuilder.Configure(commandLineConfiguration.CopyTo);
					}
					else if (serviceProvider.GetService(typeof(IOptions<CommandLineConfiguration>)) is IOptions<CommandLineConfiguration> commandLineConfigurationOptions)
					{
						commandLineBuilder.Configure(commandLineConfigurationOptions.Value.CopyTo);
					}

					buildDelegate.Invoke(commandLineBuilder);

					return commandLineBuilder.Build();
				});
				services.AddHostedService<CommandLineHostedService>();
			});

			return hostBuilder;
		}
	}
}
