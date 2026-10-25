using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace deniszykov.CommandLine.Tests
{
	public class CommandLineConfigurationTests
	{
		[Fact]
		public void CopyToCopiesAllSettingsTest()
		{
			var source = new CommandLineConfiguration();
			var properties = typeof(CommandLineConfiguration).GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(property => property.CanRead && property.CanWrite)
				.ToList();
			foreach (var property in properties)
			{
				property.SetValue(source, CreateNonDefaultValue(property.PropertyType, property.GetValue(source)));
			}

			var target = new CommandLineConfiguration();
			source.CopyTo(target);

			foreach (var property in properties)
			{
				Assert.True(Equals(property.GetValue(source), property.GetValue(target)), $"'{property.Name}' is not copied.");
			}
		}

		private static object CreateNonDefaultValue(Type type, object? currentValue)
		{
			if (type == typeof(bool)) return !(bool)currentValue!;
			if (type == typeof(int)) return (int)currentValue! + 100;
			if (type == typeof(string)) return "non-default";
			if (type == typeof(StringComparison)) return (StringComparison)currentValue! == StringComparison.CurrentCulture ? StringComparison.InvariantCulture : StringComparison.CurrentCulture;
			if (type == typeof(string[])) return new[] { "non-default" };
			if (type == typeof(char[])) return new[] { ':' };
			if (type == typeof(ExceptionEventHandler)) return new ExceptionEventHandler((sender, args) => { });
			throw new InvalidOperationException($"Add non-default value for type '{type}' to this test.");
		}
	}
}
