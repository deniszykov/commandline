using System;
using System.Collections.Generic;
using System.Linq;
using deniszykov.CommandLine.Binding;

namespace deniszykov.CommandLine
{
	/// <summary>
	/// Contextual information about currently executed verb.
	/// </summary>
	public sealed class VerbExecutionContext
	{
		/// <summary>
		/// Current <see cref="Binding.Verb"/> set provider.
		/// </summary>
		public IVerbSetBuilder VerbSetBuilder { get; }
		/// <summary>
		/// Current executing verb.
		/// </summary>
		public Verb Verb { get; }
		/// <summary>
		/// Arguments passed to <see cref="Verb"/>.
		/// </summary>
		public string[] Arguments { get; }
		/// <summary>
		/// Service provider instance used to resolve services.
		/// </summary>
		public IServiceProvider ServiceProvider { get; }
		/// <summary>
		/// Configuration used to bind and execute current verb.
		/// </summary>
		public CommandLineConfiguration Configuration { get; }
		/// <summary>
		/// Arbitrary collection of properties passed with <see cref="ICommandLineBuilder.Properties"/>  while preparing verb execution.
		/// </summary>
		public IDictionary<object, object> Properties { get; }

		/// <summary>
		/// Constructor of <see cref="VerbExecutionContext"/>.
		/// </summary>
		public VerbExecutionContext(
			 IVerbSetBuilder verbSetBuilder,
			 Verb verb,
			 string[] arguments,
			 IServiceProvider serviceProvider,
			 CommandLineConfiguration configuration,
			 IDictionary<object, object> properties)
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
		public override string ToString() => $"Verb: {this.Verb.Name}, Arguments: {string.Join(", ", this.Arguments.ToArray())}";
	}
}
