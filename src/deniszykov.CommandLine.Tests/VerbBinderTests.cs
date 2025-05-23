﻿using System;
using System.Collections;
using System.ComponentModel.Design;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using deniszykov.CommandLine.Annotations;
using deniszykov.CommandLine.Binding;
using deniszykov.TypeConversion;
using Xunit;
using Xunit.Abstractions;

namespace deniszykov.CommandLine.Tests
{
	public class VerbBinderTests
	{
		public class TestApi
		{
			public int Param1;
			public bool Param2;
			public int Param3Value;
			public string[]? Param4;
			public int Param5Value;
			public string? Param6;
			public int[]? ExtraIntegersParam;
			public string[]? ExtraStringsParam;
			public ConsoleColor? Param7Value;

			public int NamedBindVerb
			(
				[Alias("k"), Name("param-1-int")]
				int param1 = default,
				[Alias("o"), Name("param-2-bool")]
				bool param2 = default,
				[Alias("z"), Name("param-3-count")]
				OptionCount param3 = default,
				[Alias("m"), Name("param-4-string-array")]
				string[]? param4 = default,
				[Alias("Z"), Name("param-5-count")]
				OptionCount param5 = default,
				[Alias("s"), Name("param-6-string")]
				string? param6 = default,
				[Alias("e"), Name("param-7-enum")]
				ConsoleColor? param7 = default
			)
			{
				this.Param1 = param1;
				this.Param2 = param2;
				this.Param3Value = param3.Value;
				this.Param4 = param4;
				this.Param5Value = param5.Value;
				this.Param6 = param6;
				this.Param7Value = param7;

				return 0;
			}

			public int PositionalBindVerb1
			(
				int param1
			)
			{
				this.Param1 = param1;

				return 0;
			}

			public int PositionalBindVerb2
			(
				int param1,
				bool param2
			)
			{
				this.Param1 = param1;
				this.Param2 = param2;

				return 0;
			}

			public int PositionalBindVerb3
			(
				int param1,
				bool param2,
				int param3
			)
			{
				this.Param1 = param1;
				this.Param2 = param2;
				this.Param3Value = param3;

				return 0;
			}

			public int PositionalBindVerb4
			(
				int param1,
				bool param2 ,
				int param3,
				string[]? param4
			)
			{
				this.Param1 = param1;
				this.Param2 = param2;
				this.Param3Value = param3;
				this.Param4 = param4;

				return 0;
			}

			public int PositionalBindVerb5
			(
				int param1,
				bool param2,
				int param3,
				string[]? param4,
				int param5
			)
			{
				this.Param1 = param1;
				this.Param2 = param2;
				this.Param3Value = param3;
				this.Param4 = param4;
				this.Param5Value = param5;

				return 0;
			}

			public int PositionalBindVerb6
			(
				int param1 ,
				bool param2,
				int param3,
				string[]? param4,
				int param5,
				string? param6
			)
			{
				this.Param1 = param1;
				this.Param2 = param2;
				this.Param3Value = param3;
				this.Param4 = param4;
				this.Param5Value = param5;
				this.Param6 = param6;

				return 0;
			}

			public int TwoParamBindVerb
			(
				[Name("param-1-int")]
				int param1,
				[FromService]
				IConsole console,
				[Name("param-2-int")]
				int param2
			)
			{
				this.Param1 = param1;
				this.Param3Value = param2;

				console.WriteLine($"{nameof(this.TwoParamBindVerb)} {param1} {param2}");

				return 0;
			}

			public int ExtraStringParamBindVerb
			(
				[ Name("param-1-int")]
				int param1,
				[Name("param-2-int")]
				bool param2,
				[Name("param-3-int")]
				int param3,
				[Name("extra")]
				params string[] extraStringsParam
			)
			{
				this.Param1 = param1;
				this.Param2 = param2;
				this.Param3Value = param3;
				this.ExtraStringsParam = extraStringsParam;

				return 0;
			}

			public int ExtraIntParamsBindVerb
			(
				[ Name("param-1-int")]
				int param1,
				[Name("extra")]
				params int[] extraIntegersParam
			)
			{
				this.Param1 = param1;
				this.ExtraIntegersParam = extraIntegersParam;

				return 0;
			}
		}

		private readonly ITestOutputHelper output;

		public VerbBinderTests(ITestOutputHelper output)
		{
			this.output = output;
		}

