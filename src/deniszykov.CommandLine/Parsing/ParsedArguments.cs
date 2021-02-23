using System;
using System.Collections.Generic;
using System.Linq;

namespace deniszykov.CommandLine.Parsing
{
	internal struct ParsedArguments
	{
		public readonly IDictionary<string, OptionValue> ShortOptions;
		public readonly IDictionary<string, OptionValue> LongOptions;
		public readonly IReadOnlyCollection<string> Values;
		public bool HasHelpOption;

		public ParsedArguments(IDictionary<string, OptionValue> shortOptions, IDictionary<string, OptionValue> longOptions, IReadOnlyCollection<string> values, bool hasHelpOption)
		{
			if (shortOptions == null) throw new ArgumentNullException(nameof(shortOptions));
			if (longOptions == null) throw new ArgumentNullException(nameof(longOptions));
			if (values == null) throw new ArgumentNullException(nameof(values));

			this.ShortOptions = shortOptions;
			this.LongOptions = longOptions;
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
			if (this.ShortOptions == null)
			{
				return false;
			}

			return this.ShortOptions.TryGetValue(name, out optionValue);
		}

		public bool TryGetLongOption(string name, out OptionValue optionValue)
		{
			optionValue = default;

			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			if (this.LongOptions == null)
			{
				return false;
			}

			return this.LongOptions.TryGetValue(name, out optionValue);
		}
	}
}