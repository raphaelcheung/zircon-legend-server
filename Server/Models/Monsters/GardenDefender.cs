// Decompiled with JetBrains decompiler
// Type: Server.Models.Monsters.GardenDefender
// Assembly: Server, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 63AFABAB-CEF2-4B4F-8F20-3F42999D232C
// Assembly location: F:\Server-2023无限制雪域端\Server.exe

using Library;
using Library.Network;
using Library.Network.ServerPackets;
using Server.Envir;
using System;

namespace Zircon.Server.Models.Monsters
{
  public class GardenDefender : MonsterObject
  {
    protected override void Attack()
    {
      if (this.CurrentHP <= this.Stats[Stat.Health] / 2 && SEnvir.Random.Next(4) > 0)
        this.Defend();
      else
        base.Attack();
    }

    private void Defend()
    {
      this.Direction = Functions.DirectionFromPoint(this.CurrentLocation, this.Target.CurrentLocation);
      this.UpdateAttackTime();
      this.Broadcast((Packet) new ObjectRangeAttack()
      {
        ObjectID = this.ObjectID,
        Direction = this.Direction,
        Location = this.CurrentLocation
      });
      foreach (MapObject allObject in this.GetAllObjects(this.CurrentLocation, 1))
      {
        if (this.CanHelpTarget(allObject))
        {
          Stats stats = new Stats()
          {
            [Stat.MinAC] = allObject.Stats[Stat.MaxAC] / 2,
            [Stat.MaxAC] = allObject.Stats[Stat.MaxAC] / 2,
            [Stat.PhysicalResistance] = 1
          };
          allObject.BuffAdd(BuffType.Resilience, TimeSpan.FromSeconds(6.0), stats, true, false, TimeSpan.Zero);
        }
      }
    }
  }
}
