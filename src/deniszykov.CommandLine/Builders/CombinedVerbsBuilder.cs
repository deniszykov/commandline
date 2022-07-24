using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using deniszykov.CommandLine.Binding;

namespace deniszykov.CommandLine.Builders
{
	internal sealed class CombinedVerbsBuilder : IVerbSetBuilder
	{
		private readonly IReadOnlyList<IVerbSetBuilder> verbSetBuilders;

		private CombinedVerbsBuilder(IReadOnlyList<IVerbSetBuilder> verbSetBuilders)
		{
			if (verbSetBuilders == null) throw new ArgumentNullException(nameof(verbSetBuilders));

			this.verbSetBuilders = verbSetBuilders;
		}

		/// <inheritdoc />
		public VerbSet Build()
		{
			var combinedVerbSet = default(VerbSet);
			foreach (var verbSetBuilder in this.verbSetBuilders)
			{
				var verbSet = verbSetBuilder.Build();
				if (combinedVerbSet != null)
				{
					combinedVerbSet = new VerbSet(
						combinedVerbSet.Name,
						combinedVerbSet.Description,
						combinedVerbSet.Verbs.Concat(verbSet.Verbs).ToList()
					);
				}
				else
				{
					combinedVerbSet = verbSet;
				}
			}
			return combinedVerbSet ?? new VerbSet(typeof(object).GetTypeInfo());
		}

		public static IVerbSetBuilder Create(IReadOnlyList<IVerbSetBuilder> verbSetBuilders)
		{
			if (verbSetBuilders == null) throw new ArgumentNullException(nameof(verbSetBuilders));

			if (verbSetBuilders.Count == 1)
			{
				return verbSetBuilders[0];
			}
			return new CombinedVerbsBuilder(verbSetBuilders);
		}
	}
}
