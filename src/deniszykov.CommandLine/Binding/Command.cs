using System;
using System.Collections.Generic;
using System.Linq;
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
		[NotNull, ItemNotNull]
		public readonly IReadOnlyCollection<CommandParameter> BoundParameters;
		[NotNull, ItemNotNull]
		public readonly IReadOnlyCollection<CommandParameter> ServiceParameters;
		[NotNull]
		public readonly Func<object, object[], int> Invoker;

		public bool Hidden;

		public Command([NotNull] MethodInfo method)
		{
			if (method == null) throw new ArgumentNullException(nameof(method));

			this.Name = method.GetName() ?? method.Name;
			this.Description = method.GetDescription() ?? string.Empty;
			this.Hidden = method.IsHidden();

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

		[NotNull, ItemNotNull]
		public IEnumerable<CommandParameter> GetNonHiddenBoundParameter()
		{
			return this.BoundParameters.Where(param => !param.IsHidden);
		}

		[CanBeNull]
		public CommandParameter FindBoundParameter([NotNull]string name, StringComparison stringComparison)
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