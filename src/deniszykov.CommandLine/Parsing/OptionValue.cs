using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Parsing
{
	internal readonly struct OptionValue
	{
		[NotNull, ItemNotNull]
		public readonly IReadOnlyCollection<string> Raw;
		public readonly int Count;

		public OptionValue(IReadOnlyCollection<string> raw, int count)
		{
			if (raw == null) throw new ArgumentNullException(nameof(raw));
			if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

			this.Raw = raw;
			this.Count = count;
		}

		/// <inheritdoc />
		public override string ToString() => string.Join(", ", this.Raw);
	}
}