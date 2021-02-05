using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using deniszykov.CommandLine.Annotations;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Binding
{
	public sealed class VerbParameter
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
		public readonly ValueArity ValueArity;
		public readonly bool IsOptional;
		public readonly bool IsHidden;
		public readonly bool IsValueCollector;
#if !NETSTANDARD1_6
		public readonly System.ComponentModel.TypeConverter TypeConverter;
#endif

		public VerbParameter(ParameterInfo parameterInfo, int position)
		{
			if (parameterInfo == null) throw new ArgumentNullException(nameof(parameterInfo));

			this.Name = parameterInfo.GetName() ?? parameterInfo.Name;
			this.Alias = parameterInfo.GetAlias();
			this.Description = parameterInfo.GetDescription() ?? string.Empty;
			this.Position = position;
			this.ArgumentIndex = parameterInfo.Position;
			this.ValueType = parameterInfo.ParameterType.GetTypeInfo();
			this.DefaultValue = parameterInfo.DefaultValue ?? (this.ValueType.IsValueType ? Activator.CreateInstance(this.ValueType.AsType()) : null);
			this.IsOptional = parameterInfo.IsOptional;
			this.IsHidden = parameterInfo.IsHidden();
			this.IsValueCollector = parameterInfo.GetCustomAttributes<ParamArrayAttribute>().Any();

			var isList = this.ValueType.IsArray || this.ValueType.IsInstantiationOf(typeof(IList<>).GetTypeInfo());
			var isFlags = this.ValueType.IsEnum && this.ValueType.IsFlags();
			var isBool = this.ValueType.AsType() == typeof(bool);
			var isCount = this.ValueType.AsType() == typeof(OptionCount);
			this.ValueArity = 
				isCount ? ValueArity.Zero :
				isBool ? ValueArity.ZeroOrOne :
				isList ? ValueArity.ZeroOrMany :
				isFlags ? ValueArity.OneOrMany :
				ValueArity.One;

#if !NETSTANDARD1_6
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