[![Build Status](https://travis-ci.org/deniszykov/commandline.svg?branch=master)](https://travis-ci.org/deniszykov/commandline)

Introduction
============
A simple [getopt](https://man7.org/linux/man-pages/man3/getopt.3.html) styled command line library.

Installation
============
```
Install-Package deniszykov.CommandLine
```
For .NET Core Hosted environment:
```
Install-Package deniszykov.CommandLine.Hosted
```

Quick Start
============

# Basics
To start, you need to configure the entry point to the application. 
```csharp
	using System;
	using System.ComponentModel;
	using deniszykov.CommandLine;

	[Description("This is an expample application. Type /? for help.")]
	public class Program
	{
		private static int Main(string[] arguments)
		{
			var exitCode = CommandLine
				.CreateFromArguments(arguments)
				.Use<Program>() // set class with verbs/commands
				.Run();
			return exitCode;
		}

		[Description("Says hello to specified 'name'.")]
		public static int Hello(string name)
		{
			Console.WriteLine("Hello " + name + "!");
			return 0; // exit code
		}
	}
```
`CommandLine` relies on reflection to find method to invoke.  
This method should return `int` value which is interpreted as [Exit Code](https://en.wikipedia.org/wiki/Exit_status) of application.

When you could request help for your application:
```console
myapp /?

#>This test application. Type /? for help.

  Verbs:
    HELLO    Says hello to specified 'name'.
```

Or invoke `Hello(string name)` with following command:
```console
myapp hello --name Jake
#>Hello Jake!
```

## Syntax

Each "word" on a command line is a token. The rules of a specific command line application determine whether these tokens are parsed as verbs, options, values, etc. This library follow [getopt](https://man7.org/linux/man-pages/man3/getopt.3.html) syntax with addition of `verbs` preceding options and values.

```console
myapp <VERBS> <OPTIONS> <VALUES>

myapp <VERB> [<VERB> ...] [--|-<OPTION> [<OPTION_VALUE>] ...] [--] [<VALUE> ...]
```

### Verbs

A verb is a token that corresponds to an action that the app will perform. The simplest command line applications have only a one command. In this case verb token could be omitted by specifying a `CommandLineConfiguration.DefaultVerbName` when configuring the `CommandLine`. 

Verb or verbs must come before variants and meanings like this:
```console
> myapp compress -f -x c:/myfile.txt
        ^------^
```

## Sub-verbs

Several verbs can follow each other to achieve more complex APIs:
```console
> myapp disk scan --disk 1
```

### Options

An option is a named parameter that can be passed to a verb. In POSIX command lines, you'll often see options represented like this:

```console
> myapp --int-option 123
        ^----------^
```

In Windows command lines, you'll often see options represented like this:

```console
> myapp /int-option 123
        ^---------^
```
Both syntaxes are valid for this library.

### Short names

In both POSIX and Windows command lines, it's common for some options to have short names (aka aliases). In the following help example, `-v` and `--verbose` are aliases for the same option:

```console
> myapp --verbose
> myapp -v
```

Short named options could be composed together without delimiter. Last one option also could have value like this:

```console
> myapp /?
    -v, --verbose          Show verbose output 
    -d, --debug            nable debug mode
    -w, --warning-level    Set warning level

> myapp -dvw
> myapp -dvw=123
```

### Delimiters

In addition to a space delimiting an option from its argument, `=` is also could be used. The following command lines are equivalent:

```console
> myapp --integer-option 123
> myapp --integer-option=123
> myapp -i=123
```

### Values

A value is an additional tokens after options. They can be any arbitrary string:

```console
> myapp --integer-option 123 c:/myfile.txt d:/myfile.txt
                             ^---------VALUES----------^
```

There is special option ending token `--` which is used to delimit options from values:

```console
> myapp -- -cmd /? this is value
           ^------VALUES-------^
```

## Verbs binding

This library uses reflection to get a list of available verbs. Each method with a suitable signature can be called via the command line. Requirements for method are:

 - Have return type of `int` or `Task&lt;int&gt;`
 - Be non-generic method
 - Be non-special method
 - Have no `NonVerbAttribute` attribute on it

By default, name matching is case-insensitive.  

For each command line invocation, the best matching method is selected so that method overloads are supported.  

## Options binding

Options are bound to the corresponding method parameters by name.

```csharp
int Hello(string name)
{
	Console.WriteLine("Hello " + name + "!");
	return 0;
}
```

```console
myapp HELLO --name Jake
#>Hello Jake!
```

By default, name matching is case-insensitive:

```console
myapp hello --NAME Jake
#>Hello Jake!
```

Required parameters could be bound by position:

```console
myapp HELLO Jake
#>Hello Jake!
```

To be more detailed, positional parameters are taken from the values and, accordingly, "steal" the values. 

### List options

You can add array/list parameter to collect multiple values as shown below:

```csharp
int Hello(string[] names)
{
	Console.WriteLine("Hello " + string.Join(", ", names) + "!");
	return 0;
}
```

```console
myapp HELLO --names Mike Jake
#>Hello Mike, Jake!
```

Parameter accepting multiple values can be only named. Such parameters capture all values after until another option or the end of options `--` is encountered. 

### Optional parameters
You can make any parameter optional by specifying default value:
```csharp
int Print(int myOptionalParam = 100)
{
	Console.WriteLine("myOptionalParam=" + myOptionalParam);
	return 0;
}
```

```console
myapp PRINT --myOptionalParam 200
#>myOptionalParam=200
myapp PRINT
#>myOptionalParam=100
```

Optional parameters cannot be bound by position:
```console
myapp PRINT 200
#>myOptionalParam=100
```

### Flag parameters
You can have a flag(true/false) parameter. It's presence is considered to be "True" and absence is "False":
```csharp
int Flag(bool myFlag)
{
	Console.WriteLine(myFlag ? "Flag is set" : "Flag is not set");
	return 0;
}
```

```bash
myapp.exe FLAG --myFlag
#>Flag is set

myapp.exe FLAG --myFlag true
#>Flag is set

myapp.exe FLAG
#>Flag is not set

myapp.exe FLAG false
#>Flag is not set
```

You can get the number of times this flag has been specified like this:

```csharp
[Alias("v")]
int Flag(OptionCount verbosityLevel)
{
	Console.WriteLine("Verbosity level: " + verbosityLevel);
	return 0;
}
```

This also works with short option names:
```bash
myapp.exe FLAG -vvvv
#>Verbosity level: 4
```

### Supported Parameter Types
* Primitive types (int, byte, char ...)
* BCL types (String, DateTime, Decimal)
* Nullable types
* Enum types, including flags
* Types with [TypeConverterAttribute](https://msdn.microsoft.com/en-us/library/system.componentmodel.typeconverterattribute(v=vs.110).aspx) (Point, Guid, Version, TimeSpan ...)
* Types with Parse(string value) method (IpAddress, Guid ...)
* Types with explicit/implicit conversion from string

## Values binding


# Verbs Hierarchy
For example you want to build a complex API where verbs are grouped by purpose. 

Example:
```bash
myfinance Account Show --id 0a0e0000000
```

Each group must be defined as method(verb) with **one** `ICommandLineBuilder` argument.
```csharp
public static int Account(ICommandLineBuilder subVerbBuilder)
{
	return subVerbBuilder.Use<AccountCommands>()
		.Run();
}

class AccountCommands
{
	public static int Show()
	{
		// ...
	}
}
```
where `AccountCommands` is class with list of sub-verbs related to `Account` verb. 

# Help text
Your command line application can generate help for the user. This requires to define *Help* method with following code inside
```csharp
public static int Help()
{
	return CommandLine.Describe<ConsoleApp>();
}
```
Testing:
```bash
myapp.exe Help
>	HELP
```
Not too much information :)

You can decorate the method with **DescriptionAttribute** attributes to expand 'Help' information.
```csharp
using System.ComponentModel;

[Description("Display this help.")]
public static int Help()
{
	return CommandLine.Describe<ConsoleApp>();
}
```
Testing:
```bash
myapp.exe Help
>	HELP - Display this help.
```
You can add these attributes to the methods, parameters and classes. All of them are involved in the generation of reference.

# Handling Errors
To catch and handle binding or execution errors you could subscribe on **CommandLineConfiguration.UnhandledExceptionHandler** method.
```csharp
	int Main(string[] arguments)
	{
		var exitCode = CommandLine
			.CreateFromArguments(arguments)
			.Configure(config =>
			{
				config.UnhandledExceptionHandler += (sender, args) => Console.WriteLine(args.Exception.ToString());
			})
			.Use<Program>()
			.Run();
		return exitCode;
	}
```
