# dotnet-open

A .NET tool that finds and opens solution files in the current directory
or any specified path. It searches for `.sln` and `.slnx` files and opens
them with your default application. When multiple solutions are found, it
presents an interactive menu to choose which one to open.

## Install

```bash
dotnet tool install -g dotnet-open
```

## Use

Open a solution in the current directory:
```bash
dotnet open
```

Open a solution in a specific path:
```bash
dotnet open /path/to/directory
```

If multiple solutions are found, automatically open the first one:
```bash
dotnet open --first
```

Show version (of this tool):
```bash
dotnet open --version
```

## Upgrade

```bash
dotnet tool update -g dotnet-open
```

## Uninstall

```bash
dotnet tool uninstall -g dotnet-open
```
