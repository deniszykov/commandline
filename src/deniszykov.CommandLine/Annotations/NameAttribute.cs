using System;

namespace deniszykov.CommandLine.Annotations
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
	public class NameAttribute : Attribute
	{
		public virtual string Name { get; private set; }

		public NameAttribute(string name)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (name.Length == 0) throw new ArgumentOutOfRangeException(nameof(name));

			this.Name = name;
		}

		/// <inheritdoc />
		public override string ToString() => this.Name;
	}
}
