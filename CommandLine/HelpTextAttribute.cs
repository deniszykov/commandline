/*
	Copyright (c) 2017 Denis Zykov
	
	This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 

	License: https://opensource.org/licenses/MIT
*/

// ReSharper disable once CheckNamespace
namespace System
{
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method | AttributeTargets.Class)]
	public sealed class HelpTextAttribute : Attribute
	{
		public string Description { get; private set; }

		public HelpTextAttribute(string description)
		{
			if (description == null) throw new ArgumentNullException("description");

			this.Description = description;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.Description;
		}
	}
}
