/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using deniszykov.CommandLine.Binding;

namespace deniszykov.CommandLine
{
	/// <summary>
	/// Exception occurred while executing command during <see cref="CommandLine.Run"/>.
	/// There is extra information in <see cref="Exception.Data"/> dictionary under "method", "methodToken" and "bindingErrors" keys.
	/// </summary>
#if !NETSTANDARD1_6
	[Serializable]
#endif
	public sealed class CommandLineException : Exception
	{
		/// <summary>
		/// Create new instance of <see cref="CommandLineException"/>.
		/// </summary>
		public CommandLineException(string message) : base(message)
		{
		}

		/// <summary>
		/// Create new instance of <see cref="CommandLineException"/>.
		/// </summary>
		public CommandLineException(string message, Exception innerException) : base(message, innerException)
		{
		}
#if !NETSTANDARD1_6
		/// <summary>
		/// Create new instance of <see cref="CommandLineException"/>.
		/// </summary>
		private CommandLineException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
		{
		}
#endif

		internal static CommandLineException CommandNotFound(string commandName)
		{
			if (commandName == null) throw new ArgumentNullException(nameof(commandName));

			return new CommandLineException($"Command '{commandName}' is not found.");
		}
		internal static CommandLineException InvalidCommandParameters(Command command, ParameterBindingResult[] parameterBindResults)
		{
			if (command == null) throw new ArgumentNullException(nameof(command));
			if (parameterBindResults == null) throw new ArgumentNullException(nameof(parameterBindResults));

			var bindingErrors = new Dictionary<string, Exception>();
			var builder = new StringBuilder();
			builder.AppendFormat("Invalid parameters for command {0}(", command.Name);
			foreach (var parameterBindResult in parameterBindResults)
			{
				if (parameterBindResult.Parameter.IsOptional)
					builder.Append("[");
				builder.Append(parameterBindResult.Parameter.Name);
				if (parameterBindResult.Parameter.IsOptional)
					builder.Append("]");
				var isLast = parameterBindResult == parameterBindResults[parameterBindResults.Length - 1];
				if (isLast)
					builder.Append(")");
				else
					builder.Append(", ");
			}
			builder.AppendLine(":");
			foreach (var parameterBindResult in parameterBindResults)
			{
				builder.Append("\t").Append(parameterBindResult.Parameter.Name).Append(": ");
				if (parameterBindResult.IsSuccess)
				{
					var value = Convert.ToString(parameterBindResult.Value, CultureInfo.InvariantCulture) ?? "<null>";
					if (value.Length > 32)
						builder.Append(value.Substring(0, 32)).AppendLine("...");
					else
						builder.AppendLine(value);
				}
				else
				{
					bindingErrors[parameterBindResult.Parameter.Name] = parameterBindResult.Error;

					var errorMessage = parameterBindResult.Error.Message;
					var parameterType = parameterBindResult.Parameter.ValueType;
					parameterType = Nullable.GetUnderlyingType(parameterType.AsType())?.GetTypeInfo() ?? parameterType;
					builder.Append("(").Append(parameterType.Name).Append(") ");
					if (parameterBindResult.Error is FormatException && parameterBindResult.Value != null)
					{
						var value = Convert.ToString(parameterBindResult.Value, CultureInfo.InvariantCulture) ?? "<null>";
						builder.Append(errorMessage)
							.Append(" Value: '");
						if (value.Length > 32)
							builder.Append(value.Substring(0, 32)).Append("...");
						else
							builder.Append(value);
						builder.AppendLine("'.");
					}
					else
					{
						builder.AppendLine(errorMessage);
					}
				}
			}

			var error = new CommandLineException(builder.ToString());
			error.Data["method"] = command.Name;
			error.Data["bindingErrors"] = bindingErrors;
			return error;
		}
		internal static CommandLineException UnableToResolveService(Command command, Type serviceType)
		{
			if (command == null) throw new ArgumentNullException(nameof(command));
			if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

			return new CommandLineException($"Unable to resolve required service of type '{serviceType}' for '{command.Name}' command.");
		}
		internal static CommandLineException RecursiveCommandChain(IEnumerable<string> currentChain, string newCommand)
		{
			if (currentChain == null) throw new ArgumentNullException(nameof(currentChain));
			if (newCommand == null) throw new ArgumentNullException(nameof(newCommand));

			return new CommandLineException($"Calling '{newCommand}' cause possible infinite recursion in chain '{string.Join("' -> '", currentChain)}'.");
		}
	}
}
