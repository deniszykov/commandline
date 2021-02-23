using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using deniszykov.CommandLine.Annotations;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Binding
{
	/// <summary>
	/// Set of <see cref="Verbs"/>.
	/// </summary>
	/// 
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public sealed class VerbSet
	{
		/// <summary>
		/// Name of verb's set. Not used for help text.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// Description or help text for this verb set.
		/// </summary>
		public readonly string Description;
		/// <summary>
		/// Collection of <see cref="Verb"/> in thi set.
		/// </summary>
		public readonly IReadOnlyCollection<Verb> Verbs;

		public VerbSet(
			 string name,
			 string description,
			 IReadOnlyCollection<Verb> verbs)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (description == null) throw new ArgumentNullException(nameof(description));
			if (verbs == null) throw new ArgumentNullException(nameof(verbs));

			this.Name = name;
			this.Description = description;
			this.Verbs = verbs;
		}
		/// <summary>
		/// Constructor of <see cref="VerbSet"/> from <see cref="TypeInfo"/>.
		/// </summary>
		public VerbSet(TypeInfo type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			var verbs = new List<Verb>();
			foreach (var method in type.GetAllMethods().OrderBy(m => m, Comparer<MethodInfo>.Create(CompareMethods)))
			{
				if (HasInvalidSignature(method) || method.DeclaringType == typeof(object))
					continue;

				var verb = new Verb(method);
				verbs.Add(verb);
			}

			this.Verbs = verbs;
			this.Name = type.GetName() ?? type.Name;
			this.Description = type.GetDescription() ?? string.Empty;
		}

		/// <summary>
		/// Find <see cref="Verb"/> in <see cref="Verbs"/> by it's name.
		/// </summary>
		/// <param name="name">Verb's name.</param>
		/// <param name="comparison">Name's comparison mode.</param>
		/// <returns>Found <see cref="Verb"/> or null.</returns>
		public Verb? FindVerb(string name, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
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

		internal IEnumerable<Verb> GetNonHiddenVerbs()
		{
			return this.Verbs.Where(v => !v.IsHidden);
		}

		private static bool HasInvalidSignature(MethodInfo method)
		{
			if (method == null) throw new ArgumentNullException(nameof(method));

			return (method.ReturnType != typeof(int) && method.ReturnType != typeof(Task<int>)) ||
				method.IsGenericMethod && method.IsSpecialName;
		}

		private static int CompareMethods(MethodInfo x, MethodInfo y)
		{
			return y.GetParameters().Length.CompareTo(x.GetParameters().Length);
		}

		/// <inheritdoc />
		public override string ToString() => this.Name;
	}
}
