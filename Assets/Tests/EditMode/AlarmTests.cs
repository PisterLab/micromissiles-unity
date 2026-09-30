using NUnit.Framework;

public class AlarmTests {
  [Test]
  public void TryFire_BeforeDeadline_DoesNotFire() {
    var alarm = new Alarm();
    alarm.Set(currentTime: 2f, delaySeconds: 3f);

    Assert.IsTrue(alarm.IsArmed);
    Assert.IsFalse(alarm.TryFire(currentTime: 4.9f));
    Assert.IsTrue(alarm.IsArmed);
  }

  [Test]
  public void TryFire_AtDeadline_FiresOnce() {
    var alarm = new Alarm();
    alarm.Set(currentTime: 2f, delaySeconds: 3f);

    Assert.IsTrue(alarm.TryFire(currentTime: 5f));
    Assert.IsFalse(alarm.IsArmed);
    Assert.IsFalse(alarm.TryFire(currentTime: 6f));
  }

  [Test]
  public void Clear_ArmedAlarm_DoesNotFire() {
    var alarm = new Alarm();
    alarm.Set(currentTime: 2f, delaySeconds: 3f);

    alarm.Clear();

    Assert.IsFalse(alarm.IsArmed);
    Assert.IsFalse(alarm.TryFire(currentTime: 5f));
  }

  [Test]
  public void Set_FiredAlarm_RearmsWithNewDeadline() {
    var alarm = new Alarm();
    alarm.Set(currentTime: 0f, delaySeconds: 1f);
    Assert.IsTrue(alarm.TryFire(currentTime: 1f));

    alarm.Set(currentTime: 3f, delaySeconds: 2f);

    Assert.IsFalse(alarm.TryFire(currentTime: 4f));
    Assert.IsTrue(alarm.TryFire(currentTime: 5f));
  }

  [Test]
  public void Set_NegativeDelay_FiresImmediately() {
    var alarm = new Alarm();
    alarm.Set(currentTime: 2f, delaySeconds: -1f);

    Assert.IsTrue(alarm.TryFire(currentTime: 2f));
  }
}
