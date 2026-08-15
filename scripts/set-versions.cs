#!/usr/bin/env dotnet run --file

// Script to set the version properties in MSBuild project or props files based on
// information passed on the command line or build pipeline environment variables.

#:package System.CommandLine@2.0.10
#:package semver@3.0.0

using System.CommandLine;
using System.Xml.Linq;
using Semver;


var rootCommand = new RootCommand("Set MSBuild project version properties");

rootCommand.Arguments.Add(Globals.FilesArgument);
rootCommand.Options.Add(Globals.VersionSourceOption);
rootCommand.Options.Add(Globals.BuildNumberSourceOption);
rootCommand.Options.Add(Globals.VersionOption);
rootCommand.Options.Add(Globals.VersionSourceOption);
rootCommand.Options.Add(Globals.BuildNumberOption);
rootCommand.Options.Add(Globals.BuildNumberSourceOption);

var parseResult = rootCommand.Parse(args);
if (parseResult.Errors.Count > 0)
{
    foreach (var error in parseResult.Errors)
    {
        Console.Error.WriteLine(error.Message);
    }

    return 1;
}

try
{
    return RootAction(parseResult);
}
catch (ParameterException e)
{
    Console.Error.WriteLine(e.Message);
    Console.Error.WriteLine();
    rootCommand.Parse("--help").Invoke();
    return 1;
}
catch (Exception e)
{
    Console.WriteLine(e.Message);
    return 1;
}

static int RootAction(ParseResult parseResult)
{

    var fileInfos = parseResult.GetValue(Globals.FilesArgument);
    if (fileInfos is null || fileInfos.Length == 0)
    {
        throw new ParameterException("No MSBuild project files specified.");
    }
    
    var version = GetVersionBasedOnOptions(parseResult);
    var buildNumber = GetBuildNumberBasedOnOptions(parseResult);
    
    if (version == Globals.NoVersionSpecified)
    {
        throw new ParameterException("No version specified. A version is required.");
    }

    foreach (var fileInfo in fileInfos)
    {
        if (!fileInfo.Exists)
        {
            Console.Error.WriteLine($"File '{fileInfo.FullName}' does not exist.");
            break;
        }
        
        ProcessOneFile(fileInfo.FullName, version, buildNumber);
    }
    
    return 0;
}

static void ProcessOneFile(string filePath, SemVersion version, int buildNumber)
{
    var propsDocument = XDocument.Load(filePath);

    var projectElement = propsDocument.Element("Project");
    
    if(projectElement is not null)
    {
        SetProjectElementVersions(projectElement, version, buildNumber);

        propsDocument.Save(filePath);
    }
    else
    {
        Console.Error.WriteLine($"File '{filePath}' does not contain a 'Project' element.");
    }
}

static void SetProjectElementVersions(XElement projectElement, SemVersion version, int buildNumber)
{
    foreach(var childElement in projectElement.Elements())
    {
        if(childElement.Name == "PropertyGroup")
        {
            foreach(var propertyElement in childElement.Elements())
            {
                switch (propertyElement.Name.LocalName)
                {
                    case "AssemblyVersion":
                        // The assembly version is always the major version, to avoid binding woes.
                        propertyElement.Value = $"{version.Major}.0.0.0";
                        break;

                    case "FileVersion":
                        propertyElement.Value = $"{version.Major}.{version.Minor}.{buildNumber}.{version.Patch}";
                        break;

                    case "Version":
                        propertyElement.Value = version.ToString();
                        break;
                }
            }
        }
    }
}

static SemVersion GetVersionBasedOnOptions(ParseResult parseResult)
{
    var maybeVersionString = parseResult.GetValue(Globals.VersionOption);
    if (!string.IsNullOrEmpty(maybeVersionString))
    {
        return SemVersion.Parse(maybeVersionString, SemVersionStyles.OptionalPatch);
    }
    
    var maybeVersionSource = parseResult.GetValue(Globals.VersionSourceOption);

    return maybeVersionSource switch
    {
        InformationSource.Explicit => Globals.NoVersionSpecified,
        InformationSource.AzureDevOps => GetVersionFromAzureDevOpsEnvironment(),
        InformationSource.GitHub => GetVersionFromGitHubEnvironment(),
        _ => Globals.NoVersionSpecified
    };
}

