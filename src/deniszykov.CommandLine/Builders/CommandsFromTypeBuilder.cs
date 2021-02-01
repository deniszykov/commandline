using System;
using System.Reflection;
using deniszykov.CommandLine.Binding;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Builders
{
	internal sealed class CommandsFromTypeBuilder : ICommandsBuilder
	{
		[NotNull] private readonly TypeInfo commandSetType;
		private CommandSet commandSet;

		public CommandsFromTypeBuilder([NotNull] TypeInfo commandSetType)
		{
			if (commandSetType == null) throw new ArgumentNullException(nameof(commandSetType));

			this.commandSetType = commandSetType;
		}

		/// <inheritdoc />
		public CommandSet Build()
		{
			return this.commandSet ??= new CommandSet(this.commandSetType);
		}
	}
}
