/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;
using System.Collections.Generic;
using System.Linq;
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
		/// Exit code used when unhandled .NET Exception occurred during verb execution.
		/// </summary>
		public const int DotNetExceptionExitCode = -2147023895;
		/// <summary>
		/// Method name used as placeholder for missing verb name in exception text and <see cref="VerbBindingResult.VerbName"/> property.
		/// </summary>
		public const string UnknownVerbName = "<no name specified>";

		[NotNull] private readonly IVerbSetBuilder verbSetBuilder;
		[NotNull] private readonly string[] arguments;
		[NotNull] private readonly CommandLineConfiguration configuration;
		[NotNull] private readonly IDictionary<object, object> properties;
		[NotNull] private readonly IConsole console;
		[NotNull] private readonly IServiceProvider serviceProvider;
		[NotNull] private readonly VerbBinder verbBinder;
		[NotNull] private readonly VerbRenderer verbRenderer;


		public CommandLine(
			[NotNull] IVerbSetBuilder verbSetBuilder,
			[NotNull] string[] arguments,
			[NotNull] CommandLineConfiguration configuration,
			[NotNull] ITypeConversionProvider typeConversionProvider,
			[NotNull] IConsole console,
			[NotNull] IServiceProvider serviceProvider,
			[NotNull] IDictionary<object, object> properties
			)
		{
			if (verbSetBuilder == null) throw new ArgumentNullException(nameof(verbSetBuilder));
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));
			if (configuration == null) throw new ArgumentNullException(nameof(configuration));
			if (typeConversionProvider == null) throw new ArgumentNullException(nameof(typeConversionProvider));
			if (console == null) throw new ArgumentNullException(nameof(console));
			if (properties == null) throw new ArgumentNullException(nameof(properties));

			this.verbSetBuilder = verbSetBuilder;
			this.arguments = arguments;
			this.configuration = configuration;
			this.console = console;
			this.serviceProvider = serviceProvider;
			this.properties = properties;
			this.verbBinder = new VerbBinder(configuration, typeConversionProvider, this.serviceProvider);
			this.verbRenderer = new VerbRenderer(configuration, console, typeConversionProvider);
		}

		/// <summary>
		/// Run verb on configured type and return exit code of executed verb.
		/// </summary>
		/// <returns>Exit code of verb-or-<see cref="DotNetExceptionExitCode"/> if exception happened-or-<see cref="CommandLineConfiguration.BindFailureExitCode"/> if verb not found and description is shown.</returns>
		public int Run()
		{
			try
			{
				var verbSet = this.verbSetBuilder.Build();
				var bindResult = this.verbBinder.Bind(verbSet, this.configuration.DefaultVerbName, this.arguments);
				if (bindResult.IsSuccess)
				{
					// prepare scoped services
					var context = new VerbExecutionContext(this.verbSetBuilder, bindResult.Verb, this.arguments,
						this.serviceProvider, this.configuration, this.properties);
					var cancellationToken = this.console.InterruptToken;

					// resolve service parameters on verb
					this.verbBinder.ProvideContext(bindResult, context, cancellationToken);

					// execute verb
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
		/// Write description of available verbds on type into <see cref="IConsole.WriteLine"/>-or-Write detailed description of <paramref name="verbToDescribe"/> into <see cref="IConsole.WriteLine"/>.
		/// </summary>
		/// <param name="verbToDescribe">Optional verb name for detailed description.</param>
		/// <returns><see cref="CommandLineConfiguration.DescribeExitCode"/></returns>
		public int Describe(string verbToDescribe = null)
		{
			try
			{
				var verbSet = this.verbSetBuilder.Build();
				var verb = verbSet.Verbs.FirstOrDefault(otherVerb => string.Equals(otherVerb.Name, verbToDescribe, StringComparison.OrdinalIgnoreCase));
				var verbChain = this.properties.GetVerbChain().ToList();
				if (verb != null)
				{
					this.verbRenderer.Render(verbSet, verb, verbChain);
				}
				else if (string.IsNullOrEmpty(verbToDescribe) == false)
				{
					this.verbRenderer.RenderNotFound(verbSet, verbToDescribe, verbChain);
				}
				else
				{
					this.verbRenderer.RenderList(verbSet, verbChain, includeTypeHelpText: verbChain.Count == 0);
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
		public static ICommandLineBuilder CreateFromArguments([NotNull, ItemNotNull] params string[] arguments)
		{
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));

			return new CommandLineBuilder(arguments).Configure(config =>
			{
				config.SetToDefault();
			});
		}

		[NotNull]
		public static ICommandLineBuilder CreateFromContext([NotNull] VerbExecutionContext context)
		{
			if (context == null) throw new ArgumentNullException(nameof(context));

			var newArguments = context.Arguments;
			if (context.Arguments.Length > 0 && string.Equals(context.Arguments[0], context.Verb.Name, StringComparison.OrdinalIgnoreCase))
			{
				newArguments = newArguments.Skip(1).ToArray();
			}

			// configure new builder
			var commandLineBuilder = new CommandLineBuilder(newArguments)
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

			// add current verb to chain
			commandLineBuilder.Properties.AddVertToChain(context.Verb);

			// copy verb set
			commandLineBuilder.Use(() => context.VerbSetBuilder);

			return commandLineBuilder;
		}

		private void PrintOrThrowNotFoundException(VerbBindingResult bindResult)
		{
			var bestMatchMethod = (from bindingKeyValue in bindResult.FailedMethodBindings
								   let parameters = bindingKeyValue.Value
								   let method = bindingKeyValue.Key
								   orderby parameters.Count(parameter => parameter.IsSuccess) descending
								   select method).FirstOrDefault();

			var error = default(CommandLineException);
			if (bestMatchMethod == null)
			{
				error = CommandLineException.VerbNotFound(bindResult.VerbName);
			}
			else
			{
				error = CommandLineException.InvalidVerbParameters(bestMatchMethod, bindResult.FailedMethodBindings[bestMatchMethod]);
			}

			if (this.configuration.DescribeOnBindFailure)
			{
				if (bindResult.VerbName == UnknownVerbName)
				{
					this.console.WriteLine(" Error:");
					this.console.WriteLine("  No verb is specified.");
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
					this.console.WriteLine($"  Invalid parameters specified for '{bindResult.VerbName}' verb.");
					this.console.WriteLine();

					if (this.configuration.DetailedBindFailureMessage)
					{
						this.console.WriteErrorLine(error);
						this.console.WriteErrorLine();
					}

					this.Describe(bindResult.VerbName);
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
