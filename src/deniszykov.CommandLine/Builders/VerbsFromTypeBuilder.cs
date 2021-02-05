using System;
using System.Reflection;
using deniszykov.CommandLine.Binding;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Builders
{
	internal sealed class VerbsFromTypeBuilder : IVerbSetBuilder
	{
		[NotNull] private readonly TypeInfo verbSetType;
		private VerbSet verbSet;

		public VerbsFromTypeBuilder([NotNull] TypeInfo verbSetType)
		{
			if (verbSetType == null) throw new ArgumentNullException(nameof(verbSetType));

			this.verbSetType = verbSetType;
		}

		/// <inheritdoc />
		public VerbSet Build()
		{
			return this.verbSet ??= new VerbSet(this.verbSetType);
		}
	}
}
