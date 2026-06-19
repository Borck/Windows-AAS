using Shouldly;
using WindowsAas.Aas;
using WindowsAas.Abstractions.Submodels;
using Xunit;

namespace WindowsAas.Tests;

public class AasMappingTests
{
  [Fact]
  public void AasIdentifier_encode_decode_round_trips()
  {
    const string id = "urn:windows-aas:av:monitor:1";
    AasIdentifier.Decode(AasIdentifier.Encode(id)).ShouldBe(id);
  }

  [Fact]
  public void SubmodelJsonMapper_emits_aas_v3_shape()
  {
    var submodel = new SubmodelDefinition
    {
      Id = "urn:sm",
      IdShort = "Demo",
      Elements =
      [
        new SubmodelElement
        {
          IdShort = "Width",
          Kind = SubmodelElementKind.Property,
          ValueType = AasValueType.Integer,
          Value = "1920",
        },
      ],
    };

    var json = SubmodelJsonMapper.ToJson(submodel);

    json["modelType"]!.GetValue<string>().ShouldBe("Submodel");
    json["id"]!.GetValue<string>().ShouldBe("urn:sm");
    var element = json["submodelElements"]!.AsArray()[0]!;
    element["modelType"]!.GetValue<string>().ShouldBe("Property");
    element["valueType"]!.GetValue<string>().ShouldBe("xs:integer");
    element["value"]!.GetValue<string>().ShouldBe("1920");
  }
}
