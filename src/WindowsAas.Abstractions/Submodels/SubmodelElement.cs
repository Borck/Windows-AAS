namespace WindowsAas.Abstractions.Submodels;

/// <summary>
/// A single element within a <see cref="SubmodelDefinition"/>. Either a typed,
/// readable/writable <see cref="SubmodelElementKind.Property"/> or a nested
/// <see cref="SubmodelElementKind.Collection"/> of further elements.
/// </summary>
public sealed record SubmodelElement
{
  /// <summary>Short id (idShort), unique within its parent.</summary>
  public required string IdShort { get; init; }

  /// <summary>Element kind.</summary>
  public required SubmodelElementKind Kind { get; init; }

  /// <summary>Value type for <see cref="SubmodelElementKind.Property"/> elements.</summary>
  public AasValueType ValueType { get; init; } = AasValueType.String;

  /// <summary>
  /// Whether the host accepts writes (AAS → Windows) for this element. Read-only
  /// elements (e.g. hardware info) reject inbound writes.
  /// </summary>
  public bool Writable { get; init; }

  /// <summary>Initial / current value (string-serialized, AAS convention).</summary>
  public string? Value { get; init; }

  /// <summary>Optional human readable description.</summary>
  public string? Description { get; init; }

  /// <summary>Child elements for a <see cref="SubmodelElementKind.Collection"/>.</summary>
  public IReadOnlyList<SubmodelElement> Children { get; init; } = [];
}
