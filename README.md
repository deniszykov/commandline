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

  public static int Hello(string name)
  {
    Console.WriteLine("Hello " + name + "!");
    return 0; // exit code
  }
}
```
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
* [Sub-Verbs](https://github.com/deniszykov/commandline/wiki/Sub_Verbs)
* [.NET Core hosting](https://github.com/deniszykov/commandline/wiki/NET_Core_Hosting)
* [Configuration](https://github.com/deniszykov/commandline/wiki/Configuration)
* [Help Text](https://github.com/deniszykov/commandline/wiki/Help_Text)
  * [Localization](https://github.com/deniszykov/commandline/wiki/Help_Text#Localization)

  License
  ============
  MIT
