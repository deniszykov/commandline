using System;
using System.Collections.Generic;
using System.Linq;
using deniszykov.CommandLine.Binding;
using JetBrains.Annotations;

namespace deniszykov.CommandLine
{
	internal static class PropertyExtensions
	{
		/// <summary>
		/// Name of property with list of preceding commands of current <see cref="ICommandsBuilder"/>. Type is array of <see cref="Command"/>.
		/// </summary>
		public const string CommandChainPropertyName = "__command_chain__";

		[NotNull, ItemNotNull]
		public static IEnumerable<Command> GetCommandChain([NotNull] this IDictionary<object, object> properties)
		{
			if (properties == null) throw new ArgumentNullException(nameof(properties));

			if (properties.TryGetValue(CommandChainPropertyName, out var commandChainObj) &&
				commandChainObj is Command[] commandChain)
			{
				return commandChain;
			}
			else
			{
				return Enumerable.Empty<Command>();
			}
		}
		public static void AddCommandChain([NotNull] this IDictionary<object, object> properties, [NotNull] Command command)
		{
			if (properties == null) throw new ArgumentNullException(nameof(properties));
			if (command == null) throw new ArgumentNullException(nameof(command));

			var commandChain = properties.GetCommandChain().ToList();
			if (commandChain.Contains(command))
			{
				throw CommandLineException.RecursiveCommandChain(commandChain.Select(commandInChain => commandInChain.Name), command.Name);
			}
			commandChain.Add(command);
			properties[CommandChainPropertyName] = commandChain.ToArray();
		}
	}
}
