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
		private readonly string[] arguments;
		private readonly CommandLineConfiguration configuration;
		private IVerbSetBuilder verbSetBuilder;

		private Func<IServiceProvider> serviceProviderFactory;

		/// <inheritdoc />
		public IDictionary<object, object> Properties => this.properties;

		public CommandLineBuilder(string[] arguments)
		{
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));

			this.properties = new Dictionary<object, object>();
			this.arguments = arguments;
			this.verbSetBuilder = default(IVerbSetBuilder);
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
		public ICommandLineBuilder Use<VerbSetT>()
		{
			var builder = new VerbsFromTypeBuilder(typeof(VerbSetT).GetTypeInfo());
			this.verbSetBuilder = builder;
			return this;
		}
		/// <inheritdoc />
		public ICommandLineBuilder Use(Type verSetType)
		{
			if (verSetType == null) throw new ArgumentNullException(nameof(verSetType));

			var builder = new VerbsFromTypeBuilder(verSetType.GetTypeInfo());
			this.verbSetBuilder = builder;
			return this;
		}
		/// <inheritdoc />
		public ICommandLineBuilder Use(Func<IVerbSetBuilder> buildDelegate)
		{
			if (buildDelegate == null) throw new ArgumentNullException(nameof(buildDelegate));

			var builder = buildDelegate();
			this.verbSetBuilder = builder;
			return this;
		}

		public CommandLine Build()
		{
			var serviceProvider = this.serviceProviderFactory?.Invoke() ?? this.CreateDefaultServiceProviderFactory();
			var typeConversionProvider = (ITypeConversionProvider)serviceProvider.GetService(typeof(ITypeConversionProvider)) ?? new TypeConversionProvider();
			var console = (IConsole)serviceProvider.GetService(typeof(IConsole)) ?? new DefaultConsole(this.configuration.HookConsoleCancelKeyPress);

			var commandLine = new CommandLine(
				this.verbSetBuilder,
				this.arguments,
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
		public int Describe(string verbToDescribe)
		{
			return this.Build().Describe(verbToDescribe);
		}

		private IServiceProvider CreateDefaultServiceProviderFactory()
		{
			var defaultServiceProvider = new ServiceProvider();
			defaultServiceProvider.RegisterInstance(typeof(ITypeConversionProvider), new TypeConversionProvider());
			return defaultServiceProvider;
		}
	}
}