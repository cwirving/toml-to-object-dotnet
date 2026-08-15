using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using DirectoryToObject;
using TomlToObject;

namespace toml_to_object.tests;

public class TomlFileTests
{
    private static string CodeDirectory => Path.GetDirectoryName(__file__()) ?? "";

    [Test]
    public async Task CanLoadDirectoryWithTomlFiles()
    {
        var loader = Loader.FromHandlerCollection(
        [
            Handler.TomlFile.WhenExtensionIsOneOf(".toml"),
            Handler.TextFile.WhenExtensionIsOneOf(".txt")
        ]);
        var allJsonFilesPath = Path.Combine(Path.Combine(CodeDirectory, "TestData"), "CompleteDirectory");

        var loadedObject = await loader.LoadFileSystemPathIntoJsonObjectAsync(allJsonFilesPath);
        await Assert.That(loadedObject).IsNotNull();

        var expected = JsonNode.Parse("""
                                      {
                                                "subdirectory": {
                                                  "another": "value",
                                                  "nested": {
                                                    "key": "value",
                                                    "key2": "value2"
                                                  }
                                                },
                                                "test": "This is a test!\n"
                                              }
                                      """);

        await Assert.That(JsonNode.DeepEquals(loadedObject, expected)).IsTrue();
    }

    private static string __file__([CallerFilePath] string path = "")
    {
        return path;
    }
}