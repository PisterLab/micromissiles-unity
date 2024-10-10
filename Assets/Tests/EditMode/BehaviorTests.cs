using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class BehaviorTests : TestBase
{
    [Test]
    public void TestDirectAttackBehaviorWaypoints()
    {
        // Create a sample DirectAttackBehavior
        DirectAttackBehavior behavior = new DirectAttackBehavior();
        behavior.flightPlan = new DTTFlightPlan
        {
            waypoints = new List<DTTWaypoint>
            {
                new DTTWaypoint { distance = 1000, altitude = 100, power = PowerSetting.CRUISE },
                new DTTWaypoint { distance = 500, altitude = 50, power = PowerSetting.MIL },
                new DTTWaypoint { distance = 100, altitude = 25, power = PowerSetting.MAX }
            }
        };

        // Waypoints are already sorted in descending order in the production code

        Vector3 targetPosition = new Vector3(1000, 0, 0);
        const float epsilon = 0.001f;

        // Test waypoint selection based on distance
        Vector3 currentPosition = new Vector3(0, 0, 0);
        var result = behavior.GetNextWaypoint(currentPosition, targetPosition);
        Assert.AreEqual(0, result.waypointPosition.x, epsilon);
        Assert.AreEqual(100, result.waypointPosition.y, epsilon);
        Assert.AreEqual(0, result.waypointPosition.z, epsilon);
        Assert.AreEqual(PowerSetting.CRUISE, result.power);

        currentPosition = new Vector3(600, 0, 0);
        result = behavior.GetNextWaypoint(currentPosition, targetPosition);
        Assert.AreEqual(500, result.waypointPosition.x, epsilon);
        Assert.AreEqual(50, result.waypointPosition.y, epsilon);
        Assert.AreEqual(0, result.waypointPosition.z, epsilon);
        Assert.AreEqual(PowerSetting.MIL, result.power);

        currentPosition = new Vector3(920, 0, 0);
        result = behavior.GetNextWaypoint(currentPosition, targetPosition);
        Assert.AreEqual(900, result.waypointPosition.x, epsilon);
        Assert.AreEqual(25, result.waypointPosition.y, epsilon);
        Assert.AreEqual(0, result.waypointPosition.z, epsilon);
        Assert.AreEqual(PowerSetting.MAX, result.power);

        // Test behavior within final distance
        currentPosition = new Vector3(950, 0, 0);
        result = behavior.GetNextWaypoint(currentPosition, targetPosition);
        Assert.AreEqual(targetPosition.x, result.waypointPosition.x, epsilon);
        Assert.AreEqual(targetPosition.y, result.waypointPosition.y, epsilon);
        Assert.AreEqual(targetPosition.z, result.waypointPosition.z, epsilon);
        Assert.AreEqual(PowerSetting.MAX, result.power); // Should use the power of the closest waypoint

        // Test with non-zero Z coordinate
        targetPosition = new Vector3(800, 0, 600);
        currentPosition = new Vector3(0, 0, 0);
        result = behavior.GetNextWaypoint(currentPosition, targetPosition);
        Assert.AreEqual(0, result.waypointPosition.x, epsilon);
        Assert.AreEqual(100, result.waypointPosition.y, epsilon);
        Assert.AreEqual(0, result.waypointPosition.z, epsilon);
        Assert.AreEqual(PowerSetting.CRUISE, result.power);

        currentPosition = new Vector3(400, 0, 300);
        result = behavior.GetNextWaypoint(currentPosition, targetPosition);
        Assert.AreEqual(400, result.waypointPosition.x, epsilon);
        Assert.AreEqual(50, result.waypointPosition.y, epsilon);
        Assert.AreEqual(300, result.waypointPosition.z, epsilon);
        Assert.AreEqual(PowerSetting.MIL, result.power);
    }
}
