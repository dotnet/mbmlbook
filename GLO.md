# Glo

*Good-Looking Objects*

**Glo** lets you browse through a set of objects as if you were using a web browser. You can see multiple views of each object, do interactive scripting and also search and filter the data using LINQ.

To browse objects in your programs,
install `Microsoft.Research.Glo` nuget package to your .NET Framework project.
 
You can now show objects in Glo with one line:
```csharp
Microsoft.Research.Glo.GloBrowser.Show("MyDataObject", MyDataObject );
```
For example:
```csharp
string[] tests = "Hello world, how are you".Split(' ');
Microsoft.Research.Glo.GloBrowser.Show("tests", tests);
```

Now you can start exploring the objects loaded in Glo. If you are running Glo standalone then you will see a number of example objects.

Want to know more? Read the built-in documentation.

## GloObject

**GloObject** is a component of **Glo** that supports not only .NET Framework, but .NET Core and .NET Standard as well and is responsible for the serialization and deserialization of CLR objects in/from xml-like `.objml` format.

To use it, install `Microsoft.Research.GloObject` nuget package to your .NET Standard/.NET Framework/.NET Core project. After that, you will be able to use
 ```csharp
 void Microsoft.Research.Glo.SerializationManager.Save(string filename, object obj)
 ```
 and
 ```csharp
object Microsoft.Research.Glo.SerializationManager.Load(string filename, out List<Exception> errors)
 ```
 methods.