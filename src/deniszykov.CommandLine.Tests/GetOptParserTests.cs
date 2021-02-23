using System;
using System.Linq;
using System.Reflection;
using deniszykov.CommandLine.Annotations;
using deniszykov.CommandLine.Binding;
using deniszykov.CommandLine.Parsing;
using Xunit;
// ReSharper disable StringLiteralTypo

namespace deniszykov.CommandLine.Tests
{
	public class GetOptParserTests
	{
		public class TestApi
		{
			public int Test
			(
				[Alias("o"), Name("one-or-zero-param")]
				bool zOneP, // zero or one arity
				[Alias("z"), Name("zero-param")]
				OptionCount zeroP, // zero arity
				[Alias("m"), Name("zero-or-more-param")]
				string[] zMoreP, // zero or more arity
				[Alias("k"), Name("one-param")]
				int oneP, // one arity
				[Alias("Z"), Name("zero-up-param")]
				OptionCount zeroUpP// zero arity
			)
			{
				return 0;
			}
		}

		[Theory]
		// solo
		[InlineData(new[] { "-o" }, "o", new string[0], 1)]
		[InlineData(new[] { "-o", "-o" }, "o", new string[0], 2)]
		[InlineData(new[] { "-o", "1", "-o" }, "o", new[] { "1" }, 2)]
		[InlineData(new[] { "-o", "1", "-o", "2" }, "o", new[] { "1", "2" }, 2)]
		[InlineData(new[] { "-o", "1", "-k", "2" }, "o", new[] { "1" }, 1)]
		// special symbols
		[InlineData(new[] { "-k", "-" }, "k", new[] { "-" }, 1)]
		[InlineData(new[] { "-k", "@" }, "k", new[] { "@" }, 1)]
		[InlineData(new[] { "-k", "/" }, "k", new[] { "/" }, 1)]
		[InlineData(new[] { "-k", "?" }, "k", new[] { "?" }, 1)]
		[InlineData(new[] { "-k", "/?" }, "k", new[] { "/?" }, 1)]
		[InlineData(new[] { "-k", "-?" }, "k", new[] { "-?" }, 1)]
		[InlineData(new[] { "-k", "-h" }, "k", new[] { "-h" }, 1)]
		[InlineData(new[] { "-k", "--help" }, "k", new[] { "--help" }, 1)]
		// excluding special characters when greedy scanning
		[InlineData(new[] { "-k", "-", "-", "--" }, "k", new[] { "-", "-" }, 1)]
		[InlineData(new[] { "-k", "-", "-", "/?" }, "k", new[] { "-", "-" }, 1)]
		[InlineData(new[] { "-k", "-", "-", "-?" }, "k", new[] { "-", "-" }, 1)]
		[InlineData(new[] { "-k", "-", "-", "-h" }, "k", new[] { "-", "-" }, 1)]
		[InlineData(new[] { "-k", "-", "-", "--help" }, "k", new[] { "-", "-" }, 1)]
		// combined
		[InlineData(new[] { "-zo" }, "o", new string[0], 1)]
		[InlineData(new[] { "-zo" }, "z", new string[0], 1)]
		[InlineData(new[] { "-zom" }, "m", new string[0], 1)]
		[InlineData(new[] { "-moz" }, "m", new string[0], 1)]
		// combined with arg
		[InlineData(new[] { "-mzo123" }, "o", new[] { "123" }, 1)]
		[InlineData(new[] { "-mo123a" }, "o", new[] { "123a" }, 1)]
		[InlineData(new[] { "-md123a" }, "m", new[] { "d123a" }, 1)]
		// combined with explicit arg
		[InlineData(new[] { "-mzo=123" }, "o", new[] { "123" }, 1)]
		[InlineData(new[] { "-mzo321=123" }, "o", new[] { "321", "123" }, 1)]
		[InlineData(new[] { "-mzo321", "123" }, "o", new[] { "321", "123" }, 1)]
		[InlineData(new[] { "-mzoo=123" }, "o", new[] { "123" }, 2)]
		// case-sensitivity
		[InlineData(new[] { "-z" }, "z", new string[0], 1)]
		[InlineData(new[] { "-Z" }, "Z", new string[0], 1)]
		[InlineData(new[] { "-zZz" }, "Z", new string[0], 1)]
		[InlineData(new[] { "-ZZZ" }, "Z", new string[0], 3)]
		// unknown options
		[InlineData(new[] { "-u=true" }, "u", new[] { "true" }, 1)]
		[InlineData(new[] { "-u=true", "-u" }, "u", new[] { "true" }, 2)]
		[InlineData(new[] { "-u=true", "false" }, "u", new[] { "true", "false" }, 1)]
		//
		public void ParseShortOptions(string[] args, string optionName, string[] expectedArguments, int expectedCount)
		{
			var configuration = new CommandLineConfiguration();

			var parser = new GetOptParser(configuration);
			var verb = new VerbSet(typeof(TestApi).GetTypeInfo()).FindVerb(nameof(TestApi.Test));
			var getOptionArity = new Func<string, ValueArity?>(n => verb!.FindBoundParameter(n, n.Length > 1 ? configuration.LongOptionNameMatchingMode : configuration.ShortOptionNameMatchingMode)?.ValueArity);

			var parsedArguments = parser.Parse(args, getOptionArity);

			Assert.True(parsedArguments.TryGetShortOption(optionName, out var optionValue));
			Assert.Equal(expectedArguments, optionValue.Raw.ToArray());
			Assert.Equal(expectedCount, optionValue.Count);
		}

