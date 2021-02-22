using System;
using System.Collections.Generic;
using System.Linq;
using deniszykov.CommandLine.Binding;
using JetBrains.Annotations;

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
		[NotNull] public IVerbSetBuilder VerbSetBuilder { get; }
		/// <summary>
		/// Current executing verb.
		/// </summary>
		[NotNull] public Verb Verb { get; }
		/// <summary>
		/// Arguments passed to <see cref="Verb"/>.
		/// </summary>
		[NotNull] public string[] Arguments { get; }
		/// <summary>
		/// Service provider instance used to resolve services.
		/// </summary>
		[NotNull] public IServiceProvider ServiceProvider { get; }
		/// <summary>
		/// Configuration used to bind and execute current verb.
		/// </summary>
		[NotNull] public CommandLineConfiguration Configuration { get; }
		/// <summary>
		/// Arbitrary collection of properties passed with <see cref="ICommandLineBuilder.Properties"/>  while preparing verb execution.
		/// </summary>
		[NotNull] public IDictionary<object, object> Properties { get; }

		/// <summary>
		/// Constructor of <see cref="VerbExecutionContext"/>.
		/// </summary>
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
