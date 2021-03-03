/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;
using System.Collections.Generic;

namespace deniszykov.CommandLine.Parsing
{
	internal readonly struct OptionValue
	{
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