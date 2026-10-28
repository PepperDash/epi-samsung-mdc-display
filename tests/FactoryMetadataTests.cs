using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace PepperDashPluginSamsungMdcDisplay.Tests;

public class FactoryMetadataTests
{
    private static readonly Lazy<string> FactorySourceContent = new(() =>
    {
        var factoryFile = Path.Combine(AssemblyFixture.SourceDirectory, "SamsungMdcControllerFactory.cs");
        return File.ReadAllText(factoryFile);
    });

    [Fact]
    public void Factory_Sets_MinimumEssentialsFrameworkVersion_To_3_0_0()
    {
        var content = FactorySourceContent.Value;

        var match = Regex.Match(content, @"MinimumEssentialsFrameworkVersion\s*=\s*""([^""]*)""");
        match.Success.Should().BeTrue("Factory should set MinimumEssentialsFrameworkVersion");
        var minimumVersion = match.Groups[1].Value;

        // Essentials' gate (Global.IsRunningMinimumVersionOrHigher) parses the minimum with
        // new Version(...) and skips the plugin if that throws, so a prerelease literal such as
        // "3.0.0-rc.1" is never loaded. The running version has its prerelease suffix dropped
        // before comparing, so a bare "3.0.0" passes on every 3.0.0 prerelease.
        Version.TryParse(minimumVersion, out _).Should().BeTrue(
            "Essentials skips a plugin whose minimum version System.Version cannot parse, got \"{0}\"", minimumVersion);
        minimumVersion.Should().Be("3.0.0");
    }

    [Fact]
    public void Factory_Sets_TypeNames()
    {
        var content = FactorySourceContent.Value;

        var pattern = @"TypeNames\s*=\s*new\s+List<string>";
        Regex.IsMatch(content, pattern).Should().BeTrue(
            "Factory should set TypeNames in the constructor");
    }

    [Theory]
    [InlineData("samsungMdcPlugin")]
    public void Factory_Source_Contains_TypeName(string typeName)
    {
        var content = FactorySourceContent.Value;

        content.Should().Contain($"\"{typeName}\"",
            $"Factory should register type name \"{typeName}\"");
    }

    [Fact]
    public void No_Duplicate_TypeNames_In_Factory_Source()
    {
        var content = FactorySourceContent.Value;

        var typeNamesMatch = Regex.Match(content, @"TypeNames\s*=\s*new\s+List<string>\s*\{([^}]+)\}");
        typeNamesMatch.Success.Should().BeTrue("Should find TypeNames assignment");

        var typeNamesBlock = typeNamesMatch.Groups[1].Value;
        var quotedStrings = Regex.Matches(typeNamesBlock, @"""([^""]+)""")
            .Select(m => m.Groups[1].Value)
            .ToList();

        quotedStrings.Should().OnlyHaveUniqueItems("TypeNames should not contain duplicates");
    }
}
