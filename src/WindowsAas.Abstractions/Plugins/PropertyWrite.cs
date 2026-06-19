namespace WindowsAas.Abstractions.Plugins;

/// <summary>
/// A request to change a single writable property, originating from an AAS client
/// and delivered to the host over MQTT.
/// </summary>
/// <param name="SubmodelId">Target submodel id.</param>
/// <param name="IdShortPath">Dot-separated idShort path to the element within the submodel.</param>
/// <param name="Value">Desired value, string-serialized per AAS convention.</param>
public readonly record struct PropertyWriteRequest(string SubmodelId, string IdShortPath, string Value);

/// <summary>Outcome of applying a <see cref="PropertyWriteRequest"/>.</summary>
/// <param name="Success">Whether the write was applied to the Windows host.</param>
/// <param name="ActualValue">
/// The value actually in effect after the write (may differ from the requested value,
/// e.g. a monitor snapping to the nearest supported resolution). Reported back to the AAS.
/// </param>
/// <param name="Error">Error detail when <paramref name="Success"/> is <c>false</c>.</param>
public readonly record struct PropertyWriteResult(bool Success, string? ActualValue, string? Error)
{
  public static PropertyWriteResult Ok(string actualValue) => new(true, actualValue, null);

  public static PropertyWriteResult Fail(string error) => new(false, null, error);
}
