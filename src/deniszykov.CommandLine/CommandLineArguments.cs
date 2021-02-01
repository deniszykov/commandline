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
using System.Text;
using deniszykov.TypeConversion;

// ReSharper disable UnusedMember.Global

namespace deniszykov.CommandLine
{
	/// <summary>
	/// Dictionary of command line arguments. Provides parsing/formatting features and by name access to parsed arguments.
	/// </summary>
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

		private readonly ITypeConversionProvider typeConversionProvider;

		/// <summary>
		/// Get positional argument.
		/// </summary>
		/// <param name="position">Zero-based position of argument.</param>
		/// <returns>Value of positional argument.</returns>
		public string this[int position]
		{
			get => this.typeConversionProvider.ConvertToString(this.GetValueOrDefault(position.ToString()));
			set
			{
				if (string.IsNullOrEmpty(value)) throw new ArgumentException("Value can't be empty or null.", nameof(value));
				this[position.ToString()] = value;
			}
		}

		/// <summary>
		/// Creates new empty instance of <see cref="CommandLineArguments"/>.
		/// </summary>
		public CommandLineArguments()
			: base(StringComparer.Ordinal)
		{

		}

		/// <summary>
		/// Creates new instance of <see cref="CommandLineArguments"/> with <paramref name="arguments"/>.
		/// </summary>
		public CommandLineArguments(ITypeConversionProvider typeConversionProvider, params string[] arguments)
			: this(typeConversionProvider, (IEnumerable<string>)arguments)
		{

		}
		/// <summary>
		/// Creates new instance of <see cref="CommandLineArguments"/> with <paramref name="arguments"/>.
		/// </summary>
		public CommandLineArguments(ITypeConversionProvider typeConversionProvider, IEnumerable<string> arguments)
			: this()
		{
			if (typeConversionProvider == null) throw new ArgumentNullException(nameof(typeConversionProvider));
			if (arguments == null) throw new ArgumentException("arguments");

			this.typeConversionProvider = typeConversionProvider;

			foreach (var kv in ParseArguments(arguments))
			{
				if (this.TryGetValue(kv.Key, out var currentValue))
				{
					if (kv.Value == null)
						continue;

					// try to combine with existing value
					if (currentValue == null)
						this[kv.Key] = currentValue = new List<string>();
					else if (currentValue is List<string> == false)
						this[kv.Key] = currentValue = new List<string> { this.typeConversionProvider.ConvertToString(currentValue) };

					var currentList = (List<string>)currentValue;
					if (kv.Value is List<string> list)
						currentList.AddRange(list);
					else
						currentList.Add(this.typeConversionProvider.ConvertToString(kv.Value));
				}
				else
				{
					this.Add(kv.Key, kv.Value);
				}
			}
		}
		/// <summary>
		/// Creates new instance of <see cref="CommandLineArguments"/> with <paramref name="argumentsDictionary"/>.
		/// </summary>
		public CommandLineArguments(ITypeConversionProvider typeConversionProvider, IDictionary<string, object> argumentsDictionary)
			: base(argumentsDictionary, StringComparer.Ordinal)
		{
			if (typeConversionProvider == null) throw new ArgumentNullException(nameof(typeConversionProvider));
			if (argumentsDictionary == null) throw new ArgumentException("argumentsDictionary");

			this.typeConversionProvider = typeConversionProvider;
		}

		/// <summary>
		/// Add new positional argument.
		/// </summary>
		public void Add(int position, string value)
		{
			if (position < 0) throw new ArgumentOutOfRangeException(nameof(position));
			if (string.IsNullOrEmpty(value)) throw new ArgumentException("Value can't be empty or null.", nameof(value));

			this.Add(position.ToString(), value);
		}
		/// <summary>
		/// Insert new positional argument and "push" other positional argument forward.
		/// </summary>
		public void InsertAt(int position, string value)
		{
			if (position < 0) throw new ArgumentOutOfRangeException(nameof(position));
			if (string.IsNullOrEmpty(value)) throw new ArgumentException("Value can't be empty or null.", nameof(value));

			var positionKey = position.ToString();
			var hasCurrent = this.TryGetValue(positionKey, out var currentValue) && string.IsNullOrEmpty(this.typeConversionProvider.ConvertToString(currentValue)) == false;
			this[positionKey] = value;

			while (hasCurrent)
			{
				value = this.typeConversionProvider.ConvertToString(currentValue);
				positionKey = (++position).ToString();
				hasCurrent = this.TryGetValue(positionKey, out currentValue) && string.IsNullOrEmpty(this.typeConversionProvider.ConvertToString(currentValue)) == false;
				this[positionKey] = value;
			}
		}
		/// <summary>
		/// Remove positional argument and "pull" other positional arguments backward.
		/// </summary>
		public void RemoveAt(int position)
		{
			if (position < 0) throw new ArgumentOutOfRangeException(nameof(position));

			var positionKey = position.ToString();
			this.Remove(positionKey);
			for (var i = position + 1; ; i++)
			{
				positionKey = i.ToString();
				if (this.TryGetValue(positionKey, out var value) == false)
					break;
				this[(i - 1).ToString()] = value;
				this.Remove(positionKey);
			}
		}
		/// <summary>
		/// Get value of positional argument by name or null if it is missing.
		/// </summary>
		public object GetValueOrDefault(string key)
		{
			return this.TryGetValue(key, out var value) == false ? null : value;
		}
		/// <summary>
		/// Get value of positional argument by name or <paramref name="defaultValue"/> if it is missing.
		/// </summary>
		public object GetValueOrDefault(string key, object defaultValue)
		{
			return this.TryGetValue(key, out var value) == false ? defaultValue : value;
		}

		/// <summary>
		/// Transform <see cref="CommandLineArguments"/> to list of string chunks. Chunks should be escaped(quoted) before combining them into one string.
		/// </summary>
		public string[] ToArray()
		{
			var count = 0;
			foreach (var kv in this)
			{
				if (!int.TryParse(kv.Key, out _))
				{
					if (kv.Value == null)
						count++; // only parameter name
					else if (kv.Value is IList<string> list)
						count += 1 + list.Count; // parameter name + values
					else
						count += 2; // parameter name + value
				}
				else
				{
					if (kv.Value is IList<string> list)
						count += list.Count; // only values
					else if (kv.Value != null)
						count += 1; // only value
				}
			}

			var array = new string[count];
			var index = 0;
			foreach (var kv in this.OrderBy(kv => kv.Key, IntAsStringComparer.Default))
			{
				var valuesList = kv.Value as IList<string>;

				if (!int.TryParse(kv.Key, out _))
					array[index++] = string.Concat(CommandLine.ArgumentNamePrefix, kv.Key);

				if (valuesList != null)
				{
					valuesList.CopyTo(array, index);
					index += valuesList.Count;
				}
				else if (kv.Value != null)
				{
					array[index++] = this.typeConversionProvider.ConvertToString(kv.Value);
				}
			}

			return array;
		}

		/// <summary>
		/// Parse list of arguments into <see cref="CommandLineArguments"/>. Chunks should be un-escaped(un-quoted).
		/// </summary>
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
					throw new InvalidOperationException(
						$"Argument name should start with '{CommandLine.ArgumentNamePrefix}' or '{CommandLine.ArgumentNamePrefixShort}' symbols. " +
						"This is arguments parser error and should be reported to application developer.");

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

		/// <summary>
		/// Transform <see cref="CommandLineArguments"/> via <see cref="ToArray"/> into string chunks and apply basic escaping and joining with SPACE character.
		/// </summary>
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