static SemVersion GetVersionFromGitHubEnvironment()
{
    const string branchTagPrefix = "refs/tags/";
    
    var maybeSourceBranch = Environment.GetEnvironmentVariable("GITHUB_REF");
    if(string.IsNullOrEmpty(maybeSourceBranch)) throw new Exception($"GitHub environment variable 'GITHUB_REF' environment variable is missing.");

    if (maybeSourceBranch.StartsWith(branchTagPrefix))
    {
        maybeSourceBranch = maybeSourceBranch.Substring(branchTagPrefix.Length);
        return SemVersion.TryParse(maybeSourceBranch, SemVersionStyles.OptionalPatch, out var version) ? version : throw new Exception("The name of the tag in the 'GITHUB_REF' environment variable is not a valid semantic version.");
    }
    
    throw new Exception("Unsupported GitHub source branch tag.");
}

static SemVersion GetVersionFromAzureDevOpsEnvironment()
{
    const string branchTagPrefix = "refs/tags/";
    
    var maybeSourceBranch = Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCH");
    if(string.IsNullOrEmpty(maybeSourceBranch)) throw new Exception($"Azure DevOps environment variable 'BUILD_SOURCEBRANCH' environment variable is missing.");

    if (maybeSourceBranch.StartsWith(branchTagPrefix))
    {
        maybeSourceBranch = maybeSourceBranch.Substring(branchTagPrefix.Length);
        return SemVersion.TryParse(maybeSourceBranch, SemVersionStyles.OptionalPatch, out var version) ? version : throw new Exception("The name of the tag in the 'BUILD_SOURCEBRANCH' environment variable is not a valid semantic version.");
    }
    
    throw new Exception("Unsupported Azure DevOps source branch tag.");
}

static int GetBuildNumberBasedOnOptions(ParseResult parseResult)
{
    var maybeBuildNumber = parseResult.GetValue(Globals.BuildNumberOption);
    if (maybeBuildNumber >= 0)
    {
        return maybeBuildNumber;
    }
    
    var maybeBuildNumberSource = parseResult.GetValue(Globals.BuildNumberSourceOption);

    return maybeBuildNumberSource switch
    {
        InformationSource.Explicit => 0,
        InformationSource.AzureDevOps => GetBuildNumberFromAzureDevOpsEnvironment(),
        InformationSource.GitHub => GetBuildNumberFromGitHubEnvironment(),
        _ => 0
    };
}

static int GetBuildNumberFromGitHubEnvironment()
{
    var maybeBuildNumberString = Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER");
    if(string.IsNullOrEmpty(maybeBuildNumberString)) throw new Exception($"GitHub environment variable 'GITHUB_RUN_NUMBER' environment variable is missing.");

    return int.TryParse(maybeBuildNumberString, out var result)
        ? result
        : throw new Exception("The contents of the 'GITHUB_RUN_NUMBER' environment variable are not a number.");
}

static int GetBuildNumberFromAzureDevOpsEnvironment()
{
    var maybeBuildNumberString = Environment.GetEnvironmentVariable("BUILD_BUILDID");
    if(string.IsNullOrEmpty(maybeBuildNumberString)) throw new Exception($"Azure DevOps environment variable 'BUILD_BUILDID' environment variable is missing.");

    return int.TryParse(maybeBuildNumberString, out var result)
        ? result
        : throw new Exception("The contents of the 'BUILD_BUILDID' environment variable are not a number.");
}

public class ParameterException(string message) : Exception(message);

public enum InformationSource
{
    Explicit,
    GitHub,
    AzureDevOps
}

public static class Globals
{
    public static readonly SemVersion NoVersionSpecified = new SemVersion(long.MaxValue);
    
    public static readonly Argument<FileInfo[]> FilesArgument = new Argument<FileInfo[]>("files")
    {
        Arity = ArgumentArity.ZeroOrMore,
    };

    public static readonly Option<InformationSource> VersionSourceOption = new("--version-source")
    {
        Aliases = { "-s" },
        Arity = ArgumentArity.ZeroOrOne,
        Description = "Source of the version information if not specified via the '--version' option."
    };

    public static readonly Option<InformationSource> BuildNumberSourceOption = new("--build-number-source")
    {
        Aliases = { "-n" },
        Arity = ArgumentArity.ZeroOrOne,
        Description = "Source of the build number information if not specified via the '--build-number' option."
    };

    public static readonly Option<string> VersionOption = new("--version")
    {
        Aliases = { "-v" },
        Arity = ArgumentArity.ZeroOrOne,
        Description = "The version to use, overrides the --version-source option."
    };
    
    
    public static readonly Option<int> BuildNumberOption = new("--build-number")
    {
        Aliases = { "-b" },
        Arity = ArgumentArity.ZeroOrOne,
        DefaultValueFactory = (_) => -1,
        Description = "The build number to use, overrides the --build-number-source option."
    };
}

