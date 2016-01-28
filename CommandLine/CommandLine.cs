/*
	Copyright (c) 2016 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

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

		public static readonly CommandLineArguments Arguments;
		public static event UnhandledExceptionEventHandler UnhandledException;

		static CommandLine()
		{
			var args = Environment.GetCommandLineArgs();
			Arguments = new CommandLineArguments(args.Skip(1));
		}

		public static int Run<T>(CommandLineArguments arguments, string defaultMethodName = null)
		{
			if (arguments == null) throw new ArgumentNullException("arguments");

			try
			{
				var bindResult = BindMethod(typeof(T), defaultMethodName, arguments);

				if (bindResult.IsSuccess)
				{
					return (int)bindResult.Method.Invoke(null, bindResult.Arguments);
				}
				else
				{
					if (bindResult.Candidate == null)
					{
						Console.Error.WriteLine($"Unknown command '{bindResult.MethodName}'.");
						Describe<T>();
					}
					else
					{
						Console.Error.WriteLine($"Invalid parameters for '{bindResult.Candidate.Name}'.");
						Describe<T>(bindResult.Candidate.Name);
					}
				}
				return 1;
			}
			catch (Exception exception)
			{
				var handler = UnhandledException ?? DefaultUnhandledExceptionHandler;
				handler(null, new UnhandledExceptionEventArgs(exception, isTerminating: true));
				return DotNetExceptionExitCode;
			}
		}
		public static int Describe<T>(string methodToDescribe = null)
		{
			var type = typeof(T);
			var fullDescription = string.IsNullOrEmpty(methodToDescribe) == false;
			var result = new StringBuilder();
			var description = type.GetCustomAttributes(typeof(DescriptionAttribute), true).Cast<DescriptionAttribute>().FirstOrDefault();
			if (description != null) result.AppendLine(description.Description).AppendLine();

			if (string.IsNullOrEmpty(methodToDescribe))
				result.AppendLine("Actions:");
			foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
			{
				if (!method.IsStatic || method.ReturnType != typeof(int))
					continue;

				if (IsHidden(method))
					continue;

				if (string.IsNullOrEmpty(methodToDescribe) == false && !string.Equals(method.Name, methodToDescribe, StringComparison.OrdinalIgnoreCase))
					continue;

				result.AppendFormat("  {0} ", method.Name.ToUpper());
				var basePadding = GetPadding(result);

				if (fullDescription)
				{
					var paramsArr = method.GetParameters().Where(p => !IsHidden(p)).Select(parameter => string.Format(parameter.IsOptional ? "[--{0} <{0}>] " : "--{0} <{0}> ", parameter.Name)).ToArray();
					var paramsDesc = string.Concat(paramsArr);
					AppendPadded(result, paramsDesc);
					result.AppendLine();
					result.Append(' ', basePadding);
				}

				var methodDescription = method.GetCustomAttributes(typeof(DescriptionAttribute), true).Cast<DescriptionAttribute>().FirstOrDefault();
				if (methodDescription != null)
					AppendPadded(result, methodDescription.Description);
				else
					result.AppendLine();

				if (!fullDescription)
					continue;

				result.AppendLine();
				foreach (var parameter in method.GetParameters())
				{
					if (IsHidden(parameter))
						continue;

					if (parameter.ParameterType == typeof(CommandLineArguments))
						continue;

					var parameterType = parameter.ParameterType.Name;
					var options = "";
					var defaultValue = "";
					if (parameter.ParameterType.IsEnum) options = " Options: " + string.Join(", ", Enum.GetNames(parameter.ParameterType)) + ".";
					if (parameter.IsOptional && parameter.DefaultValue != null) defaultValue = " By default is '" + Convert.ToString(parameter.DefaultValue) + "'.";

					var paramDescription = parameter.GetCustomAttributes(typeof(DescriptionAttribute), true).Cast<DescriptionAttribute>().FirstOrDefault();

					result.Append(' ', basePadding + 2);
					result.Append(parameter.Name.ToUpperInvariant()).Append(" ");
					AppendPadded(result, string.Format("({0}) {1}{2}{3}", parameterType, paramDescription != null ? paramDescription.Description : string.Empty, defaultValue, options));
				}
			}

			Console.WriteLine(result);
			return 0;
		}

		private static void DefaultUnhandledExceptionHandler(object source, UnhandledExceptionEventArgs eventArgs)
		{
			Console.Error.WriteLine(eventArgs.ExceptionObject);
		}
		private static MethodBindResult BindMethod(Type type, string defaultMethodName, CommandLineArguments arguments)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (arguments == null) throw new ArgumentNullException("arguments");

			var methodName = defaultMethodName;
			if (arguments.ContainsKey("0"))
			{
				arguments = new CommandLineArguments(arguments);
				methodName = TypeConvert.ToString(arguments["0"]);
				// unshift positional parameters
				arguments.Remove("0");
				for (var i = 1; ; i++)
				{
					var value = default(object);
					var positional = i.ToString();
					if (arguments.TryGetValue(positional, out value) == false)
						break;
					arguments[(i - 1).ToString()] = value;
					arguments.Remove(positional);
				}
			}

			var candidate = default(MethodInfo);
			var allMethods = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
			foreach (var method in allMethods)
			{
				if (string.Equals(method.Name, methodName, StringComparison.OrdinalIgnoreCase) == false || !method.IsStatic || method.ReturnType != typeof(int) || IsHidden(method))
					continue;

				candidate = method;
				var parameters = method.GetParameters();
				var methodArgs = new object[parameters.Length];
				foreach (var parameter in parameters)
				{
					var value = default(object);
					if (TryBindParameter(parameter, arguments, out value) == false)
						goto nextMethod;
					methodArgs[parameter.Position] = value;
				}

				return new MethodBindResult(method, methodArgs);

				nextMethod:
				;
			}

			return new MethodBindResult(methodName, candidate);
		}
		private static bool TryBindParameter(ParameterInfo parameter, CommandLineArguments arguments, out object value)
		{
			if (parameter.ParameterType == typeof(CommandLineArguments))
				value = new CommandLineArguments(arguments);
			else if (arguments.TryGetValue(parameter.Name, out value) || arguments.TryGetValue((parameter.Position + 1).ToString(), out value))
			{
				if (parameter.ParameterType.IsArray)
				{
					var elemType = parameter.ParameterType.GetElementType();
					Debug.Assert(elemType != null, "elemType != null");
					if (value is string[])
					{
						var oldArr = value as string[];
						var newArr = Array.CreateInstance(elemType, oldArr.Length);
						for (var v = 0; v < oldArr.Length; v++)
							newArr.SetValue(TypeConvert.Convert(typeof(string), elemType, oldArr[v]), v);
						value = newArr;
					}
					else if (value != null)
					{
						var newArr = Array.CreateInstance(elemType, 1);
						newArr.SetValue(TypeConvert.Convert(value.GetType(), elemType, value), 0);
						value = newArr;
					}
					else
					{
						var newArr = Array.CreateInstance(elemType, 0);
						value = newArr;
					}
				}
				else if (parameter.ParameterType == typeof(bool) && value == null)
					value = true;
				else if (value == null)
					value = parameter.IsOptional ? parameter.DefaultValue : parameter.ParameterType.IsValueType ? TypeActivator.CreateInstance(parameter.ParameterType) : null;
				else
					value = TypeConvert.Convert(value.GetType(), parameter.ParameterType, value);
			}
			else if (parameter.IsOptional)
				value = parameter.DefaultValue;
			else
				return false;

			return true;
		}
		private static void AppendPadded(StringBuilder builder, string text)
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
		private static bool IsHidden(ICustomAttributeProvider provider)
		{
			if (provider == null) throw new ArgumentNullException("provider");

			var browsableAttrs = provider.GetCustomAttributes(typeof(BrowsableAttribute), true).Cast<BrowsableAttribute>();
			return browsableAttrs.Any(a => a.Browsable == false);
		}
	}
}
