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
using deniszykov.CommandLine.Formatting;
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
		[NotNull] private readonly HelpFormatter verbRenderer;


		public CommandLine(
			[NotNull] IVerbSetBuilder verbSetBuilder,
			[NotNull] string[] arguments,
			[NotNull] CommandLineConfiguration configuration,
			[NotNull] ITypeConversionProvider typeConversionProvider,
			[NotNull] IConsole console,
			[NotNull] IHelpTextProvider helpTextProvider,
			[NotNull] IServiceProvider serviceProvider,
			[NotNull] IDictionary<object, object> properties
			)
		{
			if (verbSetBuilder == null) throw new ArgumentNullException(nameof(verbSetBuilder));
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));
			if (configuration == null) throw new ArgumentNullException(nameof(configuration));
			if (typeConversionProvider == null) throw new ArgumentNullException(nameof(typeConversionProvider));
			if (console == null) throw new ArgumentNullException(nameof(console));
			if (helpTextProvider == null) throw new ArgumentNullException(nameof(helpTextProvider));
			if (properties == null) throw new ArgumentNullException(nameof(properties));

			this.verbSetBuilder = verbSetBuilder;
			this.arguments = arguments;
			this.configuration = configuration;
			this.console = console;
			this.serviceProvider = serviceProvider;
			this.properties = properties;
			this.verbBinder = new VerbBinder(configuration, typeConversionProvider, this.serviceProvider);
			this.verbRenderer = new HelpFormatter(configuration, console, helpTextProvider, typeConversionProvider);
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

				switch (bindResult)
				{
					case VerbBindingResult.Bound bound:
						// prepare scoped services
						var properties = new Dictionary<object, object>(this.properties);
						properties.AddVertToChain(bound.Verb); // add current verb to chain
						var context = new VerbExecutionContext(this.verbSetBuilder, bound.Verb, this.arguments,
							this.serviceProvider, this.configuration, properties);
						var cancellationToken = this.console.InterruptToken;
						//

						// resolve service parameters on verb
						this.verbBinder.ProvideContext(bound.Verb, bound.Arguments, context, cancellationToken);

						// execute verb
						return bound.Invoke();
					case VerbBindingResult.NoVerbSpecified noVerb:
						return this.WriteBindingError(noVerb, verbSet);
					case VerbBindingResult.FailedToBind failed:
						return this.WriteBindingError(failed, verbSet);
					case VerbBindingResult.HelpRequested help:
						return this.WriteHelp(help.VerbName);
					default:
						throw CommandLineException.NoVerbSpecified();
				}
			}
			catch (Exception exception)
			{
				var handler = this.configuration.UnhandledExceptionHandler ?? this.DefaultUnhandledExceptionHandler;
				handler(null, new ExceptionEventArgs(exception));

				return DotNetExceptionExitCode;
			}
		}

		private int WriteHelp(string verbName)
		{
			try
			{
				var verbSet = this.verbSetBuilder.Build();
				var verbChain = this.properties.GetVerbChain().ToList();
				var verb = string.IsNullOrEmpty(verbName) || ReferenceEquals(verbName, UnknownVerbName) ? default(Verb) : verbSet.FindVerb(verbName);
				if (verb != null)
				{
					this.verbRenderer.VerbDescription(verbSet, verb, verbChain);
				}
				else if (string.IsNullOrEmpty(verbName) || ReferenceEquals(verbName, UnknownVerbName))
				{
					this.verbRenderer.VerbList(verbSet, verbChain, includeTypeHelpText: verbChain.Count == 0);
				}
				else
				{
					this.verbRenderer.VerbNotFound(verbSet, verbName, verbChain);
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
		private int WriteBindingError(VerbBindingResult.FailedToBind bindResult, VerbSet verbSet)
		{
			var bestMatchMethod = (from bindingKeyValue in bindResult.BindingFailures
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
				error = CommandLineException.InvalidVerbParameters(bestMatchMethod, bindResult.BindingFailures[bestMatchMethod]);
			}

			if (this.configuration.DescribeOnBindFailure)
			{
				var verbChain = this.properties.GetVerbChain().ToList();

				if (bestMatchMethod != null)
				{
					this.verbRenderer.InvalidVerbParameters(bestMatchMethod, bindResult.BindingFailures[bestMatchMethod], error);
				}
				else
				{
					this.verbRenderer.VerbNotFound(verbSet, bindResult.VerbName, verbChain);
				}
			}
			else
			{
				throw error;
			}
			return this.configuration.BindFailureExitCode;
		}
		private int WriteBindingError(VerbBindingResult.NoVerbSpecified _, VerbSet verbSet)
		{
			if (!this.configuration.DescribeOnBindFailure)
			{
				throw CommandLineException.NoVerbSpecified();
			}

			var verbChain = this.properties.GetVerbChain().ToList();
			this.verbRenderer.VerbNotSpecified(verbSet, verbChain);

			return this.configuration.BindFailureExitCode;
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
		internal static ICommandLineBuilder CreateSubVerb([NotNull] VerbExecutionContext context)
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

			// copy verb set
			commandLineBuilder.Use(() => context.VerbSetBuilder);

			return commandLineBuilder;
		}

		private void DefaultUnhandledExceptionHandler(object source, ExceptionEventArgs eventArgs)
		{
			this.console.WriteErrorLine(eventArgs.Exception);
		}
	}
}
