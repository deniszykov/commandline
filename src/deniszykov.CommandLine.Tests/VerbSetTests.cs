using System.Linq;
using System.Reflection;
using deniszykov.CommandLine.Annotations;
using deniszykov.CommandLine.Binding;
using Xunit;

namespace deniszykov.CommandLine.Tests
{
	public class VerbSetTests
	{
		public class VerbsApi
		{
			public static int Count => 0;
			public int Index { get; set; }

			public static int Verb() => 0;
			public static int GenericVerb<T>() => 0;
			[NonVerb]
			public static int Helper() => 0;

			public static VerbsApi operator +(VerbsApi a, VerbsApi b) => a;
			public static explicit operator int(VerbsApi a) => 0;
		}

		[Fact]
		public void OnlyNonGenericNonSpecialMethodsAreVerbsTest()
		{
			var verbSet = new VerbSet(typeof(VerbsApi).GetTypeInfo());

			Assert.Equal(new[] { "Verb" }, verbSet.Verbs.Select(verb => verb.Name).ToArray());
		}
	}
}
