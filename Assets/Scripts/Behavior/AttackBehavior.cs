using System.Collections.Generic;
using UnityEngine;

public abstract class AttackBehavior
{
    

    // Returns the next waypoint for the threat to navigate to
    // In addition, return the power setting to use toward the waypoint
    public abstract (Vector3 waypointPosition, StaticAgentConfig.PowerSetting power) GetNextWaypoint(Vector3 currentPosition, Vector3 targetPosition);

    [System.Serializable]
    public class DTTWaypoint
    {
        public float distance;
        public float targetAltitude;
        public StaticAgentConfig.PowerSetting power;
    }

    public class VectorWaypoint
    {
        public Vector3 waypointPosition;
        public StaticAgentConfig.PowerSetting power;
    }
}