using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace System
{
    [Serializable]
    public sealed class CommandLineException : Exception
    {
        public CommandLineException(string message) : base(message)
        {
        }

        public CommandLineException(string message, Exception innerException) : base(message, innerException)
        {
        }

        private CommandLineException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        internal static CommandLineException CommandNotFound(string commandName)
        {
            if (commandName == null) throw new ArgumentNullException("commandName");

            return new CommandLineException(string.Format("Command '{0}' is not found.", commandName));
        }
        internal static CommandLineException InvalidCommandParameters(MethodInfo commandMethod, ParameterBindResult[] parameterBindResults)
        {
            if (commandMethod == null) throw new ArgumentNullException("commandMethod");
            if (parameterBindResults == null) throw new ArgumentNullException("parameterBindResults");

            var bindingErrors = new Hashtable();
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
                    var value = TypeConvert.ToString(parameterBindResult.Value);
                    if (value.Length > 32)
                        builder.Append(value.Substring(0, 32)).AppendLine("...");
                    else
                        builder.AppendLine(value);
                }
                else
                {
                    bindingErrors[parameterBindResult.Parameter.Name] = parameterBindResult.Error;

                    var errorMessage = parameterBindResult.Error.Message;
                    builder.Append("(").Append(parameterBindResult.Parameter.ParameterType.Name).Append(") ");
                    if (parameterBindResult.Error is FormatException && parameterBindResult.Value != null)
                    {
                        var value = TypeConvert.ToString(parameterBindResult.Value);
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
            error.Data["methodToken"] = commandMethod.MetadataToken;
            error.Data["bindingErrors"] = bindingErrors;
            return error;
        }
    }
}
