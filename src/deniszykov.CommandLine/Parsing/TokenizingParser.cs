/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;
using System.Collections.Generic;
using deniszykov.CommandLine.Binding;

namespace deniszykov.CommandLine.Parsing
{
	internal abstract class TokenizingParser : IArgumentsParser
	{
		private static readonly Action<string> IgnoreArgument = _ => { };
		private static readonly string[] EmptyArguments = new string[0];

		protected abstract StringComparison ShortNameMatchingMode { get; }
		protected abstract StringComparison LongNameMatchingMode { get; }

		/// <inheritdoc />
		public ParsedArguments Parse(string[] arguments, Func<string, ValueArity?> getOptionArity)
		{
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));
			if (getOptionArity == null) throw new ArgumentNullException(nameof(getOptionArity));

			var shortOptions = new Dictionary<string, OptionValue>(GetStringComparer(this.ShortNameMatchingMode));
			var longOptions = new Dictionary<string, OptionValue>(GetStringComparer(this.LongNameMatchingMode));
			var values = new List<string>();
			var hasHelpOption = false;
			var appendOption = IgnoreArgument;

			foreach (var argumentToken in this.Tokenize(arguments, getOptionArity))
			{
				switch (argumentToken.Type)
				{
					case TokenType.Value:
						appendOption = IgnoreArgument;
						values.Add(argumentToken.Value);
						break;
					case TokenType.UnknownOption:
						if (argumentToken.Value.Length == 1)
						{
							goto case TokenType.ShortOption;
						}
						else
						{
							goto case TokenType.LongOption;
						}
					case TokenType.ShortOption:
						var shortOptionName = argumentToken.Value;
						IncrementOptionCount(shortOptions, shortOptionName);
						// ReSharper disable once AccessToModifiedClosure
						appendOption = optionArgument => AppendOptionArgument(shortOptions, shortOptionName, optionArgument);
						break;
					case TokenType.LongOption:
						var longOptionName = argumentToken.Value;
						IncrementOptionCount(longOptions, longOptionName);
						// ReSharper disable once AccessToModifiedClosure
						appendOption = optionArgument => AppendOptionArgument(longOptions, longOptionName, optionArgument);
						break;
					case TokenType.OptionArgument:
						appendOption(argumentToken.Value);
						break;
					case TokenType.OptionBreak:
						appendOption = IgnoreArgument;
						continue;
					case TokenType.HelpOption:
						appendOption = IgnoreArgument;
						hasHelpOption = true;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			return new ParsedArguments(shortOptions, longOptions, values, hasHelpOption);
		}

		protected abstract IEnumerable<ArgumentToken> Tokenize(string[] arguments, Func<string, ValueArity?> getOptionArity);

		private static void IncrementOptionCount(Dictionary<string, OptionValue> options, string optionName)
		{
			if (options == null) throw new ArgumentNullException(nameof(options));
			if (optionName == null) throw new ArgumentNullException(nameof(optionName));

			if (options.TryGetValue(optionName, out var existingValue))
			{
				options[optionName] = new OptionValue(existingValue.Raw, existingValue.Count + 1);
			}
			else
			{
				options[optionName] = new OptionValue(EmptyArguments, existingValue.Count + 1);
			}
		}
		private static void AppendOptionArgument(Dictionary<string, OptionValue> options, string optionName, string optionArgument)
		{
			if (options == null) throw new ArgumentNullException(nameof(options));
			if (optionName == null) throw new ArgumentNullException(nameof(optionName));
			if (optionArgument == null) throw new ArgumentNullException(nameof(optionArgument));

			if (options.TryGetValue(optionName, out var existingValue))
			{
				if (existingValue.Raw is List<string> list)
				{
					list.Add(optionArgument);
				}
				else
				{
					list = new List<string>();
					list.AddRange(existingValue.Raw);
					list.Add(optionArgument);
					options[optionName] = new OptionValue(list, existingValue.Count);
				}
			}
			else
			{
				options[optionName] = new OptionValue(new List<string> { optionArgument }, existingValue.Count + 1);
			}
		}

		private static StringComparer GetStringComparer(StringComparison comparison)
		{
			switch (comparison)
			{
				case StringComparison.CurrentCulture: return StringComparer.CurrentCulture;
				case StringComparison.CurrentCultureIgnoreCase: return StringComparer.CurrentCultureIgnoreCase;
#if !NETSTANDARD1_6
				case StringComparison.InvariantCulture: return StringComparer.InvariantCulture;
				case StringComparison.InvariantCultureIgnoreCase: return StringComparer.InvariantCultureIgnoreCase;
#endif
				case StringComparison.Ordinal: return StringComparer.Ordinal;
				case StringComparison.OrdinalIgnoreCase: return StringComparer.OrdinalIgnoreCase;
				default: throw new ArgumentOutOfRangeException(nameof(comparison), comparison, null);
			}
		}

	}
}
