using System;
using System.Collections.Generic;
using System.Linq;
using deniszykov.CommandLine.Binding;
using JetBrains.Annotations;

namespace deniszykov.CommandLine
{
	public sealed class CommandExecutionContext
	{
		[NotNull] public ICommandsBuilder CommandsBuilder{ get; }
		[NotNull] public Command Command { get; }
		[NotNull] public string[] Arguments { get; }
		[NotNull] public IServiceProvider ServiceProvider { get; }
		[NotNull] public CommandLineConfiguration Configuration { get; }
		[NotNull] public IDictionary<object, object> Properties { get; }

		public CommandExecutionContext(
			[NotNull] ICommandsBuilder commandsBuilder,
			[NotNull] Command command,
			[NotNull] string[] arguments,
			[NotNull] IServiceProvider serviceProvider,
			[NotNull] CommandLineConfiguration configuration,
			[NotNull] IDictionary<object, object> properties)
		{
			if (commandsBuilder == null) throw new ArgumentNullException(nameof(commandsBuilder));
			if (command == null) throw new ArgumentNullException(nameof(command));
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));
			if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
			if (configuration == null) throw new ArgumentNullException(nameof(configuration));
			if (properties == null) throw new ArgumentNullException(nameof(properties));

			this.CommandsBuilder = commandsBuilder;
			this.Command = command;
			this.Arguments = arguments;
			this.ServiceProvider = serviceProvider;
			this.Configuration = configuration;
			this.Properties = properties;
		}

		/// <inheritdoc />
		public override string ToString() => $"Command: {this.Command.Name}, Arguments: {string.Join(", ", Arguments.ToArray())}";
	}
}
