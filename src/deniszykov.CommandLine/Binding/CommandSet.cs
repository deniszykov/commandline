using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using deniszykov.CommandLine.Annotations;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Binding
{
	public sealed class CommandSet
	{
		[NotNull]
		public readonly string Name;
		[NotNull]
		public readonly string Description;
		[NotNull, ItemNotNull]
		public readonly IReadOnlyCollection<Command> Commands;

		public CommandSet([NotNull] TypeInfo type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			var commands = new List<Command>();
			foreach (var method in type.GetAllMethods().OrderBy(m => m, Comparer<MethodInfo>.Create(CompareMethods)))
			{
				if (method.IsHidden() || HasInvalidSignature(method))
					continue;

				var command = new Command(method);
				commands.Add(command);
			}

			this.Commands = commands;
			this.Name = type.GetName() ?? type.Name;
			this.Description = type.GetDescription() ?? string.Empty;
		}

		private static bool HasInvalidSignature(MethodInfo method)
		{
			if (method == null) throw new ArgumentNullException(nameof(method));

			return method.ReturnType != typeof(int) || method.IsGenericMethod && method.IsSpecialName;
		}

		private static int CompareMethods(MethodInfo x, MethodInfo y)
		{
			return y.GetParameters().Length.CompareTo(x.GetParameters().Length);
		}

		/// <inheritdoc />
		public override string ToString() => this.Name;
	}
}
