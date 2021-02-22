using deniszykov.CommandLine.Binding;

namespace deniszykov.CommandLine
{
	/// <summary>
	/// Service providing localizable text during display of help for verbs.
	/// </summary>
	public interface IHelpTextProvider
	{
		/// <summary>
		/// Text similar to - "Usage: "
		/// </summary>
		string VerbUsageText { get; }
		/// <summary>
		/// Text similar to - "Options:"
		/// </summary>
		string VerbOptionsText { get; }
		/// <summary>
		///  Text similar to - " Combination of '{0}'."
		/// </summary>
		string ParameterTypeCombinationOfText { get; }
		/// <summary>
		///  Text similar to - " One of '{0}'."
		/// </summary>
		string ParameterTypeOneOfText { get; }
		/// <summary>
		///   Text similar to - " Default value is '{0}'."
		/// </summary>
		string ParameterTypeDefaultValueText { get; }
		/// <summary>
		/// "Error: "
		/// </summary>
		string NotFoundErrorText { get; }
		/// <summary>
		///  Text similar to - "Verb '{0}' is not found."
		/// </summary>
		string NotFoundMessageText { get; }
		/// <summary>
		///  Text similar to - "Verbs:"
		/// </summary>
		string NotFoundAvailableVerbsText { get; }
		/// <summary>
		///  Text similar to - "Verbs:"
		/// </summary>
		string VerbSetAvailableVerbsText { get; }
		/// <summary>
		///  Text similar to - "Error:"
		/// </summary>
		string InvalidVerbOptionsErrorText { get; }
		/// <summary>
		///  Text similar to - "Invalid parameters specified for '{0}' verb."
		/// </summary>
		string InvalidVerbOptionsMessageText { get; }
		/// <summary>
		/// Text similar to - "Options: "
		/// </summary>
		string InvalidVerbOptionsText { get; }
		/// <summary>
		///  Text similar to - "Error: "
		/// </summary>
		string VerbNotSpecifiedErrorText { get; }
		/// <summary>
		///  Text similar to - "No verb is specified."
		/// </summary>
		string VerbNotSpecifiedMessageText { get; }

		/// <summary>
		/// Text used before every help message.
		/// </summary>
		string HelpHeaderText { get; }
		/// <summary>
		/// Text used after every help message.
		/// </summary>
		string HelpFooterText { get; }

		/// <summary>
		/// Tries to provide friendly name of parameter's type.
		/// </summary>
		bool TryGetParameterTypeFriendlyName(VerbParameter parameter, out string friendlyName);
	}
}
