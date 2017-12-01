/*
	Copyright (c) 2017 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System.Collections.Generic;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace System
{
	internal class MethodBindResult
	{
		private static readonly Dictionary<MethodInfo, ParameterBindResult[]> EmptyFailedMethodBindings = new Dictionary<MethodInfo, ParameterBindResult[]>();

		public bool IsSuccess { get { return this.Method != null; } }
		public MethodInfo Method { get; private set; }
		public object[] Arguments { get; private set; }
		public Dictionary<MethodInfo, ParameterBindResult[]> FailedMethodBindings { get; private set; }
		public string MethodName { get; private set; }

		public MethodBindResult(string methodName, Dictionary<MethodInfo, ParameterBindResult[]> failedMethodBindings)
		{
			if (methodName == null) throw new ArgumentNullException("methodName");
			if (failedMethodBindings == null) throw new ArgumentNullException("failedMethodBindings");

			this.MethodName = methodName;
			this.FailedMethodBindings = failedMethodBindings;
		}
		public MethodBindResult(MethodInfo method, object[] arguments)
		{
			this.MethodName = method.Name;
			this.FailedMethodBindings = EmptyFailedMethodBindings;
			this.Method = method;
			this.Arguments = arguments;
		}

		public override string ToString()
		{
			if (this.IsSuccess)
				return string.Format("Successful binding to method {0} and {1} arguments", this.Method, this.Arguments.Length);
			else
				return string.Format("Failure binding to method {0} ", this.MethodName);
		}
	}
}
