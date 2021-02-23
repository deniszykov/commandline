using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using deniszykov.CommandLine.Annotations;
using JetBrains.Annotations;

namespace deniszykov.CommandLine.Binding
{
	/// <summary>
	/// Verb's parameter. It is an option, values or bound service.
	/// </summary>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public sealed class VerbParameter
	{
		/// <summary>
		/// Option/value's name. Could be long or alias(short) name.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// Option's alias. Could be null.
		/// </summary>
		public readonly string? Alias;
		/// <summary>
		/// Description or help text for this option/values.
		/// </summary>
		public readonly string Description;
		/// <summary>
		/// Option's position. Used for positional binding of options.
		/// </summary>
		public readonly int Position;
		/// <summary>
		/// Parameter's index in argument array for <see cref="Verb.Invoker"/>.
		/// </summary>
		public readonly int ArgumentIndex;
		/// <summary>
		/// Type of option/values/service.
		/// </summary>
		public readonly TypeInfo ValueType;
		/// <summary>
		/// Default value for option.
		/// </summary>
		public readonly object? DefaultValue;
		/// <summary>
		/// Arity of option.
		/// </summary>
		public readonly ValueArity ValueArity;
		/// <summary>
		/// Flag indication that this option is non-required for providing.
		/// </summary>
		public readonly bool IsOptional;
		/// <summary>
		/// Flag indication that this option is hidden from help display.
		/// </summary>
		public readonly bool IsHidden;
		/// <summary>
		/// Flag indication that this parameter will collect all remaining values.
		/// </summary>
		public readonly bool IsValueCollector;
#if !NETSTANDARD1_6
		/// <summary>
		/// <see cref="System.ComponentModel.TypeConverter"/> used to convert value for this option from string representation.
		/// </summary>
		public readonly System.ComponentModel.TypeConverter? TypeConverter;
#endif

		/// <summary>
		/// Constructor for <see cref="VerbParameter"/>.
		/// </summary>
		public VerbParameter(
			 string name,
			 string? alias,
			 string description,
			int position,
			int argumentIndex,
			 TypeInfo valueType,
#if !NETSTANDARD1_6
			System.ComponentModel.TypeConverter? typeConverter,
#endif
			 object? defaultValue,
			ValueArity valueArity,
			bool isOptional,
			bool isHidden,
			bool isValueCollector)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (description == null) throw new ArgumentNullException(nameof(description));
			if (valueType == null) throw new ArgumentNullException(nameof(valueType));

			this.Name = name;
			this.Alias = alias;
			this.Description = description;
			this.Position = position;
			this.ArgumentIndex = argumentIndex;
			this.ValueType = valueType;
#if !NETSTANDARD1_6
			this.TypeConverter = typeConverter;
#endif
			this.DefaultValue = defaultValue;
			this.ValueArity = valueArity;
			this.IsOptional = isOptional;
			this.IsHidden = isHidden;
			this.IsValueCollector = isValueCollector;
		}
		/// <summary>
		/// Constructor for <see cref="VerbParameter"/> from <see cref="ParameterInfo"/> and position.
		/// </summary>
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