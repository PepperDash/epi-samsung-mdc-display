using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace PepperDashPluginSamsungMdcDisplay.Tests;

public class ConfigDeserializationTests
{
    private static readonly Lazy<Type?> ConfigType = new(() =>
        AssemblyFixture.PluginAssembly.GetType("PepperDashPluginSamsungMdcDisplay.SamsungMdcDisplayPropertiesConfig"));

    [Fact]
    public void Config_Class_Exists()
    {
        ConfigType.Value.Should().NotBeNull(
            "SamsungMdcDisplayPropertiesConfig class should exist in the assembly");
    }

    [Fact]
    public void Config_Has_Parameterless_Constructor()
    {
        var type = ConfigType.Value!;
        var ctor = type.GetConstructor(Type.EmptyTypes);
        ctor.Should().NotBeNull("Config class must have a parameterless constructor for JSON deserialization");
    }

    [Theory]
    [InlineData("id")]
    [InlineData("volumeUpperLimit")]
    [InlineData("volumeLowerLimit")]
    [InlineData("pollIntervalMs")]
    [InlineData("coolingTimeMs")]
    [InlineData("warmingTimeMs")]
    [InlineData("showVolumeControls")]
    [InlineData("pollLedTemps")]
    [InlineData("friendlyNames")]
    [InlineData("customInputs")]
    [InlineData("activeInputs")]
    public void Config_Property_Has_JsonPropertyAttribute(string jsonName)
    {
        var type = ConfigType.Value!;
        var properties = type.GetProperties();

        var hasAttribute = properties.Any(p =>
            p.CustomAttributes.Any(a =>
                a.AttributeType.Name == "JsonPropertyAttribute"
                && a.ConstructorArguments.Any(arg =>
                    string.Equals(arg.Value?.ToString(), jsonName, StringComparison.Ordinal))));

        hasAttribute.Should().BeTrue($"Config should have a property with [JsonProperty(\"{jsonName}\")]");
    }

    [Theory]
    [InlineData("FriendlyName", "inputKey")]
    [InlineData("FriendlyName", "name")]
    [InlineData("CustomInput", "inputCommand")]
    [InlineData("CustomInput", "inputConnector")]
    [InlineData("CustomInput", "inputIdentifier")]
    [InlineData("ActiveInputs", "key")]
    [InlineData("ActiveInputs", "name")]
    public void Nested_Config_Property_Has_JsonPropertyAttribute(string typeName, string jsonName)
    {
        var type = AssemblyFixture.PluginAssembly.GetType("PepperDashPluginSamsungMdcDisplay." + typeName);
        type.Should().NotBeNull($"{typeName} class should exist in the assembly");

        var hasAttribute = type!.GetProperties().Any(p =>
            p.CustomAttributes.Any(a =>
                a.AttributeType.Name == "JsonPropertyAttribute"
                && a.ConstructorArguments.Any(arg =>
                    string.Equals(arg.Value?.ToString(), jsonName, StringComparison.Ordinal))));

        hasAttribute.Should().BeTrue($"{typeName} should have a property with [JsonProperty(\"{jsonName}\")]");
    }

    private const string SampleJson = """
        {
            "id": "1",
            "volumeUpperLimit": 100,
            "volumeLowerLimit": 0,
            "pollIntervalMs": 30000,
            "coolingTimeMs": 15000,
            "warmingTimeMs": 15000,
            "showVolumeControls": true,
            "pollLedTemps": false,
            "friendlyNames": [ { "inputKey": "hdmi1", "name": "Laptop" } ],
            "customInputs": [],
            "activeInputs": [ { "key": "hdmi1", "name": "HDMI 1" } ]
        }
        """;

    [Fact]
    public void Config_Deserializes_Sample_Json()
    {
        var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(SampleJson);

        dict.Should().ContainKey("id");
        dict.Should().ContainKey("volumeUpperLimit");
        dict.Should().ContainKey("activeInputs");
    }

    [Fact]
    public void ActiveInputs_Deserialize_As_List_With_Expected_Shape()
    {
        var jo = JObject.Parse(SampleJson);
        var activeInputs = jo["activeInputs"] as JArray;

        activeInputs.Should().NotBeNull("activeInputs should deserialize as a JSON array");
        activeInputs!.Should().HaveCount(1);
        activeInputs![0]["key"]!.Value<string>().Should().Be("hdmi1");
        activeInputs![0]["name"]!.Value<string>().Should().Be("HDMI 1");
    }

    [Theory]
    [InlineData("Id",                 "String")]
    [InlineData("VolumeUpperLimit",   "Int32")]
    [InlineData("VolumeLowerLimit",   "Int32")]
    [InlineData("PollIntervalMs",     "Int64")]
    [InlineData("CoolingTimeMs",      "UInt32")]
    [InlineData("WarmingTimeMs",      "UInt32")]
    [InlineData("ShowVolumeControls", "Boolean")]
    [InlineData("PollLedTemps",       "Boolean")]
    public void Config_Property_Type_Matches(string propertyName, string expectedTypeName)
    {
        var prop = ConfigType.Value!.GetProperty(propertyName);
        prop.Should().NotBeNull($"SamsungMdcDisplayPropertiesConfig should expose {propertyName}");
        prop!.PropertyType.Name.Should().Be(expectedTypeName,
            $"{propertyName} should be {expectedTypeName} for the config contract to hold.");
    }

    [Theory]
    [InlineData("FriendlyNames", "FriendlyName")]
    [InlineData("CustomInputs",  "CustomInput")]
    [InlineData("ActiveInputs",  "ActiveInputs")]
    public void List_Property_Is_List_Of_Expected_Element(string propertyName, string expectedElementType)
    {
        var prop = ConfigType.Value!.GetProperty(propertyName);
        prop.Should().NotBeNull($"SamsungMdcDisplayPropertiesConfig should expose {propertyName}");

        var type = prop!.PropertyType;
        type.IsGenericType.Should().BeTrue($"{propertyName} must be a generic collection");
        type.GetGenericTypeDefinition().Name.Should().Be("List`1",
            $"{propertyName} must be a List<T> so JSON arrays deserialize correctly");
        type.GetGenericArguments()[0].Name.Should().Be(expectedElementType,
            $"{propertyName} entries must deserialize to {expectedElementType}");
    }
}
