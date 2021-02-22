namespace deniszykov.CommandLine.Binding
{
	/// <summary>
	/// Arity of option's value.
	/// </summary>
	public enum ValueArity
	{
		/// <summary>
		/// Options doesn't have value.
		/// </summary>
		Zero,
		/// <summary>
		/// Option has zero or one value.
		/// </summary>
		ZeroOrOne,
		/// <summary>
		/// Option has one value.
		/// </summary>
		One,
		/// <summary>
		/// Option has zero or many values.
		/// </summary>
		ZeroOrMany,
		/// <summary>
		/// Option has one or many values.
		/// </summary>
		OneOrMany,
	}
}