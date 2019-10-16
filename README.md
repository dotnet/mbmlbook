# MBML Book Sample Code

Supporting code for the [Model-Based Machine Learning](http://mbmlbook.com/) book.

This project contains the sample code and test data for the freely-available book on model-based machine learning at http://mbmlbook.com/. The book introduces readers to how to build machine learning models for real-world problems. Each chapter tackles a different problem by defining a statistical model and demonstrating how to apply it to actual data. This repository contains the source code for these models together with the sample data - so that readers can run it on their own, re-create the results from the book and experiment with modifying the code.

Models here are created and applied using the [Infer.NET](https://github.com/dotnet/infer) framework.
When targeting .NET Framework/Windows code produces visualizations using [Glo](GLO.md).

## Contents

- [Structure of Repository](#structure-of-repository)
- [Building Sample Code](#building-sample-code)
- [Contributing](#contributing)
- [License](#license)
- [.NET Foundation](#.net-foundation)

## Structure of Repository

* The Visual Studio solution `Samples.sln` in the root of the repository contains a project with models for every chapter of the book and a few projects with the shared code and/or unit tests from the folders described below. Code layout in projects corresponding to the book chapters is similar and expanded only once.

* src/
    * 1\. A Murder Mystery - code by Thomas Diethe and Dmitry Kats/
    * 2\. Assessing People's Skills - code by Thomas Diethe and Dmitry Kats/
        * Data - input data in ObjML format
        * DataObjects - .NET types to hold the input data
        * Experiment - types that facilitate experimentation: types for metrics, results, and, possibly, other experiment-related data structures; a type that represents a single experiment and exposes a method to run it, type(s) that store multiple experiments together for comparison, etc.
        * Models - .NET types for [Infer.NET](https://github.com/dotnet/infer) models. Have methods to construct a model and to run inference on it.
        * Views - additional [Glo](GLO.md) views needed for specific chapter. Not included in compilation when targeting .Net Core.
        * Contents.cs - static type that has constants for the name of the chapter and all the sections within the chapter
        * Program.cs
    * 3\. Meeting Your Match - code by Thomas Diethe and Alexander Novikov
        * This chapter and the remaining chapters have similar layout to chapter 2.
    * 4\. Uncluttering Your Inbox - code by Thomas Diethe and Dmitry Kats
    * 5\. Making Recommendations - code by Yordan Zaykov and Alexander Novikov
    * 6\. Understanding Asthma - code by John Guiver and Dmitry Kats
    * 7\. Harnessing the Crowd - code by John Guiver and Dmitry Kats
    * MBMLCommon/ -  a number of utility classes shared by different projects including
        * Outputter.cs - class that encapsulates outputting objects, so that the code for chapters can output results in a platform-agnostic way. Also exposes methods for saving outputs to .objml files. 
    * MBMLViews - additional [Glo](GLO.md) views shared among the chapters.
* test/ - projects with unit tests

## Building Sample Code

Please, refer to our [building guide](BUILDING.md).

## Contributing

We welcome contributions! Please review our [contribution guide](CONTRIBUTING.md).

## License

MBML Book Sample Code is licensed under the [MIT license](LICENSE).

## .NET Foundation

MBML Book Sample Code is a [.NET Foundation](https://www.dotnetfoundation.org/projects) project.
It relies on [Infer.NET](https://github.com/dotnet/infer) framework.

There are many .NET related projects on GitHub.

- [.NET home repo](https://github.com/Microsoft/dotnet) - links to 100s of .NET projects, from Microsoft and the community.