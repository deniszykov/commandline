/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace deniszykov.CommandLine
{
	[PublicAPI]
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
		ICommandLineBuilder Use<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] VerbSetT>();
		/// <summary>
		/// Use methods from specified <paramref name="verSetType"/> type as verbs. Each call override previous <see cref="Use(Type)"/>.
		/// </summary>
		/// <param name="verSetType">Type of verb class. Used to look-up verbs by name.</param>
		/// <returns>The same instance of the <see cref="ICommandLineBuilder"/> for chaining.</returns>
		ICommandLineBuilder Use([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type verSetType);
		/// <summary>
		/// Use specified verb list builder to declare verbs. Each call override previous <see cref="Use(Type)"/>.
		/// </summary>
		/// <returns>The same instance of the <see cref="ICommandLineBuilder"/> for chaining.</returns>
		ICommandLineBuilder Use(Func<IVerbSetBuilder> buildDelegate);

		/// <summary>
		/// Build <see cref="CommandLine"/> with specified parameters. Each call will create new instance of <see cref="CommandLine"/>.
		/// </summary>
		CommandLine Build();

		/// <summary>
		/// Run <see cref="CommandLine"/> with specified parameters and return exit code.
		/// </summary>
		/// <returns>Verb's exit code.</returns>
		int Run();

		/// <summary>
		/// Run <see cref="CommandLine"/> asynchronously with specified parameters and return exit code.
		/// </summary>
		/// <returns>Verb's exit code.</returns>
		Task<int> RunAsync(CancellationToken cancellationToken);
	}
}