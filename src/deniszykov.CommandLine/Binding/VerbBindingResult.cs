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
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Binding
{
	internal abstract class VerbBindingResult
	{
		public static readonly Dictionary<Verb, ParameterBindingResult[]> EmptyFailedMethodBindings = new Dictionary<Verb, ParameterBindingResult[]>();

		public sealed class Bound : VerbBindingResult
		{
			[CanBeNull] public object Target { get; }
			[NotNull] public Verb Verb { get; }
			[NotNull, ItemCanBeNull] public object[] Arguments { get; }
			public bool HasHelpOption { get; }
			/// <inheritdoc />
			public override string VerbName => this.Verb.Name;

			public Bound([NotNull] Verb verb, [CanBeNull] object target, [NotNull, ItemCanBeNull]object[] arguments, bool hasHelpOption)
			{
				if (verb == null) throw new ArgumentNullException(nameof(verb));
				if (arguments == null) throw new ArgumentNullException(nameof(arguments));

				this.Target = target;
				this.Arguments = arguments;
				this.HasHelpOption = hasHelpOption;
				this.Verb = verb;
			}

			public Task<int> InvokeAsync()
			{
				return this.Verb.Invoker(this.Target, this.Arguments);
			}

			/// <inheritdoc />
			public override string ToString() => $"Successful binding to verb '{this.Verb}' with '{string.Join(", ", this.Arguments)}' arguments.";
		}
		public sealed class FailedToBind : VerbBindingResult
		{
			[NotNull] public Dictionary<Verb, ParameterBindingResult[]> BindingFailures { get; }
			public override string VerbName { get; }

			public FailedToBind([NotNull] string verbName, [NotNull] Dictionary<Verb, ParameterBindingResult[]> bindingFailures)
			{
				if (verbName == null) throw new ArgumentNullException(nameof(verbName));
				if (bindingFailures == null) throw new ArgumentNullException(nameof(bindingFailures));

				this.BindingFailures = bindingFailures;
				this.VerbName = verbName;
			}

			public override string ToString() => $"Failure binding to verb '{this.VerbName}'." +
				string.Join(Environment.NewLine, this.BindingFailures.Select(kv => $"{kv.Key.Name}: {string.Join(", ", kv.Value.Where(p => !p.IsSuccess).Select(p => p.ToString()))}")) + ".";
		}
		public sealed class HelpRequested : VerbBindingResult
		{
			public override string VerbName { get; }

			public HelpRequested([NotNull] string verbName)
			{
				if (verbName == null) throw new ArgumentNullException(nameof(verbName));

				this.VerbName = verbName;
			}

			/// <inheritdoc />
			public override string ToString() => $"Help requested for '{this.VerbName}'.";
		}
		public sealed class NoVerbSpecified : VerbBindingResult
		{
			public override string VerbName { get; }

			public NoVerbSpecified()
			{
				this.VerbName = CommandLine.UNKNOWN_VERB_NAME;
			}

			public override string ToString() => "No verb specified.";
		}

		[NotNull] public abstract string VerbName { get; }
	}
}
