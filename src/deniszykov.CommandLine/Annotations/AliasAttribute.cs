using System;

namespace deniszykov.CommandLine.Annotations
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public class AliasAttribute : Attribute
	{
		public virtual string Alias { get; private set; }

		public AliasAttribute(string alias)
		{
			if (alias.Length != 1) throw new ArgumentOutOfRangeException(nameof(alias), alias, "Alias should be 1 letter long.");
			if (!char.IsLetter(alias[0])) throw new ArgumentException("Alias should be a letter.", nameof(alias));

			this.Alias = alias;
		}

		/// <inheritdoc />
		public override string ToString() => this.Alias;
	}
}
