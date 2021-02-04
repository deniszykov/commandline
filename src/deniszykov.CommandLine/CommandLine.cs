﻿/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using deniszykov.CommandLine.Annotations;
using deniszykov.CommandLine.Binding;
using deniszykov.CommandLine.Builders;
using deniszykov.CommandLine.Renderers;
using deniszykov.TypeConversion;
using JetBrains.Annotations;

namespace deniszykov.CommandLine
{
	/// <summary>
	/// Utility class for building command-line styled applications.
	/// Provides action routing and parameters parsing features for called code.
	/// </summary>
	public sealed class CommandLine
	{
		/// <summary>
		/// Exit code used when unhandled .NET Exception occurred during command execution.
		/// </summary>
		public const int DotNetExceptionExitCode = -2147023895;
		/// <summary>
		/// Prefix of command's argument.
		/// </summary>
		public const string ArgumentNamePrefix = "--";
		/// <summary>
		/// Prefix of short name of command's argument.
		/// </summary>
		public const string ArgumentNamePrefixShort = "-";
		/// <summary>
		/// Method name used as placeholder for missing command name in exception text and <see cref="CommandBindingResult.CommandName"/> property.
		/// </summary>
		public const string UnknownMethodName = "<no name specified>";
		/// <summary>
		/// Name of property with list of preceding commands of current <see cref="ICommandsBuilder"/>. Type is array of <see cref="Command"/>.
		/// </summary>
		public const string CommandChainPropertyName = "__command_chain__";

		/// <summary>
		/// Assumption about console's window width when formatting <see cref="Describe"/> output. When output is redirected then <see cref="Int32.MaxValue"/> is used.
		/// </summary>
		public static int DescribeConsoleWindowWidth;

		[NotNull] private readonly ICommandsBuilder commandsBuilder;
		[NotNull] private readonly CommandLineArguments commandLineArguments;
		[NotNull] private readonly CommandLineConfiguration configuration;
		[NotNull] private readonly IDictionary<object, object> properties;
		[NotNull] private readonly ITypeConversionProvider typeConversionProvider;
		[NotNull] private readonly IConsole console;
		[NotNull] private readonly IServiceProvider serviceProvider;
		[NotNull] private readonly CommandBinder commandBinder;
		[NotNull] private readonly CommandListRenderer commandListRenderer;
		[NotNull] private readonly CommandRenderer commandRenderer;


		public CommandLine(
			[NotNull] ICommandsBuilder commandsBuilder,
			[NotNull] CommandLineArguments commandLineArguments,
			[NotNull] CommandLineConfiguration configuration,
			[NotNull] ITypeConversionProvider typeConversionProvider,
			[NotNull] IConsole console,
			[NotNull] IServiceProvider serviceProvider,
			[NotNull] IDictionary<object, object> properties
			)
		{
			if (commandsBuilder == null) throw new ArgumentNullException(nameof(commandsBuilder));
			if (commandLineArguments == null) throw new ArgumentNullException(nameof(commandLineArguments));
			if (configuration == null) throw new ArgumentNullException(nameof(configuration));
			if (typeConversionProvider == null) throw new ArgumentNullException(nameof(typeConversionProvider));
			if (console == null) throw new ArgumentNullException(nameof(console));
			if (properties == null) throw new ArgumentNullException(nameof(properties));

			this.commandsBuilder = commandsBuilder;
			this.commandLineArguments = commandLineArguments;
			this.configuration = configuration;
			this.typeConversionProvider = typeConversionProvider;
			this.console = console;
			this.serviceProvider = serviceProvider;
			this.properties = properties;
			this.commandBinder = new CommandBinder(typeConversionProvider, this.serviceProvider);
			this.commandListRenderer = new CommandListRenderer(console, typeConversionProvider);
			this.commandRenderer = new CommandRenderer(console, typeConversionProvider);
		}

		/// <summary>
		/// Run command on configured type and return exit code of executed command.
		/// </summary>
		/// <returns>Exit code of command-or-<see cref="DotNetExceptionExitCode"/> if exception happened-or-<see cref="CommandLineConfiguration.BindFailureExitCode"/> if command not found and description is shown.</returns>
		public int Run()
		{
			try
			{
				var commandSet = this.commandsBuilder.Build();
				var bindResult = this.commandBinder.Bind(commandSet, this.configuration.DefaultCommandName, this.commandLineArguments);
				if (bindResult.IsSuccess)
				{
					// prepare scoped services
					var context = new CommandExecutionContext(this.commandsBuilder, bindResult.Command, this.commandLineArguments,
						this.serviceProvider, this.configuration, this.properties);
					var cancellationToken = this.console.InterruptToken;

					// resolve service parameters on command
					this.commandBinder.ProvideContext(bindResult, context, cancellationToken);

					// execute command
					return bindResult.Invoke();
				}

				this.PrintOrThrowNotFoundException(bindResult);
				return this.configuration.BindFailureExitCode;
			}
			catch (Exception exception)
			{
				var handler = this.configuration.UnhandledExceptionHandler ?? this.DefaultUnhandledExceptionHandler;
				handler(null, new ExceptionEventArgs(exception));

				return DotNetExceptionExitCode;
			}
		}
		/// <summary>
		/// Write description of available commands on type into <see cref="IConsole.WriteLine"/>-or-Write detailed description of <paramref name="commandToDescribe"/> into <see cref="IConsole.WriteLine"/>.
		/// </summary>
		/// <param name="commandToDescribe">Optional command name for detailed description.</param>
		/// <returns><see cref="CommandLineConfiguration.DescribeExitCode"/></returns>
		public int Describe(string commandToDescribe = null)
		{
			try
			{
				var commandSet = this.commandsBuilder.Build();
				var command = commandSet.Commands.FirstOrDefault(otherCommand => string.Equals(otherCommand.Name, commandToDescribe, StringComparison.OrdinalIgnoreCase));
				if (command != null)
				{
					this.commandRenderer.Render(command);
				}
				else if (string.IsNullOrEmpty(commandToDescribe) == false)
				{
					this.commandListRenderer.RenderNotFound(commandSet, commandToDescribe);
				}
				else
				{
					this.commandListRenderer.Render(commandSet);
				}
				return this.configuration.DescribeExitCode;
			}
			catch (Exception exception)
			{
				var handler = this.configuration.UnhandledExceptionHandler ?? this.DefaultUnhandledExceptionHandler;
				handler(null, new ExceptionEventArgs(exception));

				return DotNetExceptionExitCode;
			}
		}

