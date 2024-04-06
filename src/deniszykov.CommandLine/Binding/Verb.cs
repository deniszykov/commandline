/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using deniszykov.CommandLine.Annotations;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Binding
{
	/// <summary>
	/// Verb's descriptor. Used for binding and execution verbs in <see cref="CommandLine"/>.
	/// </summary>
	[PublicAPI]
	public sealed class Verb
	{
		/// <summary>
		/// Name of verb. 
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// Description or help text for this verb.
		/// </summary>
		public readonly string Description;
		/// <summary>
		/// Type on which this verb should be executed. Used to instantiate or resolve target instance during execution and passed as first parameter to <see cref="Invoker"/> method.
		/// </summary>
		public readonly TypeInfo? TargetType;
		/// <summary>
		/// List of bound parameters(options or values) of verb.
		/// </summary>
		public readonly IReadOnlyCollection<VerbParameter> BoundParameters;
		/// <summary>
		/// List of service parameters of verb.
		/// </summary>
		public readonly IReadOnlyCollection<VerbParameter> ServiceParameters;
		/// <summary>
		/// Verb invocation function. First parameter is target instance, second is all <see cref="BoundParameters"/> and <see cref="ServiceParameters"/> in right order. Exit code is expected as result.
		/// </summary>
		public readonly Func<object?, object?[], Task<int>> Invoker;
		/// <summary>
		/// Flag indication that this verb is hidden from help display/listing.
		/// </summary>
		public readonly bool IsHidden;
		/// <summary>
		/// Flag indication that this verb has some sub-verbs.
		/// </summary>
		public readonly bool HasSubVerbs;

		/// <summary>
		/// Verb's constructor.
		/// </summary>
		public Verb(
			 string name,
			 string description,
			 TypeInfo? targetType,
			 IReadOnlyCollection<VerbParameter> boundParameters,
			 IReadOnlyCollection<VerbParameter> serviceParameters,
			 Func<object?, object?[], Task<int>> invoker,
			bool isHidden,
			bool hasSubVerbs)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (description == null) throw new ArgumentNullException(nameof(description));
			if (boundParameters == null) throw new ArgumentNullException(nameof(boundParameters));
			if (serviceParameters == null) throw new ArgumentNullException(nameof(serviceParameters));
			if (invoker == null) throw new ArgumentNullException(nameof(invoker));

			this.Name = name;
			this.Description = description;
			this.TargetType = targetType;
			this.BoundParameters = boundParameters;
			this.ServiceParameters = serviceParameters;
			this.Invoker = invoker;
			this.IsHidden = isHidden;
			this.HasSubVerbs = hasSubVerbs;
		}
		/// <summary>
		/// Verb's constructor from <see cref="MethodInfo"/>.
		/// </summary>
		public Verb(MethodInfo method)
		{
			if (method == null) throw new ArgumentNullException(nameof(method));

			this.Name = method.GetName() ?? method.Name;
			this.Description = method.GetDescription() ?? string.Empty;
			this.IsHidden = method.IsHidden();

			var boundParameters = new List<VerbParameter>();
			var serviceParameters = new List<VerbParameter>();

			foreach (var parameterInfo in method.GetParameters())
			{
				if (parameterInfo.IsServiceParameter())
				{
					var parameter = new VerbParameter(parameterInfo, serviceParameters.Count);
					serviceParameters.Add(parameter);
				}
				else
				{
					var parameter = new VerbParameter(parameterInfo, boundParameters.Count);
					boundParameters.Add(parameter);
				}
			}

			this.HasSubVerbs = serviceParameters.Any(parameterInfo => parameterInfo.ValueType.AsType() == typeof(ICommandLineBuilder));
			this.TargetType = method.IsStatic ? null : method.DeclaringType?.GetTypeInfo();
			this.BoundParameters = boundParameters;
			this.ServiceParameters = serviceParameters;
			this.Invoker = (target, args) =>
			{
				try
				{
					var result = method.Invoke(target, args);
					if (result is int exitCode)
					{
						return Task.FromResult(exitCode);
					}
					else if (result is Task<int> asyncExitCode)
					{
						return asyncExitCode;
					}
					else
					{
						throw new InvalidOperationException("Invalid method's result type. System.Int32 or Task<System.Int32> is expected.");
					}
				}
				catch (TargetInvocationException te)
				{
					ExceptionDispatchInfo.Capture(te.InnerException ?? te).Throw();
					throw;
				}
			};
		}


		internal IEnumerable<VerbParameter> GetNonHiddenBoundParameter()
		{
			return this.BoundParameters.Where(param => !param.IsHidden);
		}

		internal VerbParameter? FindBoundParameter(string name, StringComparison stringComparison)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));

			foreach (var parameter in this.BoundParameters)
			{
				if (string.Equals(parameter.Name, name, stringComparison) ||
					string.Equals(parameter.Alias, name, stringComparison))
				{
					return parameter;
				}
			}
			return null;
		}

		/// <inheritdoc />
		public override string ToString() => this.Name;
	}
}