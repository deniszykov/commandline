/*
	Copyright (c) 2017 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace System
{
	internal class ParameterBindResult
	{
		public bool IsSuccess { get { return this.Error == null; } }
		public ParameterInfo Parameter { get; private set; }
		public object Value { get; private set; }
		public Exception Error { get; private set; }

		public ParameterBindResult(ParameterInfo parameter, Exception error, object value)
		{
			if (parameter == null) throw new ArgumentNullException("parameter");

			if (error is TargetInvocationException)
				error = ((TargetInvocationException)error).InnerException;

			this.Parameter = parameter;
			this.Error = error;
			this.Value = value;
		}

		public override string ToString()
		{
			if (this.IsSuccess)
				return string.Format("Successful binding to parameter {0}.", this.Parameter.Name);
			else
				return string.Format("Failure binding to parameter {0}: {1}", this.Parameter.Name, this.Error.Message);
		}
	}
}
