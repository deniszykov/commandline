using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using deniszykov.CommandLine.Annotations;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Binding
{
	public sealed class Verb
	{
		[NotNull]
		public readonly string Name;
		[NotNull]
		public readonly string Description;
		[CanBeNull]
		public readonly TypeInfo TargetType;
		[NotNull, ItemNotNull]
		public readonly IReadOnlyCollection<VerbParameter> BoundParameters;
		[NotNull, ItemNotNull]
		public readonly IReadOnlyCollection<VerbParameter> ServiceParameters;
		[NotNull]
		public readonly Func<object, object[], int> Invoker;
		public readonly bool IsHidden;
		public readonly bool HasSubVerbs;

		public Verb([NotNull] MethodInfo method)
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
					return (int)method.Invoke(target, args);
				}
				catch (TargetInvocationException te)
				{
					ExceptionDispatchInfo.Capture(te.InnerException ?? te).Throw();
					throw;
				}
			};
		}

		[NotNull, ItemNotNull]
		public IEnumerable<VerbParameter> GetNonHiddenBoundParameter()
		{
			return this.BoundParameters.Where(param => !param.IsHidden);
		}

		[CanBeNull]
		public VerbParameter FindBoundParameter([NotNull]string name, StringComparison stringComparison)
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