using System;
using System.Collections.Generic;
using System.Linq;
using deniszykov.CommandLine.Binding;
using JetBrains.Annotations;

namespace deniszykov.CommandLine
{
	internal static class PropertyExtensions
	{
		/// <summary>
		/// Name of property with list of preceding verbs of current <see cref="IVerbSetBuilder"/>. Type is array of <see cref="Verb"/>.
		/// </summary>
		public const string VerbChainPropertyName = "__verb_chain__";

		[NotNull, ItemNotNull]
		public static IEnumerable<Verb> GetVerbChain([NotNull] this IDictionary<object, object> properties)
		{
			if (properties == null) throw new ArgumentNullException(nameof(properties));

			if (properties.TryGetValue(VerbChainPropertyName, out var verbChainObj) &&
				verbChainObj is Verb[] verbChain)
			{
				return verbChain;
			}
			else
			{
				return Enumerable.Empty<Verb>();
			}
		}
		public static void AddVertToChain([NotNull] this IDictionary<object, object> properties, [NotNull] Verb verb)
		{
			if (properties == null) throw new ArgumentNullException(nameof(properties));
			if (verb == null) throw new ArgumentNullException(nameof(verb));

			var verbChain = properties.GetVerbChain().ToList();
			if (verbChain.Contains(verb))
			{
				throw CommandLineException.RecursiveVerbChain(verbChain.Select(otherVerb => otherVerb.Name), verb.Name);
			}
			verbChain.Add(verb);
			properties[VerbChainPropertyName] = verbChain.ToArray();
		}
	}
}
