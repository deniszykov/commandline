Introduction
============
Tools for building console application.

Installation
============
```
Install-Package ConsoleApp.CommandLine
```

Quick Start
============
# Basics
To start, you need to configure the entry point to the application. Where "ConsoleApp" will be your class with a command handler.
```csharp
using System

class Program
{
	public static int Main()
	{
		return CommandLine.Run<Program>(CommandLine.Arguments, defaultCommandName: "SayHello")
	}	
	public static int SayHello()
	{
		Console.WriteLine("Hello!");
		return 0;
	}
}
```
CommandLine.Run relies on reflection to find methods. So they should be **static** and return **int** which is [Exit Code](https://en.wikipedia.org/wiki/Exit_status)

Now you can test your application:
```bash
myapp.exe SayHello 
#>Hello!
myapp.exe SAYHELLO
#>Hello! -- command name is case-insensitive (parameters are not!)
myapp.exe 
#>Hello! - Too because 'defaultCommandName' is set to 'SayHello'
```
## Parameter bindings
### Positional and named parameters
You can add parameters to your command which is automatically binds by name or position
```csharp
public static int SayHello(string name)
{
	Console.WriteLine("Hello " + name + "!");
	return 0;
}
```
Testing:
```bash
myapp.exe SayHello Mike 
#>Hello Mike!
myapp.exe SayHello --name Jake
#>Hello Jake!
```

### List  parameters
You can add array parameter to collect multiple values as shown below:
```csharp
public static int SayHello(string[] names)
{
	Console.WriteLine("Hello " + string.Join(", ", names) + "!");
	return 0;
}
```
Testing:
```bash
myapp.exe SayHello --names Mike Jake
#>Hello Mike, Jake!
```
Parameter accepting multiple values can be only named.

### Optional parameters
You can make any parameter optional by specifying default value.
```csharp
public static int ShowOptionalParameter(int myOptionalParam = 100)
{
	Console.WriteLine("My optional parameter is " + myOptionalParam);
	return 0;
}
```
Testing:
```bash
myapp.exe ShowOptionalParameter --myOptionalParam 200
#>My optional parameter is 200
myapp.exe ShowOptionalParameter 300
#>My optional parameter is 300
myapp.exe ShowOptionalParameter
#>My optional parameter is 100
```

### Flag parameters
You can have a flag(true/false) parameter. It's presence is considered to be "True" and absence is "False".
```csharp
public static int ShowFlag(bool myFlag)
{
	Console.WriteLine(myFlag ? "Flag is set" : "Flag is not set");
	return 0;
}
```
Testing:
```bash
myapp.exe ShowFlag --myFlag
#>Flag is set
myapp.exe ShowFlag
#>Flag is not set
```
### Supported Parameter Types
* Primitive types (int, byte, char ...)
* BCL types (String, DateTime, Decimal)
* Nullable types
* Enum types
* Types with [TypeConverterAttribute](https://msdn.microsoft.com/en-us/library/system.componentmodel.typeconverterattribute(v=vs.110).aspx) (Point, Guid, Version, TimeSpan ...)
* Types with Parse(string value) method (IpAddress, Guid ...)
* Types with explicit/implicit conversion from string

# Сommands Hierarchy
Suppose you want to build a complex API where commands are grouped by purpose. 

Example:
```bash
myfinance.exe Account Show --id 0a0e0000000
```

Each group must be defined as method(command) with **one** CommandLineArguments argument.
```csharp
public static int Account(CommandLineArguments arguments)
{
	return CommandLine.Run<AccountCommands>(arguments);
}

class AccountCommands
{
	public static int Show()
	{
		// ...
	}
}
```
where AccountCommands is class with list of commands as described in "Basics". 

# Commands Description
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
To catch and handle binding or execution errors you could subscribe on **CommandLine.UnhandledException** method.
```csharp
CommandLine.UnhandledException += (sender, args) => Console.WriteLine(args.ExceptionObject.ToString());
```