		[Theory]
		// solo
		[InlineData(new[] { "--one-or-zero-param" }, "one-or-zero-param", new string[0], 1)]
		[InlineData(new[] { "--one-or-zero-param", "--one-or-zero-param" }, "one-or-zero-param", new string[0], 2)]
		[InlineData(new[] { "--one-or-zero-param", "true", "--one-or-zero-param" }, "one-or-zero-param", new[] { "true" }, 2)]
		[InlineData(new[] { "--one-or-zero-param", "true", "--one-or-zero-param", "false" }, "one-or-zero-param", new[] { "true", "false" }, 2)]
		// explicit arg
		[InlineData(new[] { "--one-or-zero-param=true" }, "one-or-zero-param", new[] { "true" }, 1)]
		[InlineData(new[] { "--one-or-zero-param=true", "false" }, "one-or-zero-param", new[] { "true", "false" }, 1)]
		// unknown options
		[InlineData(new[] { "--unknown-param=true" }, "unknown-param", new[] { "true" }, 1)]
		[InlineData(new[] { "--unknown-param=true", "--unknown-param" }, "unknown-param", new[] { "true" }, 2)]
		[InlineData(new[] { "--unknown-param=true", "false" }, "unknown-param", new[] { "true", "false" }, 1)]
		public void ParseLongOptions(string[] args, string optionName, string[] expectedArguments, int expectedCount)
		{
			var configuration = new CommandLineConfiguration();

			var parser = new GetOptParser(configuration);
			var verb = new VerbSet(typeof(TestApi).GetTypeInfo()).FindVerb(nameof(TestApi.Test));
			var getOptionArity = new Func<string, ValueArity?>(n => verb!.FindBoundParameter(n, n.Length > 1 ? configuration.LongOptionNameMatchingMode : configuration.ShortOptionNameMatchingMode)?.ValueArity);

			var parsedArguments = parser.Parse(args, getOptionArity);

			Assert.True(parsedArguments.TryGetLongOption(optionName, out var optionValue));
			Assert.Equal(expectedArguments, optionValue.Raw.ToArray());
			Assert.Equal(expectedCount, optionValue.Count);
		}

