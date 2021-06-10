# dotnet-repl

This project is an experiment using [.NET Interactive](https://github.com/dotnet/interactive) and [Spectre.Console](https://github.com/spectreconsole) to create a polyglot .NET REPL. 


### Code in C#:

```console
> dotnet repl --default-kernel csharp
```

![image](https://user-images.githubusercontent.com/547415/121456759-68a85000-c95b-11eb-83a0-3b0010067e7b.png)


### Code in F#:



```console
> dotnet repl --default-kernel fsharp
```


![image](https://user-images.githubusercontent.com/547415/121456837-8d9cc300-c95b-11eb-9a91-1daae2dbc655.png)

### Submit multi-line entries





### Switch languages within the same session:


![image](https://user-images.githubusercontent.com/547415/121456913-ab6a2800-c95b-11eb-9a47-0f0828b2ba3b.png)










### Add NuGet packages:



### Run a notebook or start your REPL session using a notebook: 



### Ask for help:






### Keyboard shortcuts

Keybinding      | What it does                                                      |
----------------|-------------------------------------------------------------------|
`Shift+Enter`   | Inserts a newline without submitting the current code
`Tab`           | Show next completion
`Shift-Tab`     | Show previous completion
`Ctrl-Up`       | Go back through your submission history (current session only)
`Ctrl-Down`     | Go forward through your submission history (current session only)


### Magic commands

Because `dotnet-repl` is built on .NET Interactive, it supports "magic commands".
