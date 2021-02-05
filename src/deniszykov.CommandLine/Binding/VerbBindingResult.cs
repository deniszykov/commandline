/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Binding
{
	internal class VerbBindingResult
	{
		public static readonly Dictionary<Verb, ParameterBindingResult[]> EmptyFailedMethodBindings = new Dictionary<Verb, ParameterBindingResult[]>();

		public bool IsSuccess => this.Verb != null;

		public Verb Verb { get; private set; }
		public string VerbName { get; private set; }
		public object Target { get; private set; }
		public object[] Arguments { get; private set; }
		public Dictionary<Verb, ParameterBindingResult[]> FailedMethodBindings { get; private set; }

		public VerbBindingResult([NotNull] string verbName, [NotNull]Dictionary<Verb, ParameterBindingResult[]> failedMethodBindings)
		{
			if (verbName == null) throw new ArgumentNullException(nameof(verbName));
			if (failedMethodBindings == null) throw new ArgumentNullException(nameof(failedMethodBindings));

			this.VerbName = verbName;
			this.FailedMethodBindings = failedMethodBindings;
		}
		public VerbBindingResult([NotNull] Verb verb, [CanBeNull]object target, [NotNull]object[] arguments)
		{
			this.FailedMethodBindings = EmptyFailedMethodBindings;
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
				return $"Successful binding to method {this.Verb} and {this.Arguments.Length} arguments";
			else
				return $"Failure binding to method {this.VerbName} ";
		}
	}
}
