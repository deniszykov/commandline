using System;
using System.Reflection;

namespace deniszykov.CommandLine.Annotations
{
	/// <summary>
	/// Verb or option's name. If not set then <see cref="MethodInfo"/> or <see cref="Type"/> name is used instead.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
	public class NameAttribute : Attribute
	{
		/// <summary>
		/// Name of verb/option.
		/// </summary>
		public virtual string Name { get; private set; }

		/// <summary>
		/// Verb or option's name.
		/// </summary>
		/// <param name="name">Should be at least 2 letters long.</param>
		public NameAttribute(string name)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (name.Length <= 1) throw new ArgumentOutOfRangeException(nameof(name));

			this.Name = name;
		}

		/// <inheritdoc />
		public override string ToString() => this.Name;
	}
}
