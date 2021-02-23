/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;

namespace deniszykov.CommandLine.Annotations
{
	/// <summary>
	/// Attribute used to provide help text for verb set, verb or parameter.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method | AttributeTargets.Class)]
	public class HelpTextAttribute : Attribute
	{
		/// <summary>
		/// Help text. Should ends with dot.
		/// </summary>
		public virtual string Text { get; }

		/// <summary>
		/// Create new instance of <see cref="HelpTextAttribute"/>.
		/// </summary>
		public HelpTextAttribute(string text)
		{
			if (text == null) throw new ArgumentNullException(nameof(text));

			this.Text = text;
		}

		/// <inheritdoc />
		public override string ToString() => this.Text;
	}
}
