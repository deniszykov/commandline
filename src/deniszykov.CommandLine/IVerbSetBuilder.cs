/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using deniszykov.CommandLine.Binding;
using JetBrains.Annotations;

namespace deniszykov.CommandLine
{
	/// <summary>
	/// Builder for <see cref="VerbSet"/>.
	/// </summary>
	[PublicAPI]
	public interface IVerbSetBuilder
	{
		/// <summary>
		/// Complete <see cref="VerbSet"/> building.
		/// </summary>
		VerbSet Build();
	}
}