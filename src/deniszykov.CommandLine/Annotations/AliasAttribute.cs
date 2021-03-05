/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Annotations
{
	/// <summary>
	/// Verb's short name aka alias. Should be one letter.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter)]
	[PublicAPI]
	public class AliasAttribute : Attribute
	{
		/// <summary>
		/// Alias value. Not null.
		/// </summary>
		public virtual string Alias { get; }

		/// <summary>
		/// Verb's short name aka alias. 
		/// </summary>
		/// <param name="alias">Alias. Should be one letter.</param>
		public AliasAttribute(string alias)
		{
			if (alias == null) throw new ArgumentNullException(nameof(alias));
			if (alias.Length != 1) throw new ArgumentOutOfRangeException(nameof(alias), alias, "Alias should be 1 letter long.");
			if (!char.IsLetter(alias[0])) throw new ArgumentException("Alias should be a letter.", nameof(alias));

			this.Alias = alias;
		}

		/// <inheritdoc />
		public override string ToString() => this.Alias;
	}
}
