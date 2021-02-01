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
	internal class CommandBindingResult
	{
		public static readonly Dictionary<Command, ParameterBindingResult[]> EmptyFailedMethodBindings = new Dictionary<Command, ParameterBindingResult[]>();

		public bool IsSuccess => this.Command != null;

		public Command Command { get; private set; }
		public string CommandName { get; private set; }
		public object Target { get; private set; }
		public object[] Arguments { get; private set; }
		public Dictionary<Command, ParameterBindingResult[]> FailedMethodBindings { get; private set; }

		public CommandBindingResult([NotNull] string commandName, [NotNull]Dictionary<Command, ParameterBindingResult[]> failedMethodBindings)
		{
			if (commandName == null) throw new ArgumentNullException(nameof(commandName));
			if (failedMethodBindings == null) throw new ArgumentNullException(nameof(failedMethodBindings));

			this.CommandName = commandName;
			this.FailedMethodBindings = failedMethodBindings;
		}
		public CommandBindingResult([NotNull] Command command, [CanBeNull]object target, [NotNull]object[] arguments)
		{
			this.FailedMethodBindings = EmptyFailedMethodBindings;
			this.Command = command;
			this.Arguments = arguments;
			this.Target = target;
		}

		public int Invoke()
		{
			return this.Command.Invoker(this.Target, this.Arguments);
		}

		public override string ToString()
		{
			if (this.IsSuccess)
				return $"Successful binding to method {this.Command} and {this.Arguments.Length} arguments";
			else
				return $"Failure binding to method {this.CommandName} ";
		}
	}
}
