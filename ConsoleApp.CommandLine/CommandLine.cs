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
        public static bool DescribeOnBindFailure = true;

        static CommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            Arguments = new CommandLineArguments(args.Skip(1));
        }

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
                    if (DescribeOnBindFailure)
                    {
                        if (bindResult.Candidate == null)
                        {
                            Console.Error.WriteLine("Unknown command '{0}'.", bindResult.MethodName);
                            Describe(type);
                        }
                        else
                        {
                            Console.Error.WriteLine("Invalid parameters for '{0}'.", bindResult.Candidate);
                            Describe(type, bindResult.Candidate.Name);
                        }
                    }
                    else
                    {
                        if (bindResult.Candidate == null) throw new InvalidOperationException(string.Format("Unknown command '{0}'.", bindResult.MethodName));
                        else throw new InvalidOperationException(string.Format("Invalid parameters for '{0}'.", bindResult.Candidate));
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

            var methodName = defaultMethodName;
            if (arguments.ContainsKey("0"))
            {
                arguments = new CommandLineArguments(arguments);
                methodName = TypeConvert.ToString(arguments["0"]);
                arguments.RemoveAt(0);
            }

            var candidate = default(MethodInfo);
            var allMethods = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Array.Sort(allMethods, (x, y) => y.GetParameters().Length.CompareTo(x.GetParameters().Length));
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
            var expectedType = parameter.ParameterType;
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
                    if (value is string[])
                    {
                        var oldArr = value as string[];
                        var newArr = Array.CreateInstance(elemType, oldArr.Length);
                        for (var v = 0; v < oldArr.Length; v++) newArr.SetValue(TypeConvert.Convert(typeof(string), elemType, oldArr[v]), v);

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
                else if (expectedType == typeof(bool) && value == null)
                {
                    value = true;
                }
                else if (value == null)
                {
                    value = parameter.IsOptional ? parameter.DefaultValue : expectedType.IsValueType ? TypeActivator.CreateInstance(expectedType) : null;
                }
                else if (expectedType.IsEnum && value is string[])
                {
                    var valuesStr = (string[])value;
                    var values = Array.ConvertAll(valuesStr, v => TypeConvert.Convert(typeof(string), expectedType, v));
                    if (IsSigned(Enum.GetUnderlyingType(expectedType)))
                    {
                        var combinedValue = 0L;
                        foreach (var v in values)
                            combinedValue |= (long)TypeConvert.Convert(expectedType, typeof(long), v);

                        value = Enum.ToObject(expectedType, combinedValue);
                    }
                    else
                    {
                        var combinedValue = 0UL;
                        foreach (var v in values)
                            combinedValue |= (ulong)TypeConvert.Convert(expectedType, typeof(ulong), v);

                        value = Enum.ToObject(expectedType, combinedValue);
                    }
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
                return false;
            }

            return true;
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