		[Theory]
		[InlineData(new[] { "" }, nameof(TestApi.Param1), 0, false)]
		// int param biding
		[InlineData(new[] { "--param-1-int=1" }, nameof(TestApi.Param1), 1, true)]
		[InlineData(new[] { "--param-1-int=-1" }, nameof(TestApi.Param1), -1, true)]
		[InlineData(new[] { "--param-1-int", "1" }, nameof(TestApi.Param1), 1, true)]
		[InlineData(new[] { "--param-1-int", "-1" }, nameof(TestApi.Param1), -1, true)]
		// string and string[] binding
		[InlineData(new[] { "--param-1-int", "-1" }, nameof(TestApi.Param6), null, true)]
		[InlineData(new[] { "--param-1-int", "-1", "--param-4-string-array", "param", "-", "--" }, nameof(TestApi.Param4), new[] { "param", "-" }, true)]
		[InlineData(new[] { "--param-1-int", "-1", "--param-6-string", "param" }, nameof(TestApi.Param6), "param", true)]
		[InlineData(new[] { "--param-1-int", "-1", "--param-6-string", "" }, nameof(TestApi.Param6), "", true)]
		// enum binding
		[InlineData(new[] { "--param-7-enum", "DarkMagenta" }, nameof(TestApi.Param7Value), ConsoleColor.DarkMagenta, true)]
		[InlineData(new[] { "--param-7-enum", "green" }, nameof(TestApi.Param7Value), ConsoleColor.Green, true)]
		// count binding
		[InlineData(new[] { "--param-3-count", "--param-3-count", "--param-3-count", "--param-3-count" }, nameof(TestApi.Param3Value), 4, true)]
		[InlineData(new[] { "--param-3-count", "--param-5-count", "--param-3-count", "--param-3-count" }, nameof(TestApi.Param5Value), 1, true)]
		public async Task BindLongOptionNameTest(string[] arguments, string propertyName, object propertyValue, bool success)
		{
			var testApi = new TestApi();
			var configuration = new CommandLineConfiguration();
			var typeConversionProvider = new TypeConversionProvider();
			var container = new ServiceContainer();
			container.AddService(testApi.GetType(), testApi);

			var verbSet = new VerbSet(typeof(TestApi).GetTypeInfo());
			var bind = new VerbBinder(configuration, typeConversionProvider, container);
			var bindingResult = bind.Bind(verbSet, nameof(TestApi.NamedBindVerb), arguments);

			this.output.WriteLine(bindingResult.ToString());

			if (success)
			{
				Assert.IsType<VerbBindingResult.Bound>(bindingResult);
			}
			else
			{
				return;
			}
			Assert.Equal(0, await ((VerbBindingResult.Bound)bindingResult).InvokeAsync());

			var expectedPropertyValue = testApi.GetType().GetField(propertyName)!.GetValue(testApi);
			Assert.Equal(propertyValue, expectedPropertyValue);
		}

		[Theory]
		// int param biding
		[InlineData(new[] { "-k=1" }, nameof(TestApi.Param1), 1, true)]
		[InlineData(new[] { "-k=-1" }, nameof(TestApi.Param1), -1, true)]
		[InlineData(new[] { "-k", "1" }, nameof(TestApi.Param1), 1, true)]
		[InlineData(new[] { "-k", "-1" }, nameof(TestApi.Param1), -1, true)]
		// string and string[] binding
		[InlineData(new[] { "-k", "-1" }, nameof(TestApi.Param6), null, true)]
		[InlineData(new[] { "-k", "-1", "-m", "param", "-", "--" }, nameof(TestApi.Param4), new[] { "param", "-" }, true)]
		[InlineData(new[] { "-k", "-1", "-s", "param" }, nameof(TestApi.Param6), "param", true)]
		// count binding
		[InlineData(new[] { "-z", "-z", "-z", "-z" }, nameof(TestApi.Param3Value), 4, true)]
		[InlineData(new[] { "-z", "-Z", "-z", "-z" }, nameof(TestApi.Param5Value), 1, true)]
		public async Task BindShortOptionNameTest(string[] arguments, string propertyName, object propertyValue, bool success)
		{
			var testApi = new TestApi();
			var configuration = new CommandLineConfiguration();
			var typeConversionProvider = new TypeConversionProvider();
			var container = new ServiceContainer();
			container.AddService(testApi.GetType(), testApi);

			var verbSet = new VerbSet(typeof(TestApi).GetTypeInfo());
			var bind = new VerbBinder(configuration, typeConversionProvider, container);
			var bindingResult = bind.Bind(verbSet, nameof(TestApi.NamedBindVerb), arguments);

			this.output.WriteLine(bindingResult.ToString());

			if (success)
			{
				Assert.IsType<VerbBindingResult.Bound>(bindingResult);
			}
			else
			{
				return;
			}
			Assert.Equal(0, await ((VerbBindingResult.Bound)bindingResult).InvokeAsync());

			var expectedPropertyValue = testApi.GetType().GetField(propertyName)!.GetValue(testApi);
			Assert.Equal(propertyValue, expectedPropertyValue);
		}


