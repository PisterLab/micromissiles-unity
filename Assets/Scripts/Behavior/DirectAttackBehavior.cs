using System.Collections.Generic;
using UnityEngine;

public class DirectAttackBehavior : AttackBehavior
{
    public List<DTTWaypoint> waypoints;

    // Returns the next waypoint for the threat to navigate to
    // In addition, return the power setting to use toward the waypoint
    public override (Vector3 waypointPosition, StaticAgentConfig.PowerSetting power) GetNextWaypoint(Vector3 currentPosition, Vector3 targetPosition)
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            // If no waypoints are defined, directly target the target position
            return (targetPosition, StaticAgentConfig.PowerSetting.MAX);
        }

        Vector3 directionToTarget = targetPosition - currentPosition;
        float distanceToTarget = directionToTarget.magnitude;

        // Find the current waypoint based on the distance to target
        int currentWaypointIndex = 0;
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (distanceToTarget <= waypoints[i].distance)
            {
                currentWaypointIndex = i;
                break;
            }
        }

        Vector3 waypointPosition;
        StaticAgentConfig.PowerSetting power;

        if (currentWaypointIndex < waypoints.Count - 1)
        {
            // There is a next waypoint
            DTTWaypoint nextWaypoint = waypoints[currentWaypointIndex + 1];
            waypointPosition = targetPosition + directionToTarget.normalized * nextWaypoint.distance;
            waypointPosition.y = nextWaypoint.targetAltitude;
            power = nextWaypoint.power;
        }
        else
        {
            // This is the last waypoint, so target the final position
            waypointPosition = targetPosition;
            power = waypoints[currentWaypointIndex].power;
        }

        return (waypointPosition, power);
    }

}