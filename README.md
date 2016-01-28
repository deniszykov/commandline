Introduction
============
Tools for command-line applications.

Installation
============
```
Install-Package System.CommandLine 
```

Example
============
```csharp	
	public class Program
	{
		[Browsable(false)] // hide it from CommandLine.Describe method
		public static int Main()
		{
			var result = CommandLine.Run<Program>(CommandLine.Arguments, defaultMethodName: "Help");
			Console.ReadKey();
			return result;
		}

		[Description("Say hello to specified 'name'.")]
		public static int Hello(string name)
		{
			Console.WriteLine("Hello " + name + "!");
			return 0;
		}

		public static int Math(CommandLineArguments arguments)
		{
			return CommandLine.Run<MathCommands>(arguments, "Help");
		}

		[Description("Display this help.")]
		public static int Help(string actionToDescribe = null)
		{
			return CommandLine.Describe<Program>(actionToDescribe);
		}
	}
	
	public class MathCommands
	{
		[Description("Adds two numbers and print result.")]
		public static int Add(int value1, int value2)
		{
			Console.WriteLine(value1 + value2);
			return 0;
		}

		[Description("Adds numbers and print result.")]
		public static int Add(int[] values)
		{
			Console.WriteLine(values.Sum());
			return 0;
		}

		[Description("Display this help.")]
		public static int Help(string actionToDescribe = null)
		{
			return CommandLine.Describe<Program>(actionToDescribe);
		}
	}
```	

```bash
#with positional parameter
myapp.exe Hello Mike 
#with named parameter
myapp.exe Hello --name Mike 

#deep actions
myapp.exe Math Add 1 1
#with named parameters
myapp.exe Math Add --value1 100 --value2 500
#method overloading and array binding
myapp.exe Math Add --values 100 200 300 400

#displaying help
myapp.exe Help
#displaying help for action
myapp.exe Help Math
```
