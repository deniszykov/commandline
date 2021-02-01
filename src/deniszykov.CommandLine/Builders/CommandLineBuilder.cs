using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using deniszykov.CommandLine.Renderers;
using deniszykov.TypeConversion;

namespace deniszykov.CommandLine.Builders
{
	internal sealed class CommandLineBuilder : ICommandLineBuilder
	{
		private readonly Dictionary<object, object> properties;
		private readonly string[] commandLineArgs;
		private readonly CommandLineConfiguration configuration;
		private ICommandsBuilder commandsBuilder;

		private Func<IServiceProvider> serviceProviderFactory;

		/// <inheritdoc />
		public IDictionary<object, object> Properties => this.properties;

		public CommandLineBuilder(string[] commandLineArgs)
		{
			if (commandLineArgs == null) throw new ArgumentNullException(nameof(commandLineArgs));

			this.properties = new Dictionary<object, object>();
			this.commandLineArgs = commandLineArgs;
			this.commandsBuilder = default(ICommandsBuilder);
			this.configuration = new CommandLineConfiguration();
			this.serviceProviderFactory = this.CreateDefaultServiceProviderFactory;
		}

		/// <inheritdoc />
		public ICommandLineBuilder Configure(Action<CommandLineConfiguration> configureDelegate)
		{
			if (configureDelegate == null) throw new ArgumentNullException(nameof(configureDelegate));

			configureDelegate(this.configuration);
			return this;
		}
		/// <inheritdoc />
		public ICommandLineBuilder UseServiceProvider(Func<IServiceProvider> factory)
		{
			if (factory == null) throw new ArgumentNullException(nameof(factory));

			this.serviceProviderFactory = factory;
			return this;
		}
		/// <inheritdoc />
		public ICommandLineBuilder Use<CommandListT>()
		{
			var builder = new CommandsFromTypeBuilder(typeof(CommandListT).GetTypeInfo());
			this.commandsBuilder = builder;
			return this;
		}
		/// <inheritdoc />
		public ICommandLineBuilder Use(Type commandListType)
		{
			if (commandListType == null) throw new ArgumentNullException(nameof(commandListType));

			var builder = new CommandsFromTypeBuilder(commandListType.GetTypeInfo());
			this.commandsBuilder = builder;
			return this;
		}
		/// <inheritdoc />
		public ICommandLineBuilder Use(Func<ICommandsBuilder> buildDelegate)
		{
			if (buildDelegate == null) throw new ArgumentNullException(nameof(buildDelegate));

			var builder = buildDelegate();
			this.commandsBuilder = builder;
			return this;
		}

		public CommandLine Build()
		{
			var serviceProvider = this.serviceProviderFactory?.Invoke() ?? this.CreateDefaultServiceProviderFactory();
			var typeConversionProvider = (ITypeConversionProvider)serviceProvider.GetService(typeof(ITypeConversionProvider)) ?? new TypeConversionProvider();
			var commandLineArguments = new CommandLineArguments(typeConversionProvider, this.commandLineArgs);
			var console = (IConsole)serviceProvider.GetService(typeof(IConsole)) ?? new DefaultConsole(this.configuration.HookConsoleCancelKeyPress);

			var commandLine = new CommandLine(
				this.commandsBuilder,
				commandLineArguments,
				this.configuration,
				typeConversionProvider,
				console,
				serviceProvider,
				this.properties
			);
			return commandLine;
		}

		/// <inheritdoc />
		public int Run()
		{
			return this.Build().Run();
		}

		/// <inheritdoc />
		public int Describe(string commandToDescribe)
		{
			return this.Build().Describe(commandToDescribe);
		}

		private IServiceProvider CreateDefaultServiceProviderFactory()
		{
			var defaultServiceProvider = new ServiceProvider();
			defaultServiceProvider.RegisterInstance(typeof(ITypeConversionProvider), new TypeConversionProvider());
			return defaultServiceProvider;
		}
	}
}