# dotnet-repl

![REPL preview in C# and F#](https://user-images.githubusercontent.com/547415/121978422-02497600-cd3d-11eb-96c6-9725bda6dcaa.png)

This project is an experiment using [.NET Interactive](https://github.com/dotnet/interactive) and [Spectre.Console](https://github.com/spectreconsole) to create a polyglot .NET REPL for use on the command line. 

This is a personal project. Hopefully you enjoy it and find it useful. Contributions are welcome. 

[![NuGet Status](https://img.shields.io/nuget/v/dotnet-repl.svg?style=flat)](https://www.nuget.org/packages/dotnet-repl/) 
[![Build status](https://ci.appveyor.com/api/projects/status/j544mv4bxysjryru?svg=true)](https://ci.appveyor.com/project/jonsequitur/dotnet-repl)

## Installation

To install `dotnet-repl`, run the following in your terminal:

```console
> dotnet tool install -g dotnet-repl
```

## Features

Here's what you can do with it:

### *Code in C#*

You can start `dotnet-repl` in one of a number of different language modes. The default is C#, so the following two commands are equivalent:

```console
> dotnet repl --default-kernel csharp
> dotnet repl
```

Once the REPL has started, you can type C# code at the prompt and run it by pressing `Enter`. (Note that this is the [C# scripting](https://docs.microsoft.com/en-us/archive/msdn-magazine/2016/january/essential-net-csharp-scripting) dialect, which is also used in Visual Studio's C# Interactive Window and in .NET Interactive Notebooks.)

<img src="https://user-images.githubusercontent.com/547415/121456759-68a85000-c95b-11eb-83a0-3b0010067e7b.png" width="60%" />

One notable feature of C# scripting is the ability to specify a return value for a code submission using a "trailing expression":

<img src="https://user-images.githubusercontent.com/547415/121977410-d0cfab00-cd3a-11eb-84a0-ab4f8889c9c7.png" width="60%" />

### *Code in F#*

You can also start up the REPL in F# mode with `--default-kernel` or set the environment variable `DOTNET_REPL_DEFAULT_KERNEL` to `fsharp`:

```console
> dotnet repl --default-kernel fsharp
```
```console
# DOTNET_REPL_DEFAULT_KERNEL=fsharp
> dotnet repl
```

<img src="https://user-images.githubusercontent.com/547415/121456837-8d9cc300-c95b-11eb-9a91-1daae2dbc655.png" width="60%" />

### 📝 *Submit multi-line entries*

By pressing `Shift-Enter`, you can add multiple lines before running your code using `Enter`. This can be useful for creating multi-line code constructs, including declaring classes.

<img src="https://user-images.githubusercontent.com/547415/121463971-dc505a00-c967-11eb-8a57-b976cc6b311b.png" width="60%" />

Another handy aspect of multi-line entries is that you no longer need to use the the F# Interactive convention of terminating a line with `;;` to indicate that the accumulated submission should be run. Pressing `Enter` will submit the code, and if you need more than one line of code at a time, you can use `Shift-Enter` to add lines before submitting.

<img src="https://user-images.githubusercontent.com/547415/121977822-b5b16b00-cd3b-11eb-90d6-2798289a47d5.png" width="60%" />

### 🚥 *Switch languages within the same session*

<img src="https://user-images.githubusercontent.com/547415/121456913-ab6a2800-c95b-11eb-9a47-0f0828b2ba3b.png" width="60%" />

### 🎁 *Add NuGet packages*

You can use `#r nuget` to install a package for the duration of the current session.

<img src="https://user-images.githubusercontent.com/547415/121978012-235d9700-cd3c-11eb-89d0-ba367089208c.gif" width="60%" />


### *Initialize your REPL session using a notebook*

You can use a notebook file (either `.ipynb` or `.dib`) as an initialization script for the REPL.

```console
> dotnet repl --notebook /path/to/notebook.ipynb
```

<img src="https://user-images.githubusercontent.com/547415/121982282-13e24c00-cd44-11eb-9c00-b0e04bb18276.gif" width="60%" />

### *Run a notebook as a script*

You might also want to just use a notebook as a non-interactive script. You can do this by adding the `--exit-after-run` flag.

```console
> dotnet repl --notebook /path/to/notebook.ipynb --exit-after-run
```

Both `.ipynb` and `.dib` files are supported.

If all of the notebook's cells execute successfully, a `0` exit code is returned. Otherwise, `1` is returned. This can be used as a 
way to test notebooks. 

<img width="60%" alt="image" src="https://user-images.githubusercontent.com/547415/176486922-8db22f68-3198-4a5f-bdf7-398805b9f295.png">

If you redirect output when using `--exit-after-run`, the output will be formatted using the `.ipynb` JSON format, allowing you to rerun the code or view the results in a notebook editor.

### 💁‍♀️ *Ask for help*

You can see help for the REPL by running the `#!help` magic command. I won't print it here because it's a work in progress. Just give it a try.

### ⌨ *Keyboard shortcuts*

`dotnet-repl` supports a number of keyboard shortcuts. These will evolve over time but for now, here they are:

Keybinding      | What it does                                                      |
----------------|-------------------------------------------------------------------|
`Enter`         | Submit and run the current code
`Shift+Enter`   | Inserts a newline without submitting the current code
`Tab`           | Show next completion
`Shift+Tab`     | Show previous completion
`Ctrl+C`        | Exit the REPL
`Ctrl+Up`       | Go back through your submission history (current session only)
`Ctrl+Down`     | Go forward through your submission history (current session only)


### 🧙‍♂️ *Magic commands*

Because `dotnet-repl` is built on .NET Interactive, it supports "magic commands". You can recognize a magic command by the `#!` at the start of a line.

You can see the list of supported magic commands by running the `#!help` magic command.
