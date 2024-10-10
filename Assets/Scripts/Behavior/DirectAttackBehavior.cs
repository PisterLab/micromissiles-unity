using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class DirectAttackBehavior : AttackBehavior
{
    public new DTTFlightPlan flightPlan;

    // Returns the next waypoint for the threat to navigate to
    // In addition, return the power setting to use toward the waypoint
    public override (Vector3 waypointPosition, PowerSetting power)
        GetNextWaypoint(Vector3 currentPosition, Vector3 targetPosition)
    {
        if (flightPlan.waypoints == null || flightPlan.waypoints.Count == 0)
        {
            return (targetPosition, PowerSetting.MAX);
        }

        Vector3 directionToTarget = targetPosition - currentPosition;
        float distanceToTarget = directionToTarget.magnitude;
        Vector3 normalizedDirection = directionToTarget.normalized;

        // Sort waypoints in descending order based on distance
        flightPlan.waypoints.Sort((a, b) => b.distance.CompareTo(a.distance));

        DTTWaypoint waypoint = flightPlan.waypoints[flightPlan.waypoints.Count - 1]; // Default to last waypoint

        // Find the appropriate waypoint based on the distance to the target
        foreach (var wp in flightPlan.waypoints)
        {
            if (distanceToTarget <= wp.distance)
            {
                waypoint = wp;
            }
        }

        Vector3 waypointPosition;
        PowerSetting power;

        // If we're very close to the target, use the target position
        if (distanceToTarget <= flightPlan.waypoints[flightPlan.waypoints.Count - 1].distance)
        {
            waypointPosition = targetPosition;
            power = flightPlan.waypoints[flightPlan.waypoints.Count - 1].power;
        }
        else
        {
            // Calculate the waypoint position along the path from the target backward
            waypointPosition = targetPosition - normalizedDirection * waypoint.distance;
            waypointPosition.y = waypoint.altitude;
            power = waypoint.power;
        }

        return (waypointPosition, power);
    }

    public new static DirectAttackBehavior FromJson(string json)
    {
        string resolvedPath = ResolveBehaviorPath(json);
        string fileContent = ConfigLoader.LoadFromStreamingAssets(resolvedPath);
        DirectAttackBehavior behavior = JsonConvert.DeserializeObject<DirectAttackBehavior>(
            fileContent, new JsonSerializerSettings { Converters = { new StringEnumConverter() } });

        // Sort waypoints in descending order based on distance
        if (behavior.flightPlan != null && behavior.flightPlan.waypoints != null)
        {
            behavior.flightPlan.waypoints.Sort((a, b) => b.distance.CompareTo(a.distance));
        }

        return behavior;
    }
}

[System.Serializable]
public class DTTWaypoint : Waypoint
{
    public float distance;
    public float altitude;
    public PowerSetting power;
}

public class DTTFlightPlan : FlightPlan
{
    public List<DTTWaypoint> waypoints;
}
