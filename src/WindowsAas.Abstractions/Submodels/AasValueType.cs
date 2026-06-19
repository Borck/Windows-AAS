using System.Diagnostics.CodeAnalysis;

namespace WindowsAas.Abstractions.Submodels;

/// <summary>
/// AAS-compatible XSD value types used for submodel <c>Property</c> elements.
/// Mirrors the relevant subset of the AAS v3 <c>DataTypeDefXsd</c> enumeration;
/// the member names intentionally match the XSD type names.
/// </summary>
[SuppressMessage("Naming", "CA1720:Identifier contains type name",
  Justification = "Member names intentionally mirror the AAS DataTypeDefXsd / XSD type names.")]
public enum AasValueType
{
  String,
  Boolean,
  Integer,
  Double,
}