		[NotNull]
		public static ICommandLineBuilder CreateFromArguments([NotNull, ItemNotNull] params string[] commandLineArgs)
		{
			if (commandLineArgs == null) throw new ArgumentNullException(nameof(commandLineArgs));

			return new CommandLineBuilder(commandLineArgs).Configure(config =>
			{
				config.BindFailureExitCode = 1;
				config.DescribeExitCode = 2;
				config.NewLine = Environment.NewLine;
			});
		}

		[NotNull]
		public static ICommandLineBuilder CreateFromContext([NotNull] CommandExecutionContext context)
		{
			if (context == null) throw new ArgumentNullException(nameof(context));

			var typeConversionProvider = (ITypeConversionProvider)context.ServiceProvider.GetService(typeof(ITypeConversionProvider)) ?? new TypeConversionProvider();
			var newArguments = new CommandLineArguments(typeConversionProvider, context.Arguments);

			if (newArguments.ContainsKey("0"))
			{
				newArguments.RemoveAt(0);
			}

			// configure new builder
			var commandLineBuilder = new CommandLineBuilder(newArguments.ToArray())
				.UseServiceProvider(() => context.ServiceProvider)
				.Configure(config =>
				{
					context.Configuration.CopyTo(config);
				});

			// copy context's properties
			foreach (var contextProperty in context.Properties)
			{
				commandLineBuilder.Properties[contextProperty.Key] = contextProperty.Value;
			}

			// add current command to chain
			var commandChain = context.GetCommandChain().ToList();
			if (commandChain.Contains(context.Command))
			{
				throw CommandLineException.RecursiveCommandChain(commandChain.Select(command => command.Name), context.Command.Name);
			}
			commandChain.Add(context.Command);
			commandLineBuilder.Properties[CommandChainPropertyName] = commandChain.ToArray();

			// copy command set
			commandLineBuilder.Use(() => context.CommandsBuilder);

			return commandLineBuilder;
		}

		private void PrintOrThrowNotFoundException(CommandBindingResult bindResult)
		{
			var bestMatchMethod = (from bindingKeyValue in bindResult.FailedMethodBindings
								   let parameters = bindingKeyValue.Value
								   let method = bindingKeyValue.Key
								   orderby parameters.Count(parameter => parameter.IsSuccess) descending
								   select method).FirstOrDefault();

			var error = default(CommandLineException);
			if (bestMatchMethod == null)
			{
				error = CommandLineException.CommandNotFound(bindResult.CommandName);
			}
			else
			{
				error = CommandLineException.InvalidCommandParameters(bestMatchMethod, bindResult.FailedMethodBindings[bestMatchMethod]);
			}

			if (this.configuration.DescribeOnBindFailure)
			{
				if (bindResult.CommandName == UnknownMethodName)
				{
					this.console.WriteLine(" Error:");
					this.console.WriteLine("  No command is specified.");
					this.console.WriteLine();

					if (this.configuration.DetailedBindFailureMessage)
					{
						this.console.WriteErrorLine(error);
						this.console.WriteErrorLine();
					}

					this.Describe();
				}
				else if (bestMatchMethod != null)
				{
					this.console.WriteLine(" Error:");
					this.console.WriteLine($"  Invalid parameters specified for '{bindResult.CommandName}' command.");
					this.console.WriteLine();

					if (this.configuration.DetailedBindFailureMessage)
					{
						this.console.WriteErrorLine(error);
						this.console.WriteErrorLine();
					}

					this.Describe(bindResult.CommandName);
				}
				else
				{
					this.console.WriteLine(" Error:");
					this.console.WriteLine(error.Message);
					this.console.WriteLine();
				}
			}
			else
			{
				throw error;
			}
		}

		private void DefaultUnhandledExceptionHandler(object source, ExceptionEventArgs eventArgs)
		{
			this.console.WriteErrorLine(eventArgs.Exception);
		}
	}
}