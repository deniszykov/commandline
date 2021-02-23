using deniszykov.CommandLine.Binding;

namespace deniszykov.CommandLine.Formatting
{
	internal class DefaultHelpTextProvider : IHelpTextProvider
	{
		/// <inheritdoc />
		public string VerbUsageText => "Usage: ";
		/// <inheritdoc />
		public string VerbOptionsText => "Options: ";
		/// <inheritdoc />
		public string ParameterTypeCombinationOfText => " Combination of '{0}'.";
		/// <inheritdoc />
		public string ParameterTypeOneOfText => " One of '{0}'.";
		/// <inheritdoc />
		public string ParameterTypeDefaultValueText => " Default value is '{0}'.";
		/// <inheritdoc />
		public string NotFoundErrorText => "Error: ";
		/// <inheritdoc />
		public string NotFoundMessageText => "Verb '{0}' is not found.";
		/// <inheritdoc />
		public string NotFoundAvailableVerbsText => "Verbs: ";
		/// <inheritdoc />
		public string VerbSetAvailableVerbsText => "Verbs: ";
		/// <inheritdoc />
		public string InvalidVerbOptionsErrorText => "Error: ";
		/// <inheritdoc />
		public string InvalidVerbOptionsMessageText => "Invalid options or values specified for '{0}' verb.";
		/// <inheritdoc />
		public string InvalidVerbOptionsText => "Options: ";
		/// <inheritdoc />
		public string VerbNotSpecifiedErrorText => "Error: ";
		/// <inheritdoc />
		public string VerbNotSpecifiedMessageText => "No verb is specified.";
		/// <inheritdoc />
		public string HelpHeaderText => "";
		/// <inheritdoc />
		public string HelpFooterText => "";

		/// <inheritdoc />
		public bool TryGetParameterTypeFriendlyName(VerbParameter parameter, out string? friendlyName)
		{
			friendlyName = default;
			return false;
		}
	}
}
