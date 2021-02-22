namespace deniszykov.CommandLine
{
	/// <summary>
	/// Special type used to capture number of times the option was specified. 
	/// </summary>
	public readonly struct OptionCount
	{
		/// <summary>
		/// Count of option specifications.
		/// </summary>
		public readonly int Value;

		/// <summary>
		/// Constructor for <see cref="OptionCount"/>.
		/// </summary>
		public OptionCount(int value)
		{
			this.Value = value;
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			return obj is OptionCount optionCount && optionCount.Value == this.Value;
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return this.Value.GetHashCode();
		}

		/// <inheritdoc />
		public override string ToString() => this.Value.ToString();
	}
}
