namespace deniszykov.CommandLine
{
	public readonly struct OptionCount
	{
		public readonly int Value;

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
