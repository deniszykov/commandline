/*
	Copyright (c) 2016 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System.Collections.Generic;
using System.Linq;
using System.Text;

// ReSharper disable once CheckNamespace
namespace System
{
#if !NETSTANDARD13
	[Serializable]
#endif
	public class CommandLineArguments : Dictionary<string, object>
	{
		private class IntAsStringComparer : IComparer<string>
		{
			public static readonly IntAsStringComparer Default = new IntAsStringComparer();

			public int Compare(string x, string y)
			{
				int xInt = 0, yInt = 0;
				bool xIsInt = false, yIsInt = false;
				if (x != null && x.All(char.IsDigit) && int.TryParse(x, out xInt))
					xIsInt = true;
				if (y != null && y.All(char.IsDigit) && int.TryParse(y, out yInt))
					yIsInt = true;

				if (xIsInt && yIsInt)
					return xInt.CompareTo(yInt);
				else if (xIsInt)
					return -1;
				else if (yIsInt)
					return 1;
				else
					return StringComparer.Ordinal.Compare(x ?? string.Empty, y ?? string.Empty);
			}
		}

		public string this[int position]
		{
			get
			{
				return TypeConvert.ToString(this.GetValueOrDefault(position.ToString()));
			}
			set
			{
				if (string.IsNullOrEmpty(value)) throw new ArgumentException("Value can't be empty or null.", "value");
				this[position.ToString()] = value;
			}
		}

		public CommandLineArguments()
			: base(StringComparer.Ordinal)
		{

		}
		public CommandLineArguments(params string[] arguments)
			: this((IEnumerable<string>)arguments)
		{

		}
		public CommandLineArguments(IEnumerable<string> arguments)
			: this()
		{
			if (arguments == null) throw new ArgumentException("arguments");

			foreach (var kv in ParseArguments(arguments))
			{
				var currentValue = default(object);
				if (this.TryGetValue(kv.Key, out currentValue))
				{
					if (kv.Value == null)
						continue;

					// try to combine with existing value
					if (currentValue == null)
						this[kv.Key] = currentValue = new List<string>();
					else if (currentValue is List<string> == false)
						this[kv.Key] = currentValue = new List<string> { TypeConvert.ToString(currentValue) };

					var currentList = (List<string>)currentValue;
					if (kv.Value is List<string>)
						currentList.AddRange((List<string>)kv.Value);
					else
						currentList.Add(TypeConvert.ToString(kv.Value));
				}
				else
				{
					this.Add(kv.Key, kv.Value);
				}
			}
		}
		public CommandLineArguments(IDictionary<string, object> argumentsDictionary)
			: base(argumentsDictionary, StringComparer.Ordinal)
		{
			if (argumentsDictionary == null) throw new ArgumentException("argumentsDictionary");
		}
#if !NETSTANDARD13
		protected CommandLineArguments(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
			: base(info, context)
		{

		}
#endif
		public void Add(int position, string value)
		{
			if (position < 0) throw new ArgumentOutOfRangeException("position");
			if (string.IsNullOrEmpty(value)) throw new ArgumentException("Value can't be empty or null.", "value");

			this.Add(position.ToString(), value);
		}
		public void InsertAt(int position, string value)
		{
			if (position < 0) throw new ArgumentOutOfRangeException("position");
			if (string.IsNullOrEmpty(value)) throw new ArgumentException("Value can't be empty or null.", "value");

			var currentValue = default(object);
			var positionKey = position.ToString();
			var hasCurrent = this.TryGetValue(positionKey, out currentValue) && string.IsNullOrEmpty(TypeConvert.ToString(currentValue)) == false;
			this[positionKey] = value;

			while (hasCurrent)
			{
				value = TypeConvert.ToString(currentValue);
				positionKey = (++position).ToString();
				hasCurrent = this.TryGetValue(positionKey, out currentValue) && string.IsNullOrEmpty(TypeConvert.ToString(currentValue)) == false;
				this[positionKey] = value;
			}
		}
		public void RemoveAt(int position)
		{
			if (position < 0) throw new ArgumentOutOfRangeException("position");

			var positionKey = position.ToString();
			this.Remove(positionKey);
			for (var i = position + 1; ; i++)
			{
				var value = default(object);
				positionKey = i.ToString();
				if (this.TryGetValue(positionKey, out value) == false)
					break;
				this[(i - 1).ToString()] = value;
				this.Remove(positionKey);
			}
		}

		public object GetValueOrDefault(string key)
		{
			var value = default(object);
			return this.TryGetValue(key, out value) == false ? null : value;
		}
		public object GetValueOrDefault(string key, object defaultValue)
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
						count++; // only parameter name
					else if (kv.Value is IList<string>)
						count += 1 + ((IList<string>)kv.Value).Count; // parameter name + values
					else
						count += 2; // parameter name + value
				}
				else
				{
					if (kv.Value is IList<string>)
						count += ((IList<string>)kv.Value).Count; // only values
					else if (kv.Value != null)
						count += 1; // only value
				}
			}

			var array = new string[count];
			var index = 0;
			foreach (var kv in this.OrderBy(kv => kv.Key, IntAsStringComparer.Default))
			{
				var valuesList = kv.Value as IList<string>;

				if (!int.TryParse(kv.Key, out position))
					array[index++] = string.Concat(CommandLine.ArgumentNamePrefix, kv.Key);

				if (valuesList != null)
				{
					valuesList.CopyTo(array, index);
					index += valuesList.Count;
				}
				else if (kv.Value != null)
				{
					array[index++] = TypeConvert.ToString(kv.Value);
				}
			}

			return array;
		}

		private static IEnumerable<KeyValuePair<string, object>> ParseArguments(IEnumerable<string> arguments)
		{
			if (arguments == null) throw new ArgumentException("arguments");

			var positional = true;
			var forcePositional = false;
			var noHyphenParameters = false;
			var position = -1;
			var argumentName = default(string);
			var argumentValue = default(List<string>);
			foreach (var argument in arguments)
			{
				if (forcePositional || IsArgumentName(argument, noHyphenParameters) == false)
				{
					if (positional || forcePositional)
						yield return new KeyValuePair<string, object>((++position).ToString(), argument);
					else if (argumentValue == null)
						argumentValue = new List<string> { argument };
					else
						argumentValue.Add(argument);

					continue;
				}


				// save previous argument
				if (argumentName != null)
					yield return new KeyValuePair<string, object>(argumentName, GetNullOrFirstOrAll(argumentValue));

				argumentValue = null;
				argumentName = null;

				if (argument == CommandLine.ArgumentNamePrefix)
				{
					forcePositional = true;
					continue;
				}
				else if (argument == CommandLine.ArgumentNamePrefixShort)
				{
					noHyphenParameters = !noHyphenParameters;
					continue;
				}

				if (argument.StartsWith(CommandLine.ArgumentNamePrefix, StringComparison.Ordinal))
					argumentName = argument.Substring(CommandLine.ArgumentNamePrefix.Length);
				else if (argument.StartsWith(CommandLine.ArgumentNamePrefixShort))
					argumentName = argument.Substring(CommandLine.ArgumentNamePrefixShort.Length);
				else
					throw new InvalidOperationException(string.Format("Argument name should start with '{0}' or '{1}' symbols. " +
						"This is arguments parser error and should be reported to application developer.", CommandLine.ArgumentNamePrefix, CommandLine.ArgumentNamePrefixShort));

				positional = false;
			}

			if (!string.IsNullOrEmpty(argumentName))
				yield return new KeyValuePair<string, object>(argumentName, GetNullOrFirstOrAll(argumentValue));
		}

		private static object GetNullOrFirstOrAll(List<string> argumentValue)
		{
			if (argumentValue == null)
				return null;
			if (argumentValue.Count == 1)
				return argumentValue[0];
			return argumentValue.ToArray();
		}
		private static bool IsStartsAsNumber(string value)
		{
			if (string.IsNullOrEmpty(value))
				return false;

			if (value.Length > 1 && value[0] == '-' && char.IsDigit(value[1]))
				return true;
			else if (value.Length > 0)
				return char.IsDigit(value[0]);
			else
				return false;
		}
		private static bool IsArgumentName(string value, bool noHyphenParameters)
		{
			if (string.IsNullOrEmpty(value))
				return false;

			if (IsStartsAsNumber(value))
				return false;

			return value.StartsWith(CommandLine.ArgumentNamePrefix, StringComparison.Ordinal) ||
				(noHyphenParameters == false && value.StartsWith(CommandLine.ArgumentNamePrefixShort, StringComparison.Ordinal));
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
