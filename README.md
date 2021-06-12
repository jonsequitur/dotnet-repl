# dotnet-repl

This project is an experiment using [.NET Interactive](https://github.com/dotnet/interactive) and [Spectre.Console](https://github.com/spectreconsole) to create a polyglot .NET REPL. 


### Code in C#:

You can start up `dotnet-repl` in one of a number of different language modes. The default is C#, so the following two commands are equivalent:

```console
> dotnet repl
> dotnet repl --default-kernel csharp
```

![image](https://user-images.githubusercontent.com/547415/121456759-68a85000-c95b-11eb-83a0-3b0010067e7b.png)

### Code in F#:

You can also start up the REPL in F# mode:

```console
> dotnet repl --default-kernel fsharp
```

![image](https://user-images.githubusercontent.com/547415/121456837-8d9cc300-c95b-11eb-9a91-1daae2dbc655.png)

### Submit multi-line entries

By pressing `Shift-Enter`, you can add multiple lines before running your code using `Enter`. This can be useful for creating multi-line code constructs, including declaring classes.

![image](https://user-images.githubusercontent.com/547415/121463971-dc505a00-c967-11eb-8a57-b976cc6b311b.png)

Another handy aspect of multi-line entries is that the F# Interactive convention of of terminating a line with `;;` to indicate that the accumulated submission should be run is no longer needed.



### Switch languages within the same session:

![image](https://user-images.githubusercontent.com/547415/121456913-ab6a2800-c95b-11eb-9a47-0f0828b2ba3b.png)



### Add NuGet packages:

You can use `#r nuget` to install a package for the duration of the current session.


### Run a notebook or start your REPL session using a notebook: 



### Ask for help:

You can see help for the REPL by running the `#!help` magic command.

### Keyboard shortcuts

Keybinding      | What it does                                                      |
----------------|-------------------------------------------------------------------|
`Enter`         | Submit and run the current code
`Shift+Enter`   | Inserts a newline without submitting the current code
`Tab`           | Show next completion
`Shift-Tab`     | Show previous completion
`Ctrl-Up`       | Go back through your submission history (current session only)
`Ctrl-Down`     | Go forward through your submission history (current session only)


### Magic commands

Because `dotnet-repl` is built on .NET Interactive, it supports "magic commands". You can recognize a magic command because it occurs at the start of a line and is prefixed by `#!`.

You can see the list of supported magic commands by running the `#!help` magic command.
