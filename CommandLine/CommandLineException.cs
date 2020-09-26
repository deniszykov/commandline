﻿/*
	Copyright (c) 2020 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/
using System.Collections.Generic;
using System.Reflection;
using System.Text;

// ReSharper disable once CheckNamespace
namespace System
{
	/// <summary>
	/// Exception occurred while executing command during <see cref="CommandLine.Run"/>.
	/// There is extra information in <see cref="Exception.Data"/> dictionary under "method", "methodToken" and "bindingErrors" keys.
	/// </summary>
#if !NETSTANDARD1_3
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
#if !NETSTANDARD1_3
		/// <summary>
		/// Create new instance of <see cref="CommandLineException"/>.
		/// </summary>
		private CommandLineException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
		{
		}
#endif

		internal static CommandLineException CommandNotFound(string commandName)
		{
			if (commandName == null) throw new ArgumentNullException("commandName");

			return new CommandLineException(string.Format("Command '{0}' is not found.", commandName));
		}
		internal static CommandLineException InvalidCommandParameters(MethodInfo commandMethod, ParameterBindResult[] parameterBindResults)
		{
			if (commandMethod == null) throw new ArgumentNullException("commandMethod");
			if (parameterBindResults == null) throw new ArgumentNullException("parameterBindResults");

			var bindingErrors = new Dictionary<string, Exception>();
			var builder = new StringBuilder();
			builder.AppendFormat("Invalid parameters for command {0}(", commandMethod.Name);
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
					var value = TypeConvert.ToString(parameterBindResult.Value) ?? "<null>";
					if (value.Length > 32)
						builder.Append(value.Substring(0, 32)).AppendLine("...");
					else
						builder.AppendLine(value);
				}
				else
				{
					bindingErrors[parameterBindResult.Parameter.Name] = parameterBindResult.Error;

					var errorMessage = parameterBindResult.Error.Message;
					var parameterType = parameterBindResult.Parameter.ParameterType;
					parameterType = Nullable.GetUnderlyingType(parameterType) ?? parameterType;
					builder.Append("(").Append(parameterType.Name).Append(") ");
					if (parameterBindResult.Error is FormatException && parameterBindResult.Value != null)
					{
						var value = TypeConvert.ToString(parameterBindResult.Value) ?? "<null>";
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
			error.Data["method"] = commandMethod.Name;
#if !NETSTANDARD1_3
			error.Data["methodToken"] = commandMethod.MetadataToken;
#endif
			error.Data["bindingErrors"] = bindingErrors;
			return error;
		}
	}
}
