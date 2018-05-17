/*
	Copyright (c) 2017 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

// ReSharper disable once CheckNamespace
namespace System
{
	public static class CommandLine
	{
		public const int DotNetExceptionExitCode = -2147023895;
		public const string ArgumentNamePrefix = "--";
		public const string ArgumentNamePrefixShort = "-";
		public const string UnknownMethodName = "<no name specified>";

#if !NETSTANDARD13
		private static CommandLineArguments applicationArguments;
		/// <summary>
		/// Lazy initialized application startup parameters. Executable name as first parameter is substituted.
		/// </summary>
		public static CommandLineArguments Arguments
		{
			get
			{
				if (applicationArguments == null)
					applicationArguments = new CommandLineArguments(Environment.GetCommandLineArgs().Skip(1));
				return applicationArguments;
			}
		}
#endif
		/// <summary>
		/// Exception handling for <see cref="Run"/> parameterInfo.
		/// </summary>
		public static event ExceptionEventHandler UnhandledException;
		/// <summary>
		/// Try to describe API into default output when bind error occurs (command name mistype or wrong arguments).
		/// </summary>
		public static bool DescribeOnBindFailure = true;
		/// <summary>
		/// Output whole error message to stderr when bind error occurs (command name mistype or wrong arguments).
		/// </summary>
		public static bool WriteWholeErrorMessageOnBindFailure = false;
		/// <summary>
		/// Exit code returned when unable to find command and help text is displayed instead.
		/// </summary>
		public static int BindFailureExitCode = 1;
		/// <summary>
		/// Exit code used by Describe method as return value.
		/// </summary>
		public static int DescribeExitCode = 0;

		public static int Run<T>(CommandLineArguments arguments, string defaultCommandName = null)
		{
			if (arguments == null) throw new ArgumentNullException("arguments");

			return Run(typeof(T), arguments, defaultCommandName);
		}
		public static int Run(Type type, CommandLineArguments arguments, string defaultCommandName = null)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (arguments == null) throw new ArgumentNullException("arguments");

			try
			{
				var bindResult = BindMethod(type, defaultCommandName, arguments);

				if (bindResult.IsSuccess)
				{
					return (int)bindResult.Method.Invoke(null, bindResult.Arguments);
				}
				else
				{
					var bestMatchMethod = (from bindingKeyValue in bindResult.FailedMethodBindings
										   let parameters = bindingKeyValue.Value
										   let method = bindingKeyValue.Key
										   orderby parameters.Count(parameter => parameter.IsSuccess) descending
										   select method).FirstOrDefault();

					var error = default(CommandLineException);
					if (bestMatchMethod == null)
					{
						error = CommandLineException.CommandNotFound(bindResult.MethodName);
					}
					else
					{
						error = CommandLineException.InvalidCommandParameters(bestMatchMethod, bindResult.FailedMethodBindings[bestMatchMethod]);
					}

					if (DescribeOnBindFailure)
					{
						if (bindResult.MethodName == UnknownMethodName)
						{
							Console.WriteLine(" Error:");
							Console.WriteLine("  No command is specified.");
							Console.WriteLine();

							if (WriteWholeErrorMessageOnBindFailure)
							{
								Console.Error.WriteLine(error);
								Console.Error.WriteLine();
							}

							Describe(type);
						}
						else
						{
							Console.WriteLine(" Error:");
							Console.WriteLine(string.Format("  Invalid parameters specified for '{0}' command.", bindResult.MethodName));
							Console.WriteLine();

							if (WriteWholeErrorMessageOnBindFailure)
							{
								Console.Error.WriteLine(error);
								Console.Error.WriteLine();
							}

							Describe(type, bindResult.MethodName);
						}
					}
					else
					{
						throw error;
					}
				}

				return BindFailureExitCode;
			}
			catch (Exception exception)
			{
				var handler = UnhandledException ?? DefaultUnhandledExceptionHandler;
				handler(null, new ExceptionEventArgs(exception));

				return DotNetExceptionExitCode;
			}
		}
		public static int Describe<T>(string commandToDescribe = null)
		{
			var type = typeof(T);
			return Describe(type, commandToDescribe);
		}
		public static int Describe(Type type, string commandToDescribe = null)
		{
			if (type == null) throw new ArgumentNullException("type");

			if (string.IsNullOrEmpty(commandToDescribe) == false)
			{
				Console.WriteLine(GetCommandDescription(type, commandToDescribe));
			}
			else
			{
				Console.WriteLine(GetTypeDescription(type, includeTypeHelpText: true));
			}

			return DescribeExitCode;
		}
		private static string GetTypeDescription(Type type, bool includeTypeHelpText)
		{
			if (type == null) throw new ArgumentNullException("type");

			var result = new StringBuilder();
			var basePadding = 1;

			if (includeTypeHelpText)
			{
				var descriptionText = default(string);
				var helpText = type.GetTypeInfo().GetCustomAttributes(typeof(HelpTextAttribute), true).Cast<HelpTextAttribute>().FirstOrDefault();
				if (helpText != null)
				{
					descriptionText = helpText.Description;
				}
#if !NETSTANDARD13
				var description = type.GetCustomAttributes(typeof(DescriptionAttribute), true).Cast<DescriptionAttribute>().FirstOrDefault();
				if (description != null)
				{
					descriptionText = descriptionText ?? description.Description;
				}
#endif
				result.Append(' ', basePadding).AppendLine(descriptionText).AppendLine();
			}
			result.Append(' ', basePadding).AppendLine("Commands:");

			foreach (var method in type.GetTypeInfo().GetAllMethods().Where(IsValidCommandSignature))
			{
				if (IsHidden(method))
					continue;

				result.AppendFormat("  {0} ", method.Name.ToUpper());

				var methodDescriptionText = default(string);
				var methodHelpText = method.GetCustomAttributes(typeof(HelpTextAttribute), true).Cast<HelpTextAttribute>().FirstOrDefault();
				if (methodHelpText != null)
					methodDescriptionText = methodHelpText.Description;
#if !NETSTANDARD13
				var methodDescription = method.GetCustomAttributes(typeof(DescriptionAttribute), true).Cast<DescriptionAttribute>().FirstOrDefault();
				if (methodDescription != null)
					methodDescriptionText = methodDescriptionText ?? methodDescription.Description;
#endif
				if (string.IsNullOrEmpty(methodDescriptionText) == false)
					AppendLinePadded(result, methodDescriptionText);
				else
					result.AppendLine();
			}

			return result.ToString();
		}
		private static string GetCommandDescription(Type type, string commandName)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (commandName == null) throw new ArgumentNullException("commandName");

			var result = new StringBuilder();
			var basePadding = 1;

			foreach (var method in type.GetTypeInfo().GetAllMethods().Where(IsValidCommandSignature))
			{
				if (string.Equals(method.Name, commandName, StringComparison.OrdinalIgnoreCase) == false)
					continue;

				if (IsHidden(method))
					continue;

				result.Append(' ', basePadding).AppendLine("Command:");
				result.Append(' ', basePadding + 1).Append(method.Name.ToUpper()).Append(" ");

				var paramsArr = method.GetParameters().Where(p => !IsHidden(p)).Select(parameter => string.Format(parameter.IsOptional ? "[--{0} <{1}>] " : "--{0} <{1}> ", parameter.Name, GetParameterTypeFriendlyName(parameter))).ToArray();
				var paramsDesc = string.Concat(paramsArr);
				AppendLinePadded(result, paramsDesc);
				result.AppendLine();

				var methodDescriptionText = default(string);
				var methodHelpText = method.GetCustomAttributes(typeof(HelpTextAttribute), true).Cast<HelpTextAttribute>().FirstOrDefault();
				if (methodHelpText != null)
					methodDescriptionText = methodHelpText.Description;
#if !NETSTANDARD13
				var methodDescription = method.GetCustomAttributes(typeof(DescriptionAttribute), true).Cast<DescriptionAttribute>().FirstOrDefault();
				if (methodDescription != null)
					methodDescriptionText = methodDescriptionText ?? methodDescription.Description;
#endif
				methodDescriptionText = (methodDescriptionText ?? string.Empty).Trim();

				if (string.IsNullOrEmpty(methodDescriptionText) == false)
				{
					result.Append(' ', basePadding).AppendLine("Description:");
					result.Append(' ', basePadding + 1);

					if (methodDescriptionText.EndsWith(".", StringComparison.OrdinalIgnoreCase) == false)
						methodDescriptionText += ".";

					AppendLinePadded(result, methodDescriptionText);
				}
				else
				{
					result.AppendLine();
				}

				result.AppendLine();
				result.Append(' ', basePadding).AppendLine("Parameters:");
				foreach (var parameter in method.GetParameters())
				{
					if (IsHidden(parameter))
						continue;

					if (parameter.ParameterType == typeof(CommandLineArguments))
						continue;

					var parameterType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
					var parameterTypeName = parameterType.Name;
					var options = "";
					var defaultValue = "";
					if (parameterType.GetTypeInfo().IsEnum) options = " Options are " + string.Join(", ", Enum.GetNames(parameterType)) + ".";
					if (parameter.IsOptional && parameter.DefaultValue != null) defaultValue = " Default value is '" + TypeConvert.Convert(parameter.DefaultValue.GetType(), parameterType, parameter.DefaultValue) + "'.";

					var paramDescriptionText = default(string);
					var paramHelpText = parameter.GetCustomAttributes(typeof(HelpTextAttribute), true).Cast<HelpTextAttribute>().FirstOrDefault();
					if (paramHelpText != null)
						paramDescriptionText = paramHelpText.Description;
#if !NETSTANDARD13
					var paramDescription = parameter.GetCustomAttributes(typeof(DescriptionAttribute), true).Cast<DescriptionAttribute>().FirstOrDefault();
					if (paramDescription != null)
						paramDescriptionText = paramDescriptionText ?? paramDescription.Description;
#endif

					paramDescriptionText = (paramDescriptionText ?? string.Empty).Trim();

					if (paramDescriptionText.Length > 0 && paramDescriptionText.EndsWith(".", StringComparison.OrdinalIgnoreCase) == false)
						paramDescriptionText += ".";

					result.Append(' ', basePadding + 2);
					result.Append(parameter.Name.ToUpperInvariant()).Append(" ");
					AppendLinePadded(result, string.Format("({0}) {1}{2}{3}", parameterTypeName, paramDescriptionText ?? string.Empty, defaultValue, options));
				}
			}

			return result.ToString();
		}

		private static void DefaultUnhandledExceptionHandler(object source, ExceptionEventArgs eventArgs)
		{
			Console.Error.WriteLine(eventArgs.Exception);
		}
		private static MethodBindResult BindMethod(Type type, string defaultMethodName, CommandLineArguments arguments)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (arguments == null) throw new ArgumentNullException("arguments");

			if (string.IsNullOrEmpty(defaultMethodName))
			{
				defaultMethodName = null;
			}

			var allMethods = type.GetTypeInfo().GetAllMethods().Where(IsValidCommandSignature).ToList();
			allMethods.Sort((x, y) => y.GetParameters().Length.CompareTo(x.GetParameters().Length));

			var methodName = default(string);
			var bestMatchResult = default(MethodBindResult);
			if (arguments.ContainsKey("0"))
			{
				var newArguments = new CommandLineArguments(arguments);
				methodName = TypeConvert.ToString(newArguments["0"]);
				newArguments.RemoveAt(0);
				var result = BindMethod(allMethods, methodName, newArguments);
				if (result.IsSuccess)
					return result;

				bestMatchResult = result;
			}

			if (string.IsNullOrEmpty(defaultMethodName) == false)
			{
				methodName = defaultMethodName;
				var result = BindMethod(allMethods, defaultMethodName, arguments);
				if (result.IsSuccess)
					return result;

				bestMatchResult = bestMatchResult ?? result;
			}

			if (string.IsNullOrEmpty(methodName))
			{
				methodName = UnknownMethodName;
			}

			bestMatchResult = bestMatchResult ?? new MethodBindResult(methodName, MethodBindResult.EmptyFailedMethodBindings);

			return bestMatchResult;
		}
		private static MethodBindResult BindMethod(List<MethodInfo> methods, string methodName, CommandLineArguments arguments)
		{
			if (methods == null) throw new ArgumentNullException("methods");
			if (methodName == null) throw new ArgumentNullException("methodName");
			if (arguments == null) throw new ArgumentNullException("arguments");

			var failedMethods = new Dictionary<MethodInfo, ParameterBindResult[]>();
			foreach (var method in methods)
			{
				if (string.Equals(method.Name, methodName, StringComparison.OrdinalIgnoreCase) == false ||
					!method.IsStatic ||
					method.ReturnType != typeof(int) ||
					IsHidden(method))
					continue;

				var parameters = method.GetParameters();
				var parameterBindings = new ParameterBindResult[parameters.Length];
				var isSuccessfulBinding = true;
				foreach (var parameter in parameters)
				{
					var bindResult = BindParameter(parameter, arguments);
					parameterBindings[parameter.Position] = bindResult;
					isSuccessfulBinding = isSuccessfulBinding && bindResult.IsSuccess;
				}

				if (isSuccessfulBinding == false)
				{
					failedMethods.Add(method, parameterBindings);
					continue;
				}

				var methodArguments = new object[parameters.Length];
				for (var i = 0; i < parameters.Length; i++)
					methodArguments[i] = parameterBindings[i].Value;

				return new MethodBindResult(method, methodArguments);
			}

			return new MethodBindResult(methodName, failedMethods);
		}
		private static ParameterBindResult BindParameter(ParameterInfo parameter, CommandLineArguments arguments)
		{
			var expectedType = parameter.ParameterType;
			var value = default(object);
			try
			{
				if (expectedType == typeof(CommandLineArguments))
				{
					value = new CommandLineArguments(arguments);
				}
				else if (arguments.TryGetValue(parameter.Name, out value) || arguments.TryGetValue((parameter.Position).ToString(), out value))
				{
					if (expectedType.IsArray)
					{
						var elemType = expectedType.GetElementType();
						Debug.Assert(elemType != null, "elemType != null");
						if (value is IList<string>)
						{
							var valuesStr = (IList<string>)value;
							var values = Array.CreateInstance(elemType, valuesStr.Count);
							for (var i = 0; i < valuesStr.Count; i++)
							{
								value = valuesStr[i]; // used on failure in exception block
								values.SetValue(TypeConvert.Convert(typeof(string), elemType, valuesStr[i]), i);
							}
							value = values;
						}
						else if (value != null)
						{
							var values = Array.CreateInstance(elemType, 1);
							values.SetValue(TypeConvert.Convert(value.GetType(), elemType, value), 0);
							value = values;
						}
						else
						{
							var values = Array.CreateInstance(elemType, 0);
							value = values;
						}
					}
					else if (expectedType == typeof(bool) && value == null)
					{
						value = true;
					}
					else if (value == null)
					{
						value = parameter.IsOptional ? parameter.DefaultValue : expectedType.GetTypeInfo().IsValueType ? TypeActivator.CreateInstance(expectedType) : null;
					}
					else if (expectedType.GetTypeInfo().IsEnum && value is IList<string>)
					{
						var valuesStr = (IList<string>)value;
						var values = new object[valuesStr.Count];
						for (var i = 0; i < valuesStr.Count; i++)
						{
							value = valuesStr[i]; // used on failure in exception block
							values[i] = TypeConvert.Convert(typeof(string), expectedType, value);
						}

						if (IsSigned(Enum.GetUnderlyingType(expectedType)))
						{
							var combinedValue = 0L;
							foreach (var enumValue in values)
							{
								value = enumValue; // used on failure in exception block
								combinedValue |= (long)TypeConvert.Convert(expectedType, typeof(long), value);
							}

							value = Enum.ToObject(expectedType, combinedValue);
						}
						else
						{
							var combinedValue = 0UL;
							foreach (var enumValue in values)
							{
								value = enumValue; // used on failure in exception block
								combinedValue |= (ulong)TypeConvert.Convert(expectedType, typeof(ulong), enumValue);
							}

							value = Enum.ToObject(expectedType, combinedValue);
						}
					}
					else if (value is IList<string> && expectedType.GetTypeInfo().IsAssignableFrom(typeof(IList<string>).GetTypeInfo()) == false)
					{
						throw new FormatException(string.Format("Parameter has {0} values while only one is expected.", ((IList<string>)value).Count));
					}
					else
					{
						value = TypeConvert.Convert(value.GetType(), expectedType, value);
					}
				}
				else if (parameter.IsOptional)
				{
					value = parameter.DefaultValue;
				}
				else if (expectedType == typeof(bool))
				{
					value = false;
				}
				else
				{
					throw new InvalidOperationException("Missing parameter value.");
				}
			}
			catch (Exception bindingError)
			{
				return new ParameterBindResult(parameter, bindingError, value);
			}

			if (value != null && value.GetType() != parameter.ParameterType)
				value = TypeConvert.Convert(value.GetType(), parameter.ParameterType, value);

			return new ParameterBindResult(parameter, null, value);
		}
		private static bool IsSigned(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return type == typeof(sbyte) || type == typeof(short) || type == typeof(int) || type == typeof(long);
		}
		private static void AppendLinePadded(StringBuilder builder, string text)
		{
			if (builder == null) throw new ArgumentNullException("builder");
			if (text == null) throw new ArgumentNullException("text");

			var padding = GetPadding(builder);
			var chunkSize = Console.WindowWidth - padding - Environment.NewLine.Length;
			for (var c = 0; c < text.Length; c += chunkSize)
			{
				if (c > 0)
					builder.Append(' ', padding);
				builder.Append(text, c, Math.Min(text.Length - c, chunkSize));
				builder.AppendLine();
			}
		}
		private static int GetPadding(StringBuilder builder)
		{
			if (builder == null) throw new ArgumentNullException("builder");

			var padding = 0;
			while (padding + 1 < builder.Length && builder[builder.Length - 1 - padding] != '\n')
				padding++;

			return padding;
		}
		private static string GetParameterTypeFriendlyName(ParameterInfo parameter)
		{
			if (parameter == null) throw new ArgumentNullException("parameter");

			var parameterType = parameter.ParameterType;
			parameterType = Nullable.GetUnderlyingType(parameterType) ?? parameterType;

			if (parameterType == typeof(int) || parameterType == typeof(sbyte) || parameterType == typeof(short) || parameterType == typeof(long) ||
				parameterType == typeof(uint) || parameterType == typeof(byte) || parameterType == typeof(ushort) || parameterType == typeof(ulong))
			{
				return "Integer";
			}
			else if (parameterType == typeof(double) || parameterType == typeof(float) || parameterType == typeof(decimal))
			{
				return "Number";
			}
			else if (parameterType == typeof(string))
			{
				return "Text";
			}
			else if (parameterType == typeof(char))
			{
				return "Symbol";
			}
			else if (parameterType == typeof(bool))
			{
				return "true/false";
			}
			else
			{
				return parameterType.Name;
			}
		}

		private static bool IsValidCommandSignature(MethodInfo method)
		{
			return method.IsStatic && method.ReturnType == typeof(int) && method.IsGenericMethod == false && method.IsSpecialName == false;
		}

		private static bool IsHidden(MethodInfo method)
		{
			if (method == null) throw new ArgumentNullException("method");

			if (method.GetCustomAttributes(typeof(HiddenAttribute), true).Any())
				return true;

#if NETSTANDARD13
            return false;
#else
			var browsableAttrs = method.GetCustomAttributes(typeof(BrowsableAttribute), true).Cast<BrowsableAttribute>();
			return browsableAttrs.Any(a => a.Browsable == false);
#endif
		}
		private static bool IsHidden(ParameterInfo parameterInfo)
		{
			if (parameterInfo == null) throw new ArgumentNullException("parameterInfo");

			if (parameterInfo.GetCustomAttributes(typeof(HiddenAttribute), true).Any())
				return true;

#if NETSTANDARD13
            return false;
#else
			var browsableAttrs = parameterInfo.GetCustomAttributes(typeof(BrowsableAttribute), true).Cast<BrowsableAttribute>();
			return browsableAttrs.Any(a => a.Browsable == false);
#endif

		}
	}
}
