using System;

namespace deniszykov.CommandLine.Annotations
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public class AliasAttribute : Attribute
	{
		public virtual string Alias { get; set; }

		public AliasAttribute(string alias)
		{
			this.Alias = alias;
		}

		/// <inheritdoc />
		public override string ToString() => this.Alias;
	}
}
