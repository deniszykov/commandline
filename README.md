[![dotnet_build](https://github.com/deniszykov/commandline/actions/workflows/dotnet_build.yml/badge.svg)](https://github.com/deniszykov/commandline/actions/workflows/dotnet_build.yml)
[![NuGet](https://img.shields.io/nuget/v/deniszykov.CommandLine.svg)](https://www.nuget.org/packages/deniszykov.CommandLine)

Introduction
============
A simple [getopt](https://man7.org/linux/man-pages/man3/getopt.3.html) styled command line library for .NET.

Features
--------
* `getopt`-style syntax: short (`-n`), long (`--name`), Windows-style (`/n`, `/name`), `--name=value` and bundled short options.
* Verbs and sub-verbs discovered from your classes via reflection — no schema, no codegen.
* Synchronous and asynchronous entry points (`int` or `Task<int>` return type).
* Built-in help (`/?`, `-h`, `--help`) generated from `[Description]` / `[HelpText]` attributes.
* Cancellation via `CancellationToken` (also driven by `Console.CancelKeyPress`).
* Service injection into verb parameters via any `IServiceProvider` (works with `Microsoft.Extensions.DependencyInjection`).
* First-class integration with the .NET Generic Host (`IHostBuilder.ConfigureCommandLineHost`).
* Localizable help text via `IHelpTextProvider`.
* Configurable name matching (case-sensitive/insensitive), prefixes, separators and help options.
* Multi-targeted: .NET Framework 4.5+, .NET Standard 1.6 / 2.1, .NET Core 3.1, .NET 6, .NET 10.

Installation
============
```
Install-Package deniszykov.CommandLine
```
[.NET Framework / .NET Core Example](https://github.com/deniszykov/commandline/blob/master/src/deniszykov.CommandLine.Example/Program.cs)

For .NET Generic Host environment:
```
Install-Package deniszykov.CommandLine.Hosted
```
[.NET Generic Host Example](https://github.com/deniszykov/commandline/blob/master/src/deniszykov.CommandLine.Hosted.Example/Program.cs)

Quick Start
============

To start, you need to configure the entry point to the application: 
```csharp
public class Program
{

  private static int Main(string[] arguments) =>
    CommandLine
      .CreateFromArguments(arguments)
      .Use<Program>() // set class with verbs/commands
      .Run();

  //
  // Usage: myapp.exe hello --name <name>
  // 
  public static int Hello(string name)
  //                  ^            ^
  //                 Verb        Option
  {
    Console.WriteLine("Hello " + name + "!");
    return 0; // exit code
  }  

}
```
[Example Code](https://github.com/deniszykov/commandline/blob/master/src/deniszykov.CommandLine.Example/Program.cs) / [.NET Hosted Example Code](https://github.com/deniszykov/commandline/blob/master/src/deniszykov.CommandLine.Hosted.Example/Program.cs)

`CommandLine` relies on reflection to find method to invoke.  
This method should return `int` value which is interpreted as [Exit Code](https://en.wikipedia.org/wiki/Exit_status) of application.  
Asynchronous entry points and methods are also supported. To do this, use method `RunAsync()` and `Task<int>` as return type.  

When you could request help for your application:  
```console
> myapp /?

This test application. Type /? for help.

  Verbs:
    HELLO    Says hello to specified 'name'.
```

Or invoke `Hello(string name)` with following command:
```console
> myapp hello --name Jake

Hello Jake!
```

Async Entry Point
-----------------
```csharp
public class Program
{
  private static Task<int> Main(string[] arguments) =>
    CommandLine
      .CreateFromArguments(arguments)
      .Use<Program>()
      .RunAsync(CancellationToken.None);

  public static async Task<int> Hello(string name, CancellationToken cancellation)
  {
    await Task.Delay(100, cancellation);
    Console.WriteLine("Hello " + name + "!");
    return 0;
  }
}
```

.NET Generic Host
-----------------
```csharp
await Host.CreateDefaultBuilder(args)
  .ConfigureServices((ctx, services) =>
  {
    services.Configure<CommandLineConfiguration>(ctx.Configuration.GetSection("CommandLine"));
  })
  .ConfigureCommandLineHost(args, builder =>
  {
    builder.Use<Program>();
  })
  .Build()
  .RunAsync();
```
The exit code returned by the executed verb is written to `Environment.ExitCode` automatically.

Documentation
============
* [Syntax](https://github.com/deniszykov/commandline/wiki/Syntax)
  * [Verbs](https://github.com/deniszykov/commandline/wiki/Syntax#Verbs)
  * [Sub-verbs](https://github.com/deniszykov/commandline/wiki/Syntax#Sub-verbs)
  * [Options](https://github.com/deniszykov/commandline/wiki/Syntax#Options)
  * [Values](https://github.com/deniszykov/commandline/wiki/Syntax#Values)
* [Binding](https://github.com/deniszykov/commandline/wiki/Binding)
  * [Verbs binding](https://github.com/deniszykov/commandline/wiki/Binding#Verbs-binding)
  * [Options binding](https://github.com/deniszykov/commandline/wiki/Binding#Options-binding)
  * [Values binding](https://github.com/deniszykov/commandline/wiki/Binding#Values-binding)
  * [Service resolution and DI](https://github.com/deniszykov/commandline/wiki/Binding#Service-resolution-and-DI)
  * [Cancellation](https://github.com/deniszykov/commandline/wiki/Binding#Cancellation)
  * [Supported .NET types](https://github.com/deniszykov/commandline/wiki/Binding#Supported-NET-types)
  * [Multiple verb classes](https://github.com/deniszykov/commandline/wiki/Binding#Multiple-verb-classes)
* [Sub-Verbs](https://github.com/deniszykov/commandline/wiki/Sub_Verbs)
* [.NET Core hosting](https://github.com/deniszykov/commandline/wiki/NET_Core_Hosting)
* [Configuration](https://github.com/deniszykov/commandline/wiki/Configuration)
  * [Settings reference](https://github.com/deniszykov/commandline/wiki/Configuration#Settings-reference)
  * [Unknown options](https://github.com/deniszykov/commandline/wiki/Configuration#Unknown-options)
  * [Error handling and exit codes](https://github.com/deniszykov/commandline/wiki/Configuration#Error-handling)
* [Help Text](https://github.com/deniszykov/commandline/wiki/Help_Text)
  * [Localization](https://github.com/deniszykov/commandline/wiki/Help_Text#Localization)
* [Extensibility](https://github.com/deniszykov/commandline/wiki/Extensibility)

License
============
[MIT](https://github.com/deniszykov/commandline/blob/master/LICENSE)
