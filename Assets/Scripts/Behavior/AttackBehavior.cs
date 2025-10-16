using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class AttackBehavior {
  public string name;
  public AttackBehaviorType attackBehaviorType;

  public Vector3 targetPosition;
  public Vector3 targetVelocity;
  public Vector3 targetColliderSize;
  public FlightPlan flightPlan;
  // Returns the next waypoint for the threat to navigate to
  // In addition, return the power setting to use toward the waypoint
  public virtual (Vector3 waypointPosition, PowerSetting power)
      GetNextWaypoint(Vector3 currentPosition, Vector3 targetPosition) {
    return (targetPosition, PowerSetting.IDLE);
  }

  protected static string ResolveBehaviorPath(string json) {
    // Append "Configs/Behaviors/Attack/" to the json path if it's not already there
    if (!json.StartsWith("Configs/Behaviors/Attack/")) {
      json = "Configs/Behaviors/Attack/" + json;
    }
    return json;
  }

  public static AttackBehavior FromJson(string json) {
    string resolvedPath = ResolveBehaviorPath(json);
    string fileContent = ConfigLoader.LoadFromStreamingAssets(resolvedPath);
    return JsonConvert.DeserializeObject<AttackBehavior>(fileContent, new JsonSerializerSettings {
      Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
    });
  }

  [JsonConverter(typeof(StringEnumConverter))]
  public enum AttackBehaviorType { DIRECT_ATTACK, PREPLANNED_ATTACK, SLALOM_ATTACK }
}

[System.Serializable]
public class VectorWaypoint : Waypoint {
  public Vector3 waypointPosition;
  public PowerSetting power;
}

[System.Serializable]
public class Waypoint {}
public class FlightPlan {
  public string type;
}
