using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using deniszykov.CommandLine.Formatting;
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
			var helpTextProvider = (IHelpTextProvider)serviceProvider.GetService(typeof(IHelpTextProvider)) ?? new DefaultHelpTextProvider();

			var commandLine = new CommandLine(
				this.verbSetBuilder,
				this.arguments,
				this.configuration,
				typeConversionProvider,
				console,
				helpTextProvider,
				serviceProvider,
				this.properties
			);
			return commandLine;
		}

		/// <inheritdoc />
		public int Run()
		{
			try
			{
				return this.Build().RunAsync().Result;
			}
			catch (AggregateException aggregateException)
			{
				ExceptionDispatchInfo.Capture((aggregateException.InnerException ?? aggregateException)).Throw();
				throw;
			}
		}
		/// <inheritdoc />
		public Task<int> RunAsync()
		{
			return this.Build().RunAsync();
		}

		private IServiceProvider CreateDefaultServiceProviderFactory()
		{
			var defaultServiceProvider = new ServiceProvider();
			defaultServiceProvider.RegisterInstance(typeof(ITypeConversionProvider), new TypeConversionProvider());
			return defaultServiceProvider;
		}
	}
}