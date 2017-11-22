/*
	Copyright (c) 2016 Denis Zykov
	
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
        /// <summary>
        /// Exception handling for <see cref="Run"/> method.
        /// </summary>
        public static event UnhandledExceptionEventHandler UnhandledException;
        /// <summary>
        /// Try to describe API into default output when bind error occurs (command name mistype or wrong arguments).
        /// </summary>
        public static bool DescribeOnBindFailure = true;
        /// <summary>
        /// Exit code used on 'soft' binding failure when command description is displayed.
        /// </summary>
        public static int BindFailureExitCode = 1;

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
                        Console.Error.WriteLine("Error: " + error.Message + Environment.NewLine);
                        Describe(type);
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
                handler(null, new UnhandledExceptionEventArgs(exception, isTerminating: true));

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

            var fullDescription = string.IsNullOrEmpty(commandToDescribe) == false;
            var result = new StringBuilder();
            var description = type.GetCustomAttributes(typeof(DescriptionAttribute), true).Cast<DescriptionAttribute>().FirstOrDefault();
            if (description != null) result.AppendLine(description.Description).AppendLine();

            if (string.IsNullOrEmpty(commandToDescribe))
                result.AppendLine("Commands:");
            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (!method.IsStatic || method.ReturnType != typeof(int))
                    continue;

                if (IsHidden(method))
                    continue;

                if (string.IsNullOrEmpty(commandToDescribe) == false && !string.Equals(method.Name, commandToDescribe, StringComparison.OrdinalIgnoreCase))
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

            var allMethods = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Array.Sort(allMethods, (x, y) => y.GetParameters().Length.CompareTo(x.GetParameters().Length));

            if (arguments.ContainsKey("0"))
            {
                var newArguments = new CommandLineArguments(arguments);
                var methodName = TypeConvert.ToString(newArguments["0"]);
                newArguments.RemoveAt(0);
                var result = BindMethod(allMethods, methodName, newArguments);
                if (result.IsSuccess)
                    return result;
            }

            if (string.IsNullOrEmpty(defaultMethodName) == false)
            {
                var result = BindMethod(allMethods, defaultMethodName, arguments);
                if (result.IsSuccess)
                    return result;
            }

            return new MethodBindResult("<no name specified>", default(Dictionary<MethodInfo, ParameterBindResult[]>));
        }
        private static MethodBindResult BindMethod(MethodInfo[] methods, string methodName, CommandLineArguments arguments)
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
                        value = parameter.IsOptional ? parameter.DefaultValue : expectedType.IsValueType ? TypeActivator.CreateInstance(expectedType) : null;
                    }
                    else if (expectedType.IsEnum && value is IList<string>)
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
                    else if (value is IList<string> && expectedType.IsAssignableFrom(typeof(IList<string>)) == false)
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

            return new ParameterBindResult(parameter, null, value);
        }
        private static bool IsSigned(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            return type == typeof(sbyte) || type == typeof(short) || type == typeof(int) || type == typeof(long);
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
