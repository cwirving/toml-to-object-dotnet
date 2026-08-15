# toml-to-object

A simple extension for the [directory-to-object](https://www.nuget.org/packages/directory-to-object) library that
adds [TOML](https://toml.io) file parsing using the [Tomlyn](https://xoofx.github.io/Tomlyn/) TOML parsing library.

Since the library makes use of extension properties, this is package is available for only .NET 10 and above.

## Installation

```
dotnet add package toml-to-object
```

## Usage

This library extends the `Handlers` static class in `directory-to-object` with an additional `TomlFile` property in
addition to the existing `JsonFile` and `TextFile` builder properties.

```csharp
using DirectoryToObject;
using TomlToObject;

// Load the files with ".json", ".toml" and ".txt" extensions in the directory:
var loader = Loader.FromHandlerCollection(
[
    Handler.JsonFile.WhenExtensionIsOneOf(".json"),
    Handler.TomlFile.WhenExtensionIsOneOf(".toml"),
    Handler.TextFile.WhenExtensionIsOneOf(".txt")
]);

var loadedObject = await loader.LoadFileSystemPathIntoJsonObjectAsync(path_to_directory);
```
