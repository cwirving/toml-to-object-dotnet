using System.Text.Json.Nodes;
using DirectoryToObject;
using DirectoryToObject.Builder;
using Tomlyn;
using Tomlyn.Model;

namespace TomlToObject;

/// <summary>
///     Extensions to the directory-to-object <c>Handler</c> static class to add new TOML-related handlers.
/// </summary>
public static class HandlerExtensions
{
    extension(Handler)
    {
        /// <summary>
        ///     Handle all files as TOML using the Tomlyn library in DOM mode. Use the <c>WhenExtensionIsOneOf()</c> method
        ///     to narrow the handling to files with one or more specific extension. E.g.,
        ///     <c>Handler.JsonFile.WhenExtensionIsOneOf(".toml")</c>
        /// </summary>
        public static DirectoryLoaderBuilder TomlFile => Handler.NewFileLoader(ProcessTomlFileEntry);
    }

    /// <summary>
    ///     File processing function that loads it as TOML.
    /// </summary>
    /// <param name="directory">The enclosing directory.</param>
    /// <param name="entry">The file system entry to process.</param>
    /// <param name="relativePathInFileSystem">The directory's relative path in the file system.</param>
    /// <returns>A <see cref="ContextResponse" /> containing the parsed TOML converted to a <see cref="JsonNode" />.</returns>
    /// <exception cref="DirectoryLoadingException">If an internal error occurred while opening the file.</exception>
    private static async Task<ContextResponse> ProcessTomlFileEntry(
        IFileSystemDirectory directory,
        IFileSystemEntry entry,
        string relativePathInFileSystem)
    {
        using var streamOrDirectory = await directory.OpenDirectoryEntry(entry.Name).ConfigureAwait(false);
        if (streamOrDirectory.Stream is null)
            throw new DirectoryLoadingException($"Could not open stream for {entry.Name}");

        using var reader = new StreamReader(streamOrDirectory.Stream);
        var tomlString = await reader.ReadToEndAsync().ConfigureAwait(false);

        return new ContextResponse(Path.GetFileNameWithoutExtension(entry.Name),
            TomlSerializer.Deserialize<TomlTable>(tomlString).ToJsonObject());
    }
}