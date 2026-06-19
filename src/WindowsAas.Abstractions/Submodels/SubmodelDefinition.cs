namespace WindowsAas.Abstractions.Submodels;

/// <summary>
/// A plugin-contributed submodel: the unit the host registers in the BaSyx
/// AAS environment / submodel registry. Each plugin contributes one info/overview
/// submodel plus zero or more capability submodels (the AV plugin contributes one
/// submodel per audio/video device).
/// </summary>
public sealed record SubmodelDefinition
{
  /// <summary>
  /// Globally unique submodel identifier (an IRI), e.g.
  /// <c>urn:windows-aas:av:monitor:1</c>.
  /// </summary>
  public required string Id { get; init; }

  /// <summary>Short id (idShort) shown in tooling.</summary>
  public required string IdShort { get; init; }

  /// <summary>
  /// Semantic id describing the submodel's meaning (e.g. a ConceptDescription IRI).
  /// </summary>
  public string? SemanticId { get; init; }

  /// <summary>Top-level elements of the submodel.</summary>
  public required IReadOnlyList<SubmodelElement> Elements { get; init; }
}