		[Theory]
		[InlineData(new[] { "a" }, new[] { "a" })]
		[InlineData(new[] { "a", "b" }, new[] { "a", "b" })]
		[InlineData(new[] { "a", "b", "ccc" }, new[] { "a", "b", "ccc" })]
		[InlineData(new[] { "-1", "-1.2", "+100500" }, new[] { "-1", "-1.2", "+100500" })]
		[InlineData(new[] { "http://example.com" }, new[] { "http://example.com" })]
		[InlineData(new[] { "a=b" }, new[] { "a=b" })]
		[InlineData(new[] { "-u" }, new[] { "-u" })]
		[InlineData(new[] { "-unknown-param" }, new[] { "-unknown-param" })]
		[InlineData(new[] { "--unknown-param" }, new[] { "--unknown-param" })]
		[InlineData(new[] { "--unknown-param=value" }, new[] { "--unknown-param=value" })]
		[InlineData(new[] { "-zZ", "--unknown-param=value" }, new[] { "--unknown-param=value" })]
		[InlineData(new[] { "--unknown-param=value", "-Zz" }, new[] { "--unknown-param=value" })]
		[InlineData(new[] { "-Zz", "--unknown-param=value", "-Zz" }, new[] { "--unknown-param=value" })]
		[InlineData(new[] { "-Zz", "--zero-param", "--unknown-param=value", "-Zz" }, new[] { "--unknown-param=value" })]
		[InlineData(new[] { "-Zz", "--zero-param", "param", "-Zz" }, new[] { "param" })]
		// Options break
		[InlineData(new[] { "-Zz", "--zero-param", "--", "-1", "-1.2", "+100500", "http://example.com", "-Zz" }, new[] { "-1", "-1.2", "+100500", "http://example.com", "-Zz" })]
		public void ParseValues(string[] args, string[] expectedValues)
		{
			var configuration = new CommandLineConfiguration {
				TreatUnknownOptionsAsValues = true
			};

			var parser = new GetOptParser(configuration);
			var verb = new VerbSet(typeof(TestApi).GetTypeInfo()).FindVerb(nameof(TestApi.Test));
			var getOptionArity = new Func<string, ValueArity?>(n => verb!.FindBoundParameter(n, n.Length > 1 ? configuration.LongOptionNameMatchingMode : configuration.ShortOptionNameMatchingMode)?.ValueArity);

			var parsedArguments = parser.Parse(args, getOptionArity);

			Assert.Equal(expectedValues, parsedArguments.Values);
		}

		[Theory]
		// solo
		[InlineData(new[] { "--zero-param" }, false)]
		[InlineData(new[] { "--zero-param", "-h", "--one-or-zero-param" }, true)]
		[InlineData(new[] { "--zero-param", "/h", "--one-or-zero-param" }, true)]
		[InlineData(new[] { "--zero-param", "--help", "--one-or-zero-param" }, true)]
		[InlineData(new[] { "--zero-param", "-?", "--one-or-zero-param" }, true)]
		[InlineData(new[] { "--zero-param", "/?", "--one-or-zero-param" }, true)]
		[InlineData(new[] { "/?", "--one-or-zero-param", "--one-or-zero-param" }, true)]
		[InlineData(new[] { "--one-or-zero-param", "--zero-param", "--help" }, true)]
		public void HelpOption(string[] args, bool hasHelpOption)
		{
			var configuration = new CommandLineConfiguration();

			var parser = new GetOptParser(configuration);
			var verb = new VerbSet(typeof(TestApi).GetTypeInfo()).FindVerb(nameof(TestApi.Test));
			var getOptionArity = new Func<string, ValueArity?>(n => verb!.FindBoundParameter(n, n.Length > 1 ? configuration.LongOptionNameMatchingMode : configuration.ShortOptionNameMatchingMode)?.ValueArity);

			var parsedArguments = parser.Parse(args, getOptionArity);

			Assert.Equal(hasHelpOption, parsedArguments.HasHelpOption);
		}
	}
}
