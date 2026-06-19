using Shouldly;
using WindowsAas.Mqtt;
using Xunit;

namespace WindowsAas.Tests;

public class MqttTopicsTests
{
  private readonly MqttTopics _topics = new("windows-aas");

  [Fact]
  public void Set_topic_round_trips_through_TryParseSet()
  {
    const string submodelId = "urn:windows-aas:av:monitor:1";
    const string path = "Resolution";

    var topic = _topics.Set(submodelId, path);
    var parsed = _topics.TryParseSet(topic, out var gotId, out var gotPath);

    parsed.ShouldBeTrue();
    gotId.ShouldBe(submodelId);
    gotPath.ShouldBe(path);
  }

  [Fact]
  public void TryParseSet_rejects_value_topics()
  {
    var topic = _topics.Value("urn:windows-aas:av:monitor:1", "Resolution");
    _topics.TryParseSet(topic, out _, out _).ShouldBeFalse();
  }

  [Fact]
  public void Set_topic_preserves_nested_idShort_paths()
  {
    var topic = _topics.Set("urn:sm", "Group.Child");
    _topics.TryParseSet(topic, out var id, out var path).ShouldBeTrue();
    id.ShouldBe("urn:sm");
    path.ShouldBe("Group.Child");
  }
}
