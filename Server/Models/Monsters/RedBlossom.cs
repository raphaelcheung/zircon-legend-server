// Decompiled with JetBrains decompiler
// Type: Server.Models.Monsters.RedBlossom
// Assembly: Server, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 63AFABAB-CEF2-4B4F-8F20-3F42999D232C
// Assembly location: F:\Server-2023无限制雪域端\Server.exe

using Library;
using Library.Network;
using Library.Network.ServerPackets;
using Server.Envir;
using System;
using System.Drawing;

namespace Zircon.Server.Models.Monsters
{
  public class RedBlossom : SpittingSpider
  {
    protected override bool InAttackRange()
    {
      return this.Target.CurrentMap == this.CurrentMap && !(this.Target.CurrentLocation == this.CurrentLocation) && Functions.InRange(this.CurrentLocation, this.Target.CurrentLocation, 5);
    }

    public override void ProcessTarget()
    {
      if (this.Target == null)
        return;
      if (!this.InAttackRange())
      {
        if (this.CurrentLocation == this.Target.CurrentLocation)
        {
          MirDirection mirDirection = (MirDirection) SEnvir.Random.Next(8);
          int i = SEnvir.Random.Next(2) == 0 ? 1 : -1;
          for (int index = 0; index < 8 && !this.Walk(mirDirection); ++index)
            mirDirection = Functions.ShiftDirection(mirDirection, i);
        }
        else
          this.MoveTo(this.Target.CurrentLocation);
      }
      else
      {
        if (!this.CanAttack)
          return;
        int num1 = Math.Abs(this.Target.CurrentLocation.X - this.CurrentLocation.X);
        Point currentLocation = this.Target.CurrentLocation;
        int y1 = currentLocation.Y;
        currentLocation = this.CurrentLocation;
        int y2 = currentLocation.Y;
        int num2 = Math.Abs(y1 - y2);
        if (num1 != 0 && num1 != num2 && num2 != 0 && SEnvir.Random.Next(8) > 0)
          this.MoveTo(this.Target.CurrentLocation);
        else
          this.Attack();
      }
    }

    protected override void Attack()
    {
      this.Direction = Functions.DirectionFromPoint(this.CurrentLocation, this.Target.CurrentLocation);
      this.UpdateAttackTime();
      Point currentLocation1 = this.Target.CurrentLocation;
      int x1 = currentLocation1.X;
      currentLocation1 = this.CurrentLocation;
      int x2 = currentLocation1.X;
      int num1 = Math.Abs(x1 - x2);
      Point currentLocation2 = this.Target.CurrentLocation;
      int y1 = currentLocation2.Y;
      currentLocation2 = this.CurrentLocation;
      int y2 = currentLocation2.Y;
      int num2 = Math.Abs(y1 - y2);
      if (num1 == 0 || num1 == num2 || num2 == 0)
      {
        this.Broadcast((Packet) new ObjectAttack()
        {
          ObjectID = this.ObjectID,
          Direction = this.Direction,
          Location = this.CurrentLocation
        });
        this.LineAttack(5);
      }
      else
      {
        this.Broadcast((Packet) new ObjectRangeAttack()
        {
          ObjectID = this.ObjectID,
          Direction = this.Direction,
          Location = this.CurrentLocation
        });
        foreach (MirDirection direction in Enum.GetValues(typeof (MirDirection)))
          this.LineAttack(5, direction);
      }
    }
  }
}
