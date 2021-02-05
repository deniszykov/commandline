using System;
using System.Collections.Generic;
using System.Linq;
using deniszykov.CommandLine.Binding;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Parsing
{
	internal sealed class GetOptParser : TokenizingParser
	{
		public override StringComparison LongNameMatchingMode { get; }
		public override StringComparison ShortNameMatchingMode { get; }
		[NotNull] public string[] ShortNamePrefixes { get; }
		[NotNull] public string[] LongNamePrefixes { get; }
		[NotNull] public string[] OptionsBreaks { get; }
		[NotNull] public string[] HelpOptions { get; }
		[NotNull] public char[] OptionArgumentSplitter { get; }
		public bool TreatUnknownOptionsAsValues { get; }

		public GetOptParser([NotNull] CommandLineConfiguration configuration)
		{
			if (configuration == null) throw new ArgumentNullException(nameof(configuration));

			this.LongNameMatchingMode = configuration.LongOptionNameMatchingMode;
			this.ShortNameMatchingMode = configuration.ShortOptionNameMatchingMode;
			this.ShortNamePrefixes = configuration.ShortOptionNamePrefixes ?? new string[0];
			this.LongNamePrefixes = configuration.LongOptionNamePrefixes ?? new string[0];
			this.OptionsBreaks = configuration.OptionsBreaks ?? new string[0];
			this.HelpOptions = configuration.HelpOptions ?? new string[0];
			this.OptionArgumentSplitter = configuration.OptionArgumentSplitters ?? "=".ToCharArray();
			this.TreatUnknownOptionsAsValues = configuration.TreatUnknownOptionsAsValues;
		}

		protected override IEnumerable<ArgumentToken> Tokenize(string[] arguments, Func<string, ValueArity?> getOptionArity)
		{
			const int MODE_VALUE_OR_OPTION = 0;
			const int MODE_VALUE = 1;
			const int MODE_ONE_OR_MORE_ARGUMENTS = 2;
			const int MODE_ZERO_OR_MORE_ARGUMENTS = 3;
			const int MODE_ZERO_OR_MORE_EXPLICIT_ARGUMENTS = 4;

			var mode = MODE_VALUE_OR_OPTION;
			foreach (var argument in arguments)
			{
				var longName = default(string);
				var shortNameLetters = default(string);

				switch (mode)
				{
					case MODE_VALUE:
						yield return new ArgumentToken(TokenType.Value, argument);
						continue;
					case MODE_VALUE_OR_OPTION:
						if (this.IsOptionsBreak(argument))
						{
							mode = MODE_VALUE;
							yield return new ArgumentToken(TokenType.OptionBreak, argument);
						}
						else if (this.IsHelpOption(argument))
						{
							yield return new ArgumentToken(TokenType.HelpOption, argument);
							yield break;
						}
						else if (this.IsLongNameOption(argument, out longName, out var optionArgument) && getOptionArity(longName) != null)
						{
							yield return new ArgumentToken(TokenType.LongOption, longName);

							if (!string.IsNullOrEmpty(optionArgument))
							{
								yield return new ArgumentToken(TokenType.OptionArgument, optionArgument);
							}

							switch (getOptionArity(longName) ?? ValueArity.ZeroOrMany)
							{
								case ValueArity.Zero:
									mode = MODE_VALUE_OR_OPTION;
									break;
								case ValueArity.OneOrMany:
								case ValueArity.One:
									mode = MODE_ONE_OR_MORE_ARGUMENTS;
									break;
								case ValueArity.ZeroOrMany:
								case ValueArity.ZeroOrOne:
									mode = MODE_ZERO_OR_MORE_ARGUMENTS;
									break;
								default:
									throw new ArgumentOutOfRangeException();
							}
						}
						else if (this.IsShortNameOption(argument, out shortNameLetters, out optionArgument))
						{
							for (var l = 0; l < shortNameLetters.Length; l++)
							{
								var isLast = l == shortNameLetters.Length - 1;
								var letter = shortNameLetters[l];
								var shortName = letter.ToString();
								var arity = getOptionArity(shortName);
								if (arity == null)
								{
									if (l != 0 || this.IsLongNameOption(argument, out longName, out _) == false)
									{
										longName = shortNameLetters.Substring(l);
									}

									if (this.TreatUnknownOptionsAsValues || longName.All(char.IsDigit))
									{
										yield return new ArgumentToken(TokenType.Value, l == 0 ? argument : longName);
										l = shortNameLetters.Length;
									}
									else
									{
										yield return new ArgumentToken(TokenType.UnknownOption, longName);
										mode = MODE_ZERO_OR_MORE_EXPLICIT_ARGUMENTS;
										l = shortNameLetters.Length;
									}
								}
								else
								{
									yield return new ArgumentToken(TokenType.ShortOption, shortName);

									switch (arity.Value)
									{
										case ValueArity.Zero:
											continue;
										case ValueArity.One:
										case ValueArity.OneOrMany:
											if (!isLast) // threat rest as argument for short option (e.g. -s100 is -s=100)
											{
												yield return new ArgumentToken(TokenType.OptionArgument, shortNameLetters.Substring(l + 1));
												mode = arity == ValueArity.One ? MODE_VALUE_OR_OPTION : MODE_ZERO_OR_MORE_ARGUMENTS;
											}
											else
											{
												mode = arity == ValueArity.One ? MODE_ONE_OR_MORE_ARGUMENTS : MODE_ZERO_OR_MORE_ARGUMENTS;
											}
											l = shortNameLetters.Length;
											break;
										case ValueArity.ZeroOrOne:
										case ValueArity.ZeroOrMany:
										default:
											var nextShortNameIsKnown = !isLast && getOptionArity(shortNameLetters[l + 1].ToString()).HasValue;
											if (nextShortNameIsKnown)
											{
												continue;
											}
											else
											{
												goto case ValueArity.One;
											}
									}
								}
							}

							if (!string.IsNullOrEmpty(optionArgument))
							{
								yield return new ArgumentToken(TokenType.OptionArgument, optionArgument);

								if (mode == MODE_ONE_OR_MORE_ARGUMENTS)
								{
									mode = MODE_ZERO_OR_MORE_ARGUMENTS;
								}
							}
						}
						else
						{
							yield return new ArgumentToken(TokenType.Value, argument);
						}
						continue;
					case MODE_ONE_OR_MORE_ARGUMENTS:
						yield return new ArgumentToken(TokenType.OptionArgument, argument);
						mode = MODE_ZERO_OR_MORE_ARGUMENTS;
						continue;
					case MODE_ZERO_OR_MORE_ARGUMENTS:
						if ((this.IsLongNameOption(argument, out longName, out _) && getOptionArity(longName) != null) ||
							(this.IsShortNameOption(argument, out shortNameLetters, out _) && getOptionArity(shortNameLetters[0].ToString()) != null) ||
							this.IsOptionsBreak(argument) ||
							this.IsHelpOption(argument))
						{
							goto case MODE_VALUE_OR_OPTION;
						}
						yield return new ArgumentToken(TokenType.OptionArgument, argument);
						continue;
					case MODE_ZERO_OR_MORE_EXPLICIT_ARGUMENTS:
						if (this.IsLongNameOption(argument, out longName, out _) ||
							this.IsShortNameOption(argument, out shortNameLetters, out _) ||
							this.IsOptionsBreak(argument) ||
							this.IsHelpOption(argument))
						{
							goto case MODE_VALUE_OR_OPTION;
						}
						yield return new ArgumentToken(TokenType.OptionArgument, argument);
						continue;
				}
			}
		}

		private static bool IsExactOption(string argument, string[] optionVariants, StringComparison comparison)
		{
			foreach (var variant in optionVariants)
			{
				if (string.Compare(variant, 0, argument, 0, variant.Length, comparison) == 0 && variant.Length == argument.Length)
				{
					return true;
				}
			}
			return false;
		}
		private bool IsOption(string[] prefixes, string argument, out string optionName, out string optionArgument)
		{
			optionName = default;
			optionArgument = default;

			var valueSplitterIndex = argument.IndexOfAny(this.OptionArgumentSplitter);
			foreach (var optionPrefix in prefixes)
			{
				if (argument.Length <= optionPrefix.Length ||
					argument.StartsWith(optionPrefix, StringComparison.Ordinal) == false)
				{
					continue;
				}

				var nameEnd = valueSplitterIndex < 0 ? argument.Length : valueSplitterIndex;
				optionName = argument.Substring(optionPrefix.Length, nameEnd - optionPrefix.Length);

				if (valueSplitterIndex > 0)
				{
					optionArgument = argument.Substring(valueSplitterIndex + 1);
				}

				return true;
			}

			return false;
		}
		private bool IsShortNameOption(string argument, out string optionName, out string optionArgument)
		{
			return this.IsOption(this.ShortNamePrefixes, argument, out optionName, out optionArgument);
		}
		private bool IsLongNameOption(string argument, out string optionName, out string optionArgument)
		{
			return this.IsOption(this.LongNamePrefixes, argument, out optionName, out optionArgument);
		}
		private bool IsOptionsBreak(string argument)
		{
			return IsExactOption(argument, this.OptionsBreaks, StringComparison.Ordinal);
		}
		private bool IsHelpOption(string argument)
		{
			return IsExactOption(argument, this.HelpOptions, StringComparison.Ordinal);
		}
	}
}
