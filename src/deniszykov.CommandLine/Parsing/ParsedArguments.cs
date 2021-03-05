/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace deniszykov.CommandLine.Parsing
{
	internal readonly struct ParsedArguments
	{
		private readonly IDictionary<string, OptionValue> shortOptions;
		private readonly IDictionary<string, OptionValue> longOptions;

		public readonly IReadOnlyCollection<string> Values;
		public readonly bool HasHelpOption;

		public ParsedArguments(IDictionary<string, OptionValue> shortOptions, IDictionary<string, OptionValue> longOptions, IReadOnlyCollection<string> values, bool hasHelpOption)
		{
			if (shortOptions == null) throw new ArgumentNullException(nameof(shortOptions));
			if (longOptions == null) throw new ArgumentNullException(nameof(longOptions));
			if (values == null) throw new ArgumentNullException(nameof(values));

			this.shortOptions = shortOptions;
			this.longOptions = longOptions;
			this.Values = values;
			this.HasHelpOption = hasHelpOption;
		}

		public bool TryGetValue(int position, out OptionValue optionValue)
		{
			optionValue = default;

			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			if (this.Values == null)
			{
				return false;
			}

			if (position == -1) // special 'params' parameter
			{
				optionValue = new OptionValue(this.Values, 1);
				return true;
			}

			if (this.Values.Count <= position)
			{
				return false;
			}
			optionValue = new OptionValue(new[] { this.Values.ElementAt(position) }, 1);
			return true;
		}

		public bool TryGetShortOption(string name, out OptionValue optionValue)
		{
			optionValue = default;

			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			if (this.shortOptions == null)
			{
				return false;
			}

			return this.shortOptions.TryGetValue(name, out optionValue);
		}

		public bool TryGetLongOption(string name, out OptionValue optionValue)
		{
			optionValue = default;

			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			if (this.longOptions == null)
			{
				return false;
			}

			return this.longOptions.TryGetValue(name, out optionValue);
		}
	}
}