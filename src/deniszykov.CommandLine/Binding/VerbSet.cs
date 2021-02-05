using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using deniszykov.CommandLine.Annotations;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Binding
{
	public sealed class VerbSet
	{
		[NotNull]
		public readonly string Name;
		[NotNull]
		public readonly string Description;
		[NotNull, ItemNotNull]
		public readonly IReadOnlyCollection<Verb> Verbs;

		public VerbSet([NotNull] TypeInfo type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			var verbs = new List<Verb>();
			foreach (var method in type.GetAllMethods().OrderBy(m => m, Comparer<MethodInfo>.Create(CompareMethods)))
			{
				if (HasInvalidSignature(method))
					continue;

				var verb = new Verb(method);
				verbs.Add(verb);
			}

			this.Verbs = verbs;
			this.Name = type.GetName() ?? type.Name;
			this.Description = type.GetDescription() ?? string.Empty;
		}

		[CanBeNull]
		public Verb FindVerb([NotNull] string name, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));

			foreach (var verb in this.Verbs)
			{
				if (string.Equals(verb.Name, name, comparison))
				{
					return verb;
				}
			}
			return null;
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
