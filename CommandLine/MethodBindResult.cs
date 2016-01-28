/*
	Copyright (c) 2016 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

// ReSharper disable once CheckNamespace
using System.Reflection;

namespace System
{
	internal class MethodBindResult
	{

		public bool IsSuccess { get { return this.Method != null; } }
		public MethodInfo Method { get; private set; }
		public object[] Arguments { get; private set; }
		public MethodInfo Candidate { get; private set; }
		public string MethodName { get; private set; }

		public MethodBindResult(string methodName, MethodInfo candidate)
		{
			this.MethodName = methodName;
			this.Candidate = candidate;
		}
		public MethodBindResult(MethodInfo method, object[] arguments)
		{
			this.MethodName = method.Name;
			this.Candidate = method;
			this.Method = method;
			this.Arguments = arguments;
		}

		public override string ToString()
		{
			if (this.IsSuccess)
				return string.Format("Successfull binding to method {0} and {1} arguments", this.Method, this.Arguments.Length);
			else if (this.Candidate != null)
				return string.Format("Failure binding to method {0} because of parameters.", this.Method);
			else
				return string.Format("Failure binding to method {0} ", this.MethodName);
		}
	}
}
