using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace deniszykov.CommandLine
{
	public interface ICommandLineBuilder
	{
		/// <summary>
		/// A central location for sharing state between components during the host building process.
		/// </summary>
		IDictionary<object, object> Properties { get; }

		/// <summary>
		/// Configure <see cref="CommandLine"/> with specific parameters. Could be called multiple times.
		/// </summary>
		/// <param name="configureDelegate">The delegate for configuring <see cref="CommandLineConfiguration"/>.</param>
		/// <returns>The same instance of the <see cref="ICommandLineBuilder"/> for chaining.</returns>
		ICommandLineBuilder Configure(Action<CommandLineConfiguration> configureDelegate);
		/// <summary>
		/// Provider <see cref="IServiceProvider"/> to retrieve services during binding and invocation of verb. Could be called once.
		/// </summary>
		/// <param name="factory">Factory creating <see cref="IServiceProvider"/> instance to use.</param>
		/// <returns>The same instance of the <see cref="ICommandLineBuilder"/> for chaining.</returns>
		ICommandLineBuilder UseServiceProvider(Func<IServiceProvider> factory);
		/// <summary>
		/// Use methods from specified <typeparamref name="VerbSetT"/> type as verbs. Each call override previous <see cref="Use(Type)"/>.
		/// </summary>
		/// <typeparam name="VerbSetT">Type of verb class. Used to look-up verbs by name.</typeparam>
		/// <returns>The same instance of the <see cref="ICommandLineBuilder"/> for chaining.</returns>
		ICommandLineBuilder Use<VerbSetT>();
		/// <summary>
		/// Use methods from specified <paramref name="verSetType"/> type as verbs. Each call override previous <see cref="Use(Type)"/>.
		/// </summary>
		/// <param name="verSetType">Type of verb class. Used to look-up verbs by name.</param>
		/// <returns>The same instance of the <see cref="ICommandLineBuilder"/> for chaining.</returns>
		ICommandLineBuilder Use(Type verSetType);
		/// <summary>
		/// Use specified verb list builder to declare verbs. Each call override previous <see cref="Use(Type)"/>.
		/// </summary>
		/// <returns>The same instance of the <see cref="ICommandLineBuilder"/> for chaining.</returns>
		ICommandLineBuilder Use(Func<IVerbSetBuilder> buildDelegate);

		/// <summary>
		/// Run <see cref="CommandLine"/> with specified parameters and return exit code.
		/// </summary>
		/// <returns>Verb's exit code.</returns>
		int Run();
	}
}