using System;

namespace deniszykov.CommandLine.Annotations
{
	[AttributeUsage(AttributeTargets.Method)]
	public class NameAttribute : Attribute
	{
		public virtual string Name { get; set; }

		public NameAttribute(string name)
		{
			this.Name = name;
		}

		/// <inheritdoc />
		public override string ToString() => this.Name;
	}
}
