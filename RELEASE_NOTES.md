# 2.0.2
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
- TypeConvert dependecy update (bug fixes)

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