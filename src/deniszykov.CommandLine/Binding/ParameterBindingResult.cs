/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;
using System.Reflection;

namespace deniszykov.CommandLine.Binding
{
	internal class ParameterBindingResult
	{
		public bool IsSuccess => this.Error == null;

		public VerbParameter Parameter { get; private set; }
		public object Value { get; private set; }
		public Exception Error { get; private set; }

		public ParameterBindingResult(VerbParameter parameter, Exception error, object value)
		{
			if (parameter == null) throw new ArgumentNullException(nameof(parameter));

			if (error is TargetInvocationException)
				error = ((TargetInvocationException)error).InnerException;

			this.Parameter = parameter;
			this.Error = error;
			this.Value = value;
		}

		public override string ToString()
		{
			if (this.IsSuccess)
				return $"Successful binding to parameter {this.Parameter.Name}.";
			else
				return $"Failure binding to parameter {this.Parameter.Name}: {this.Error.Message}";
		}
	}
}
