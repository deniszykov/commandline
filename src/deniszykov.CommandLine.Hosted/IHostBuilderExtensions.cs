using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace deniszykov.CommandLine.Hosted
{
	/// <summary>
	/// Extensions for <see cref="IHostBuilder"/> integrating <see cref="CommandLine"/> into a generic .NET host.
	/// </summary>
	// ReSharper disable once InconsistentNaming
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public static class IHostBuilderExtensions
	{
		/// <summary>
		/// Register <see cref="CommandLine"/> into specified <paramref name="hostBuilder"/> using process command line arguments
		/// (taken from <see cref="Environment.GetCommandLineArgs"/> without the executable path).
		/// </summary>
		/// <param name="hostBuilder">Host builder to configure.</param>
		/// <param name="buildDelegate">Delegate used to configure <see cref="ICommandLineBuilder"/> (verbs, configuration).</param>
		/// <returns>Same instance of <paramref name="hostBuilder"/> for chaining.</returns>
		public static IHostBuilder ConfigureCommandLineHost(this IHostBuilder hostBuilder, Action<ICommandLineBuilder> buildDelegate)
		{
			if (hostBuilder == null) throw new ArgumentNullException(nameof(hostBuilder));
			if (buildDelegate == null) throw new ArgumentNullException(nameof(buildDelegate));

			return ConfigureCommandLineHost(hostBuilder, null, buildDelegate);
		}
		/// <summary>
		/// Register <see cref="CommandLine"/> into specified <paramref name="hostBuilder"/> using provided <paramref name="arguments"/>.
		/// </summary>
		/// <param name="hostBuilder">Host builder to configure.</param>
		/// <param name="arguments">Command line arguments to bind. When <c>null</c>, falls back to <see cref="Environment.GetCommandLineArgs"/> without the executable path.</param>
		/// <param name="buildDelegate">Delegate used to configure <see cref="ICommandLineBuilder"/> (verbs, configuration).</param>
		/// <returns>Same instance of <paramref name="hostBuilder"/> for chaining.</returns>
		public static IHostBuilder ConfigureCommandLineHost(this IHostBuilder hostBuilder, string[]? arguments, Action<ICommandLineBuilder> buildDelegate)
		{
			if (hostBuilder == null) throw new ArgumentNullException(nameof(hostBuilder));
			if (buildDelegate == null) throw new ArgumentNullException(nameof(buildDelegate));

			arguments ??= Environment.GetCommandLineArgs().Skip(1).ToArray();

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
