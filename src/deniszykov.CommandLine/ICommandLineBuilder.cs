using System;
using System.Collections.Generic;

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
		/// Provider <see cref="IServiceProvider"/> to retrieve services during binding and invocation of command. Could be called once.
		/// </summary>
		/// <param name="factory">Factory creating <see cref="IServiceProvider"/> instance to use.</param>
		/// <returns>The same instance of the <see cref="ICommandLineBuilder"/> for chaining.</returns>
		ICommandLineBuilder UseServiceProvider(Func<IServiceProvider> factory);
		/// <summary>
		/// Use methods from specified <typeparamref name="CommandListT"/> type as commands. Each call override previous <see cref="Use(Type)"/>.
		/// </summary>
		/// <typeparam name="CommandListT">Type of command class. Used to look-up commands by name.</typeparam>
		/// <returns>The same instance of the <see cref="ICommandLineBuilder"/> for chaining.</returns>
		ICommandLineBuilder Use<CommandListT>();
		/// <summary>
		/// Use methods from specified <paramref name="commandListType"/> type as commands. Each call override previous <see cref="Use(Type)"/>.
		/// </summary>
		/// <param name="commandListType">Type of command class. Used to look-up commands by name.</param>
		/// <returns>The same instance of the <see cref="ICommandLineBuilder"/> for chaining.</returns>
		ICommandLineBuilder Use(Type commandListType);
		/// <summary>
		/// Use specified command list builder to declare commands. Each call override previous <see cref="Use(Type)"/>.
		/// </summary>
		/// <returns>The same instance of the <see cref="ICommandLineBuilder"/> for chaining.</returns>
		ICommandLineBuilder Use(Func<ICommandsBuilder> buildDelegate);

		/// <summary>
		/// Run <see cref="CommandLine"/> with specified parameters and return exit code.
		/// </summary>
		/// <returns>Command's exit code.</returns>
		int Run();

		/// <summary>
		/// Write description of available commands on type into <see cref="IConsole.WriteLine"/>-or-Write detailed description of <paramref name="commandToDescribe"/> into <see cref="IConsole.WriteLine"/>.
		/// </summary>
		/// <returns>Command's exit code.</returns>
		int Describe(string commandToDescribe);
	}
}