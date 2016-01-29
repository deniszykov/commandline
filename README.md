Introduction
============
Tools for building console application.

Installation
============
```
Install-Package System.CommandLine 
```

Quick Start
============
#### Basics
To start, you need to configure the entry point to the application. Where "ConsoleApp" will be your class with a command handler.
```csharp	
		public static int Main()
		{
			CommandLine.Run<ConsoleApp>(CommandLine.Arguments, defaultMethodName: "SayHello")
		}
```
Then define class "ConsoleApp" as shown below
```csharp	
class ConsoleApp
{
	public static int SayHello()
	{
		Console.WriteLine("Hello!");
		return 0;
	}
}
```
CommandLine.Run uses reflection to find command methods. So they should be **static** and return **int** [Exit Code](https://en.wikipedia.org/wiki/Exit_status)

Now you can test your application
```bash
myapp.exe SayHello 
#>Hello!
myapp.exe 
#>Hello! too because 'defaultMethodName' is set to 'SayHello'
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
#>Hello Jake!
```
Unfortunately an array parameter can be only named, I will revisit it in future :)

You can have a flag parameter also. It's presence is considered to be "True" and absence is "False".
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

Each group must be defined as command with one **CommandLineArguments** argument.
```csharp
public static int Account(CommandLineArguments arguments)
{
	return CommandLine.Run<AccountCommands>(arguments);
}
public static int Product(CommandLineArguments arguments)
{
	return CommandLine.Run<ProductCommands>(arguments);
}
```
Where AccountCommands is class with list of commands as described in "Basics". 

Testing Hierarchical commands
```bash
myapp.exe Account Show --id 0a0e0000000
```

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
