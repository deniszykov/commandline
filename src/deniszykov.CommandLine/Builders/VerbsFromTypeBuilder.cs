/*
	Copyright (c) 2021 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

using System;
using System.Reflection;
using deniszykov.CommandLine.Binding;

namespace deniszykov.CommandLine.Builders
{
	internal sealed class VerbsFromTypeBuilder : IVerbSetBuilder
	{
		private readonly TypeInfo verbSetType;
		private VerbSet? verbSet;

		public VerbsFromTypeBuilder(TypeInfo verbSetType)
		{
			if (verbSetType == null) throw new ArgumentNullException(nameof(verbSetType));

			this.verbSetType = verbSetType;
		}

		/// <inheritdoc />
		public VerbSet Build()
		{
			return this.verbSet ??= new VerbSet(this.verbSetType);
		}
	}
}
