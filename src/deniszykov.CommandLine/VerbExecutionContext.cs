using System;
using System.Collections.Generic;
using System.Linq;
using deniszykov.CommandLine.Binding;
using JetBrains.Annotations;

namespace deniszykov.CommandLine
{
	public sealed class VerbExecutionContext
	{
		[NotNull] public IVerbSetBuilder VerbSetBuilder{ get; }
		[NotNull] public Verb Verb { get; }
		[NotNull] public string[] Arguments { get; }
		[NotNull] public IServiceProvider ServiceProvider { get; }
		[NotNull] public CommandLineConfiguration Configuration { get; }
		[NotNull] public IDictionary<object, object> Properties { get; }

		public VerbExecutionContext(
			[NotNull] IVerbSetBuilder verbSetBuilder,
			[NotNull] Verb verb,
			[NotNull] string[] arguments,
			[NotNull] IServiceProvider serviceProvider,
			[NotNull] CommandLineConfiguration configuration,
			[NotNull] IDictionary<object, object> properties)
		{
			if (verbSetBuilder == null) throw new ArgumentNullException(nameof(verbSetBuilder));
			if (verb == null) throw new ArgumentNullException(nameof(verb));
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));
			if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
			if (configuration == null) throw new ArgumentNullException(nameof(configuration));
			if (properties == null) throw new ArgumentNullException(nameof(properties));

			this.VerbSetBuilder = verbSetBuilder;
			this.Verb = verb;
			this.Arguments = arguments;
			this.ServiceProvider = serviceProvider;
			this.Configuration = configuration;
			this.Properties = properties;
		}

		/// <inheritdoc />
		public override string ToString() => $"Verb: {this.Verb.Name}, Arguments: {string.Join(", ", Arguments.ToArray())}";
	}
}