		[Theory]
		// positional bindings - fn(int, bool, int, string[], int, string);
		[InlineData(new[] { "100500" }, nameof(TestApi.PositionalBindVerb1), nameof(TestApi.Param1), 100500, true)]
		[InlineData(new[] { "1", "true" }, nameof(TestApi.PositionalBindVerb2), nameof(TestApi.Param2), true, true)]
		[InlineData(new[] { "1", "true", "1" }, nameof(TestApi.PositionalBindVerb3), nameof(TestApi.Param3Value), 1, true)]
		[InlineData(new[] { "1", "true", "1", "string value" }, nameof(TestApi.PositionalBindVerb4), nameof(TestApi.Param4), new[] { "string value" }, true)]
		[InlineData(new[] { "1", "true", "1", "string value", "1" }, nameof(TestApi.PositionalBindVerb5), nameof(TestApi.Param5Value), 1, true)]
		[InlineData(new[] { "1", "true", "1", "string value", "1", "another=str" }, nameof(TestApi.PositionalBindVerb6), nameof(TestApi.Param6), "another=str", true)]
		// invalid argument types
		[InlineData(new[] { "1", "0", "1", "a", "1", "a" }, nameof(TestApi.PositionalBindVerb6), nameof(TestApi.Param6), "", false)]
		[InlineData(new[] { "any", "0", "1", "a", "1", "a" }, nameof(TestApi.PositionalBindVerb6), nameof(TestApi.Param6), "", false)]
		[InlineData(new[] { "999999999999999999", "0", "1", "a", "1", "a" }, nameof(TestApi.PositionalBindVerb6), nameof(TestApi.Param6), "", false)]
		// binding intersection with named parameters
		[InlineData(new[] { "100500", "--param-1-int", "120" }, nameof(TestApi.TwoParamBindVerb), nameof(TestApi.Param1), 120, false)]
		// binding rest parameters
		[InlineData(new[] { "100500", "true", "120", "any", "extra", "params" }, nameof(TestApi.ExtraStringParamBindVerb), nameof(TestApi.ExtraStringsParam), new[] { "any", "extra", "params" }, true)]
		// binding typed values
		[InlineData(new[] { "100500", "1", "2", "3" }, nameof(TestApi.ExtraIntParamsBindVerb), nameof(TestApi.ExtraIntegersParam), new[] { 1, 2, 3 }, true)]
		// partial binding on required parameters
		[InlineData(new[] { "--param-1-int", "120" }, nameof(TestApi.TwoParamBindVerb), nameof(TestApi.Param1), 120, false)]
		[InlineData(new[] { "--param-2-int", "120" }, nameof(TestApi.TwoParamBindVerb), nameof(TestApi.Param2), 120, false)]
		public async Task BindValueTest(string[] arguments, string methodName, string propertyName, object propertyValue, bool success)
		{
			var testApi = new TestApi();
			var configuration = new CommandLineConfiguration();
			var typeConversionProvider = new TypeConversionProvider();
			var container = new ServiceContainer();
			container.AddService(testApi.GetType(), testApi);

			var verbSet = new VerbSet(typeof(TestApi).GetTypeInfo());
			var bind = new VerbBinder(configuration, typeConversionProvider, container);
			var bindingResult = bind.Bind(verbSet, methodName, arguments);

			this.output.WriteLine(bindingResult.ToString());

			if (success)
			{
				Assert.IsType<VerbBindingResult.Bound>(bindingResult);
			}
			else
			{
				return;
			}
			Assert.Equal(0, await ((VerbBindingResult.Bound)bindingResult).InvokeAsync());

			var expectedPropertyValue = testApi.GetType().GetField(propertyName)!.GetValue(testApi);

			if (propertyValue is IEnumerable enumExpected && expectedPropertyValue is IEnumerable actualPropertyValue &&
				propertyValue is string == false && expectedPropertyValue is string == false)
				Assert.Equal(enumExpected.Cast<object>(), actualPropertyValue.Cast<object>());
			else
				Assert.Equal(propertyValue, expectedPropertyValue);
		}

		// case insensitive parameter binding

		// bind command by name
		// bind command by case sensitive name
	}
}
