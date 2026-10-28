using FluentAssertions;
using Xunit;

namespace PepperDashPluginSamsungMdcDisplay.Tests;

public class FactoryDiscoveryTests
{
    [Fact]
    public void Assembly_Loads_Successfully()
    {
        var assembly = AssemblyFixture.PluginAssembly;
        assembly.Should().NotBeNull();
    }

    [Fact]
    public void Assembly_Name_Is_EpiDisplaySamsungMdc()
    {
        var assembly = AssemblyFixture.PluginAssembly;
        assembly.GetName().Name.Should().Be("Pepperdash.Essentials.Plugins.Display.Samsung.MDC");
    }

    [Fact]
    public void Factory_Count_Is_Two()
    {
        // SamsungMdcControllerFactory (real display) and MockSamsungMdcFactory (bench mock).
        var factories = AssemblyFixture.FindFactoryTypes();
        factories.Should().HaveCount(2);
    }

    [Fact]
    public void MockSamsungMdcFactory_Exists()
    {
        var factories = AssemblyFixture.FindFactoryTypes();
        factories.Should().ContainSingle(t => t.Name == "MockSamsungMdcFactory");
    }

    [Fact]
    public void Mock_Console_Help_Fits_The_79_Byte_Limit()
    {
        // CrestronConsole.AddNewConsoleCommand throws on longer help, and inside BuildDevice that makes the
        // mock type load as "unknown"
        var source = File.ReadAllText(Path.Combine(AssemblyFixture.SourceDirectory, "Mock", "MockSamsungMdcConsole.cs"));
        var help = System.Text.RegularExpressions.Regex.Match(source, @"const string Help\s*=\s*""([^""]*)""");
        help.Success.Should().BeTrue();
        System.Text.Encoding.ASCII.GetByteCount(help.Groups[1].Value).Should().BeLessThanOrEqualTo(79);
    }

    [Fact]
    public void Mock_Factory_Registers_MockSamsungMdc_Type()
    {
        var source = File.ReadAllText(Path.Combine(AssemblyFixture.SourceDirectory, "Mock", "MockSamsungMdcFactory.cs"));
        source.Should().Contain("\"mockSamsungMdc\"");
    }

    [Fact]
    public void SamsungMdcControllerFactory_Exists()
    {
        var factories = AssemblyFixture.FindFactoryTypes();
        factories.Should().ContainSingle(t => t.Name == "SamsungMdcControllerFactory");
    }

    [Fact]
    public void Factory_Has_Parameterless_Constructor()
    {
        var factories = AssemblyFixture.FindFactoryTypes();
        foreach (var factory in factories)
        {
            var ctor = factory.GetConstructor(Type.EmptyTypes);
            ctor.Should().NotBeNull($"Factory '{factory.Name}' must have a parameterless constructor");
        }
    }
}
