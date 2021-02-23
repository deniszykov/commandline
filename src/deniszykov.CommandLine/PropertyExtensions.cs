using System;
using System.Collections.Generic;
using System.Linq;
using deniszykov.CommandLine.Binding;

namespace deniszykov.CommandLine
{
	internal static class PropertyExtensions
	{
		/// <summary>
		/// Name of property with list of preceding verbs of current <see cref="IVerbSetBuilder"/>. Type is array of <see cref="Verb"/>.
		/// </summary>
		public const string VERB_CHAIN_PROPERTY_NAME = "__verb_chain__";

		public static IEnumerable<Verb> GetVerbChain(this IDictionary<object, object> properties)
		{
			if (properties == null) throw new ArgumentNullException(nameof(properties));

			if (properties.TryGetValue(VERB_CHAIN_PROPERTY_NAME, out var verbChainObj) &&
				verbChainObj is Verb[] verbChain)
			{
				return verbChain;
			}
			else
			{
				return Enumerable.Empty<Verb>();
			}
		}
		public static void AddVerbToChain(this IDictionary<object, object> properties, Verb verb)
		{
			if (properties == null) throw new ArgumentNullException(nameof(properties));
			if (verb == null) throw new ArgumentNullException(nameof(verb));

			var verbChain = properties.GetVerbChain().ToList();
			if (verbChain.Contains(verb))
			{
				throw CommandLineException.RecursiveVerbChain(verbChain.Select(otherVerb => otherVerb.Name), verb.Name);
			}
			verbChain.Add(verb);
			properties[VERB_CHAIN_PROPERTY_NAME] = verbChain.ToArray();
		}
	}
}
