using System.Text.Json.Nodes;
using WindowsAas.Abstractions.Submodels;

namespace WindowsAas.Aas;

/// <summary>
/// Maps the host-neutral <see cref="SubmodelDefinition"/> onto the AAS v3 JSON
/// serialization understood by the BaSyx <c>aas-environment</c>.
/// </summary>
public static class SubmodelJsonMapper
{
  /// <summary>Builds the AAS v3 JSON object for a whole submodel.</summary>
  public static JsonObject ToJson(SubmodelDefinition submodel)
  {
    ArgumentNullException.ThrowIfNull(submodel);

    var json = new JsonObject
    {
      ["modelType"] = "Submodel",
      ["id"] = submodel.Id,
      ["idShort"] = submodel.IdShort,
      ["kind"] = "Instance",
    };

    if (submodel.SemanticId is { } semanticId)
    {
      json["semanticId"] = SemanticIdReference(semanticId);
    }

    var elements = new JsonArray();
    foreach (var element in submodel.Elements)
    {
      elements.Add(ToJson(element));
    }

    json["submodelElements"] = elements;
    return json;
  }

  private static JsonObject ToJson(SubmodelElement element)
  {
    if (element.Kind == SubmodelElementKind.Collection)
    {
      var children = new JsonArray();
      foreach (var child in element.Children)
      {
        children.Add(ToJson(child));
      }

      return new JsonObject
      {
        ["modelType"] = "SubmodelElementCollection",
        ["idShort"] = element.IdShort,
        ["value"] = children,
      };
    }

    return new JsonObject
    {
      ["modelType"] = "Property",
      ["idShort"] = element.IdShort,
      ["valueType"] = ToXsd(element.ValueType),
      ["value"] = element.Value,
    };
  }

  /// <summary>Maps our value type enum to the AAS XSD type identifier.</summary>
  public static string ToXsd(AasValueType valueType) => valueType switch
  {
    AasValueType.Boolean => "xs:boolean",
    AasValueType.Integer => "xs:integer",
    AasValueType.Double => "xs:double",
    _ => "xs:string",
  };

  private static JsonObject SemanticIdReference(string semanticId) => new()
  {
    ["type"] = "ExternalReference",
    ["keys"] = new JsonArray
    {
      new JsonObject
      {
        ["type"] = "GlobalReference",
        ["value"] = semanticId,
      },
    },
  };
}
