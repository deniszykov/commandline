/*
	Copyright (c) 2016 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

// ReSharper disable once CheckNamespace
namespace System
{
	[Serializable]
	public class CommandLineArguments : Dictionary<string, object>
	{
		public CommandLineArguments()
			: base(StringComparer.Ordinal)
		{

		}
		public CommandLineArguments(IEnumerable<string> arguments)
			: this()
		{
			if (arguments == null) throw new ArgumentException("arguments");

			foreach (var kv in ParseArguments(arguments))
				this.Add(kv.Key, kv.Value);
		}
		public CommandLineArguments(IDictionary<string, object> argumentsDictionary)
			: base(argumentsDictionary, StringComparer.Ordinal)
		{
			if (argumentsDictionary == null) throw new ArgumentException("argumentsDictionary");
		}
		protected CommandLineArguments(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{

		}

		public object GetValueOrDefault(string key, object defaultValue = null)
		{
			var value = defaultValue;
			return this.TryGetValue(key, out value) == false ? defaultValue : value;
		}

		public string[] ToArray()
		{
			var count = 0;
			var position = 0;
			foreach (var kv in this)
			{
				if (!int.TryParse(kv.Key, out position))
				{
					if (kv.Value == null)
						count++;
					else if (kv.Value is string)
						count += 2;
					else if (kv.Value is string[])
						count += 1 + ((string[])kv.Value).Length;
				}
				else
					count++;
			}
			var array = new string[count];
			var index = 0;
			foreach (var kv in this.OrderByDescending(kv => kv.Key))
			{
				if (!int.TryParse(kv.Key, out position))
				{
					array[index++] = string.Concat(CommandLine.ArgumentNamePrefix, kv.Key);

					var valueString = kv.Value as string;
					var valueArray = kv.Value as string[];
					if (valueString != null)
					{
						array[index++] = valueString;
					}
					else if (valueArray != null)
					{
						Array.Copy(valueArray, 0, array, index, valueArray.Length);
						index += valueArray.Length;
					}
				}
				else
				{
					array[index++] = kv.Key;
				}
			}

			return array;
		}

		private static IEnumerable<KeyValuePair<string, object>> ParseArguments(IEnumerable<string> arguments)
		{
			if (arguments == null) throw new ArgumentException("arguments");

			var positional = true;
			var position = -1;
			var argumentName = default(string);
			var argumentValue = default(List<string>);
			foreach (var argument in arguments)
			{
				position++;

				if (string.IsNullOrEmpty(argument) ||
					(argument.StartsWith(CommandLine.ArgumentNamePrefix, StringComparison.Ordinal) == false &&
						argument.StartsWith(CommandLine.ArgumentNamePrefixShort, StringComparison.Ordinal) == false))
				{
					if (positional)
						yield return new KeyValuePair<string, object>(position.ToString(), argument);
					else if (argumentValue == null)
						argumentValue = new List<string> { argument };
					else
						argumentValue.Add(argument);

					continue;
				}

				positional = false;

				if (argumentName != null)
					yield return new KeyValuePair<string, object>(argumentName, argumentValue != null ? argumentValue.ToArray() : null);

				if (argument.StartsWith(CommandLine.ArgumentNamePrefix, StringComparison.Ordinal))
					argumentName = argument.Substring(CommandLine.ArgumentNamePrefix.Length);
				else if (argument.StartsWith(CommandLine.ArgumentNamePrefixShort))
					argumentName = argument.Substring(CommandLine.ArgumentNamePrefixShort.Length);
			}
		}

		public override string ToString()
		{
			var arguments = this.ToArray();
			var length = arguments.Sum(a => a.Length) + arguments.Length * 3;
			var sb = new StringBuilder(length);
			foreach (var value in arguments)
			{
				var start = sb.Length;
				sb.Append(value);

				sb.Replace("\\", "\\\\", start, sb.Length - start);
				sb.Replace("\"", "\\\"", start, sb.Length - start);

				if (value != null && value.IndexOf(' ') != -1)
					sb.Insert('"', start).Append('"');

				sb.Append(' ');
			}

			if (sb.Length > 0)
				sb.Length--;

			return sb.ToString();
		}
	}
}
