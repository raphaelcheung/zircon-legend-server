// Decompiled with JetBrains decompiler
// Type: Server.Models.Monsters.GardenSoldier
// Assembly: Server, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 63AFABAB-CEF2-4B4F-8F20-3F42999D232C
// Assembly location: F:\Server-2023无限制雪域端\Server.exe

using Library;
using Library.Network;
using Library.Network.ServerPackets;
using Server.Envir;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Zircon.Server.Models.Monsters
{
  public class GardenSoldier : MonsterObject
  {
    public int AttackRange = 7;

    protected override bool InAttackRange()
    {
      return this.Target.CurrentMap == this.CurrentMap && !(this.Target.CurrentLocation == this.CurrentLocation) && Functions.InRange(this.CurrentLocation, this.Target.CurrentLocation, this.AttackRange);
    }

    protected bool InMeleeAttackRange()
    {
      if (!this.InAttackRange())
        return false;
      int x1 = this.Target.CurrentLocation.X;
      Point currentLocation = this.CurrentLocation;
      int x2 = currentLocation.X;
      int num1 = Math.Abs(x1 - x2);
      currentLocation = this.Target.CurrentLocation;
      int y1 = currentLocation.Y;
      currentLocation = this.CurrentLocation;
      int y2 = currentLocation.Y;
      int num2 = Math.Abs(y1 - y2);
      return num1 <= 2 && num2 <= 2 && (num1 == 0 || num1 == num2 || num2 == 0);
    }

    public override void ProcessTarget()
    {
      if (this.Target == null)
        return;
      if (this.CurrentLocation == this.Target.CurrentLocation)
      {
        MirDirection mirDirection = (MirDirection) SEnvir.Random.Next(8);
        int i = SEnvir.Random.Next(2) == 0 ? 1 : -1;
        for (int index = 0; index < 8 && !this.Walk(mirDirection); ++index)
          mirDirection = Functions.ShiftDirection(mirDirection, i);
      }
      else if (!this.InMeleeAttackRange())
        this.MoveTo(this.Target.CurrentLocation);
      if (!this.InAttackRange() || !this.CanAttack)
        return;
      this.Attack();
    }

    protected override void Attack()
    {
      this.Direction = Functions.DirectionFromPoint(this.CurrentLocation, this.Target.CurrentLocation);
      this.UpdateAttackTime();
      if (!this.InMeleeAttackRange())
      {
        this.RangeAttack();
      }
      else
      {
        this.Broadcast((Packet) new ObjectAttack()
        {
          ObjectID = this.ObjectID,
          Direction = this.Direction,
          Location = this.CurrentLocation
        });
        for (int distance = 1; distance <= 2; ++distance)
        {
          for (int i = -1; i <= 1; ++i)
          {
            foreach (MapObject target in this.GetTargets(this.CurrentMap, Functions.Move(this.CurrentLocation, Functions.ShiftDirection(this.Direction, i), distance), 0))
              this.ActionList.Add(new DelayedAction(SEnvir.Now.AddMilliseconds(400.0), ActionType.DelayAttack, new object[3]
              {
                (object) target,
                (object) this.GetDC(),
                (object) this.AttackElement
              }));
          }
        }
      }
    }

    private void RangeAttack()
    {
      this.Direction = Functions.DirectionFromPoint(this.CurrentLocation, this.Target.CurrentLocation);
      this.Broadcast((Packet) new ObjectRangeAttack()
      {
        ObjectID = this.ObjectID,
        Direction = this.Direction,
        Location = this.CurrentLocation,
        Targets = new List<uint>() { this.Target.ObjectID }
      });
      this.ActionList.Add(new DelayedAction(SEnvir.Now.AddMilliseconds(400.0), ActionType.DelayAttack, new object[3]
      {
        (object) this.Target,
        (object) this.GetMC(),
        (object) this.AttackElement
      }));
    }
  }
}
