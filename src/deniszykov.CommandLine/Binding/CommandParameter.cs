using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using deniszykov.CommandLine.Annotations;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Binding
{
	public sealed class CommandParameter
	{
		[NotNull]
		public readonly string Name;
		[CanBeNull]
		public readonly string Alias;
		[NotNull]
		public string Description;
		public readonly int Position;
		public readonly int ArgumentIndex;
		[NotNull]
		public readonly TypeInfo ValueType;
		[CanBeNull]
		public readonly object DefaultValue;
		public readonly ParameterValueArity ValueArity;
		public readonly bool IsOptional;
#if !NETSTANDARD1_3
		public readonly System.ComponentModel.TypeConverter TypeConverter;
#endif

		public CommandParameter(ParameterInfo parameterInfo, int position)
		{
			if (parameterInfo == null) throw new ArgumentNullException(nameof(parameterInfo));

			this.Name = parameterInfo.GetName() ?? parameterInfo.Name;
			this.Alias = parameterInfo.GetAlias();
			this.Description = parameterInfo.GetDescription() ?? string.Empty;
			this.Position = position;
			this.ArgumentIndex = parameterInfo.Position;
			this.ValueType = parameterInfo.ParameterType.GetTypeInfo();
			this.DefaultValue = parameterInfo.DefaultValue;
			this.IsOptional = parameterInfo.IsOptional;

			var isList = this.ValueType.IsArray || this.ValueType.IsInstantiationOf(typeof(IList<>).GetTypeInfo());
			var isFlags = this.ValueType.IsEnum && this.ValueType.IsFlags();
			this.ValueArity = parameterInfo.ParameterType == typeof(bool) ? ParameterValueArity.ZeroOrOne :
				isList ? ParameterValueArity.ZeroOrMany :
				isFlags ? ParameterValueArity.OneOrMany :
				ParameterValueArity.One;

#if !NETSTANDARD1_3
			var typeConverterAttribute = parameterInfo.GetCustomAttributes(typeof(System.ComponentModel.TypeConverterAttribute), inherit: true).FirstOrDefault();
			if (typeConverterAttribute != null)
			{
				var typeConverterType = Type.GetType(((System.ComponentModel.TypeConverterAttribute)typeConverterAttribute).ConverterTypeName, throwOnError: true);
				var typeConverter = (System.ComponentModel.TypeConverter)Activator.CreateInstance(typeConverterType);
				if (typeConverter.GetType() != typeof(System.ComponentModel.TypeConverter))
				{
					this.TypeConverter = typeConverter;
				}
			}
#endif
		}

		/// <inheritdoc />
		public override string ToString() => this.Name;
	}
}