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
#### Basics
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

Now you can test your application
```bash
myapp.exe SayHello 
#>Hello!
myapp.exe 
#>Hello! - Too because 'defaultCommandName' is set to 'SayHello'
```
#### Parameter bindings
You can add parameters to your command which is automatically binds by name or position
```csharp
public static int SayHello(string name)
{
	Console.WriteLine("Hello " + name + "!");
	return 0;
}
```
Testing positional and named parameters
```bash
myapp.exe SayHello Mike 
#>Hello Mike!
myapp.exe SayHello --name Jake
#>Hello Jake!
```
You can add array parameter to collect multiple values as shown below
```csharp
public static int SayHello(string[] names)
{
	Console.WriteLine("Hello " + string.Join(", ", names) + "!");
	return 0;
}
```
Testing array parameter
```bash
myapp.exe SayHello --names Mike Jake
#>Hello Mike, Jake!
```
Unfortunately an array parameter can be only named, I will revisit it in future :)

You can have a flag(true/false) parameter. It's presence is considered to be "True" and absence is "False".
```csharp
public static int ShowFlag(bool myFlag)
{
	Console.WriteLine(myFlag ? "Flag is set" : "Flag is not set");
	return 0;
}
```
Testing flag parameter
```bash
myapp.exe ShowFlag --myFlag
#>Flag is set
myapp.exe ShowFlag
#>Flag is not set
```

####Hierarchical commands
Suppose you want to build a complex menu where commands are grouped by purpose.

Example:
```bash
myapp.exe Account Show --id 0a0e0000000
```

Each group must be defined as command with one **CommandLineArguments** argument.
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
Where AccountCommands is class with list of commands as described in "Basics". 

####Generating help page
Your console application can generate help for the user. This requires to define *Help* method with following code inside
```csharp
public static int Help()
{
	return CommandLine.Describe<ConsoleApp>();
}
```
Testing help
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
Testing help with custom description
```bash
myapp.exe Help
>	HELP - Display this help.
```
You can add these attributes to the methods, parameters and classes. All of them are involved in the generation of reference.
