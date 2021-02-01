using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using deniszykov.CommandLine.Annotations;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Binding
{
	public sealed class Command
	{
		[NotNull]
		public readonly string Name;
		[NotNull]
		public readonly string Description;
		[CanBeNull]
		public readonly TypeInfo TargetType;
		[NotNull]
		public readonly IReadOnlyCollection<CommandParameter> BoundParameters;
		[NotNull]
		public readonly IReadOnlyCollection<CommandParameter> ServiceParameters;
		[NotNull]
		public readonly Func<object, object[], int> Invoker;

		public Command([NotNull] MethodInfo method)
		{
			if (method == null) throw new ArgumentNullException(nameof(method));

			this.Name = method.GetName() ?? method.Name;
			this.Description = method.GetDescription() ?? string.Empty;

			var boundParameters = new List<CommandParameter>();
			var serviceParameters = new List<CommandParameter>();

			foreach (var parameterInfo in method.GetParameters())
			{

				if (parameterInfo.IsServiceParameter())
				{
					var parameter = new CommandParameter(parameterInfo, serviceParameters.Count);
					serviceParameters.Add(parameter);
				}
				else
				{
					var parameter = new CommandParameter(parameterInfo, boundParameters.Count);
					boundParameters.Add(parameter);
				}
			}

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

		/// <inheritdoc />
		public override string ToString() => this.Name;
	}
}