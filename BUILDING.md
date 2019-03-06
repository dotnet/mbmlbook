# Building Sample Code

- [Building with Visual Studio 2017](#building-with-visual-studio-2017)
- [Building from the command line](#building-from-the-command-line)

First, you must clone the repository.
Next decide whether you want to use a code editor like Visual Studio (recommended) or the command line.


## Building with Visual Studio 2017

1. If you don't have Visual Studio 2017, you can install the free [Visual Studio 2017 Community](https://visualstudio.microsoft.com/vs/community/).
1. Start Visual Studio.
1. Select `File -> Open -> Project/Solution` and open the `Samples.sln` solution file located in your cloned repository.
1. Compile using `Build -> Build Solution`.
1. To run models from a certain chapter, right-click on a correspondingly named project, select `Set as a StartUp Project`, then press `Ctrl+F5` to start it.

## Building from the command line

Provided sample code runs, albeit without visualizing results, on .NET Core 2.1. You can build and run it using the command line as follows.

### Prerequisites

* **[.NET Core 2.1 SDK](https://www.microsoft.com/net/download/)** to build and run projects targeting .NET Core.

* (Optional) On Windows, the **[.NET framework developer pack](https://www.microsoft.com/net/download)**. Provided visualizations are viewable only when targeting .NET Framework.

### Build 

Navigate to the directory with the project you want to build and run

```
dotnet build -f netcoreapp2.1
```
The `-f netcoreapp2.1` sets the target framework to .NET Core 2.1. By default all the projects target both .NET Core 2.1 and .NET Framework 4.6.1, which results in error on systems where the latter is not present.

### Run

Either navigate to the directory with the project and run

```
dotnet run -f netcoreapp2.1 ./output
```
Or build the project, navigate to the directory with the binaries and run
```
dotnet project.dll ./output
```
Here `project.dll` should be replaced with the name of actual executable; `./output` is the folder where the output produced by the program should be stored.