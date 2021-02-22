using deniszykov.CommandLine.Binding;

namespace deniszykov.CommandLine
{
	/// <summary>
	/// Builder for <see cref="VerbSet"/>.
	/// </summary>
	public interface IVerbSetBuilder
	{
		/// <summary>
		/// Complete <see cref="VerbSet"/> building.
		/// </summary>
		VerbSet Build();
	}
}