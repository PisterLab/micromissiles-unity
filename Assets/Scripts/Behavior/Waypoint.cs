public class Waypoint {
  public float Distance { get; }
  public float Altitude { get; }
  public Configs.Power Power { get; }

  public Waypoint(in Configs.AttackBehaviorConfig.Types.FlightPlan.Types.Waypoint waypoint) {
    Distance = waypoint.Distance;
    Altitude = waypoint.Altitude;
    Power = waypoint.Power;
  }
}
