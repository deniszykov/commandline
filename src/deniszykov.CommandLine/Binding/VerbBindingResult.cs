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
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Binding
{
	internal class VerbBindingResult
	{
		public static readonly Dictionary<Verb, ParameterBindingResult[]> EmptyFailedMethodBindings = new Dictionary<Verb, ParameterBindingResult[]>();

		public bool IsSuccess => this.Verb != null;

		public Verb Verb { get; }
		public string VerbName { get; }
		public object Target { get; }
		public object[] Arguments { get; }
		public Dictionary<Verb, ParameterBindingResult[]> BindingFailures { get; }

		public VerbBindingResult([NotNull] string verbName, [NotNull]Dictionary<Verb, ParameterBindingResult[]> bindingFailures)
		{
			if (verbName == null) throw new ArgumentNullException(nameof(verbName));
			if (bindingFailures == null) throw new ArgumentNullException(nameof(bindingFailures));

			this.VerbName = verbName;
			this.BindingFailures = bindingFailures;
		}
		public VerbBindingResult([NotNull] Verb verb, [CanBeNull]object target, [NotNull]object[] arguments)
		{
			this.BindingFailures = EmptyFailedMethodBindings;
			this.Verb = verb;
			this.Arguments = arguments;
			this.Target = target;
		}

		public int Invoke()
		{
			return this.Verb.Invoker(this.Target, this.Arguments);
		}

		public override string ToString()
		{
			if (this.IsSuccess)
				return $"Successful binding to verb '{this.Verb}' with '{string.Join(", ", this.Arguments)}' arguments.";
			else
				return $"Failure binding to verb '{this.VerbName}'." + string.Join(Environment.NewLine, this.BindingFailures.Select(kv => $"{kv.Key.Name}: {string.Join(", ", kv.Value.Where(p => !p.IsSuccess).Select(p => p.ToString()))}"));
		}
	}
}
