namespace WindowsAas.Abstractions.Submodels;

/// <summary>
/// The subset of AAS submodel element kinds the host maps Windows properties to.
/// </summary>
public enum SubmodelElementKind
{
  /// <summary>A single typed value (maps to an AAS <c>Property</c>).</summary>
  Property,

  /// <summary>A nested collection of elements (maps to a <c>SubmodelElementCollection</c>).</summary>
  Collection,
}
