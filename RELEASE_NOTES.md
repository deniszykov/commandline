# 2.2.1
- fixed generic methods, property getters and operators returning `int` being discovered as verbs.
- fixed `CommandLineConfiguration.OutputSubVerbHelpTitle` not being inherited by sub-verbs and being ignored when configured through .NET Generic Host.
- fixed `IHostBuilder.ConfigureCommandLineHost(buildDelegate)` overload without arguments taking the executable path as a verb name.
- documentation: added settings reference, unknown options, error handling and extensibility pages to the [wiki](https://github.com/deniszykov/commandline/wiki).

# 2.2.0
- feature: added `CommandLineConfiguration.FailOnUnknownOptions`. When `true`, an option that matches no parameter of the verb fails binding with an "Unknown option" error instead of being ignored. Off by default. Sub-verbs inherit it, and `ICommandLineBuilder.Configure` on a sub-verb builder turns it on or off for that sub-verb set only. Verbs with sub-verbs are not checked. While it is on, `AllowPrefixesInOptionValues` is treated as `false`.
- fixed `--option=value` followed by another option: the next option was taken as a second value (`--name=a --flag` failed with "Option expects a single argument but received 2").

# 2.1.12
- bumped framework support to .NET 10

# 2.1.11
- made `enum` binding case-insensitive.

# 2.1.10
- updated dependencies
- feature: added `CommandLineConfiguration.AllowPrefixesInOptionValues` which allow to have `--unknown-options` after known ones. If set to true then such `--unknown-options` will be captured as values of known option. 

# 2.1.8
- updated dependencies
- feature: added alternative ExceptionEventHandler registration via `services.AddTransient<ExceptionEventHandler>()`

# 2.1.7
- added option to display title text for sub-verbs
- fixed help text generation for sub-verbs

# 2.0.2 - 2.0.6
- added possibility to use multiple c# classes as combined source of verbs
- updated dependencies

# 2.0.1
- updated target frameworks for 'Hosted' version of package to include .NET Framework 4.6.1

# 2.0.0
- rewritten as a service
- commands, Parameters are now Verbs, Options, Values
- now follows getOpt syntax
- services could be injected in parameters
- verbs are now could be async and cancelled
- configuration is now in separate class
- help request is build-in and doesn't require custom 'Help' command
- console has been abstracted and could be replaced
- help text is now localizable
- fixed bugs with help text and sub-verbs

# 1.3.1 - 1.3.2
- TypeConvert dependency update (bug fixes)

# 1.3.0
- added TypeConverterAttribute support on command parameters. It's takes precendence before any other types of type conversions.

# 1.2.9
- added netcoreapp2.1 target platform
- dependencies update (internal)

# 1.2.7
- fixed exception when calling Describe while console output is redirected
- TypeConvert package update

# 1.2.6
- TypeConvert package update
- documentation update

# 1.2.5
- added WriteWholeErrorMessageOnBindFailure option for debugging purpose (it writes descriptive error message to stderr stream)
- added DescribeExitCode option for controlling exit code of Describe method
- tuned error messages when no command is specified or wrong parameters are passed
- tuned Describe method for better description text (friendly type names, nullable types support etc...)

# 1.2.4
- fixed binding error when no default action is specified
- added XML documentation file to package

# 1.2.3
- updated references for .NET Core Targets and .NET Standard

# 1.2.2
- returned original library name ConsoleApp.CommandLine.dll 

# 1.2.1
- embedded TypeConvert dependency

# 1.2.0
- CommandLine.UnhandledException type changed to ExceptionEventHandler
- added custom description attributes as replacement to System.ComponentModel attributes: HelpTextAttribute and HiddenAttribute
- added support of .NET Standard platform

# 1.1.3
- refactored error messages fo parameters binding failure cases.
- added CommandLineException to signal binding failures.
- fixed few array parameter binding bugs

# 1.1.2
- added bare double hyphen to enforce positional parameters
- added bare single hyphen to disable hyphen interpretation in values
- added special treatment for negative numbers
- added CommandLine.DescribeOnBindFailure which controls reaction on method binding failure (true to run CommandLine.Describe(), false to throw exception).
- added enum flags binding subroutine, now "--flag Flag1 Flag2 Flag3" arguments are supported.
- changed method binding order to from most parameters to less (original was chaotic), binding strategy is still - "first match".
- added non-generic Run and Describe methods
- fixed bug with positional parameters binding

# 1.0.0
- initial release