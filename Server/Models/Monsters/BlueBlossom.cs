// Decompiled with JetBrains decompiler
// Type: Server.Models.Monsters.BlueBlossom
// Assembly: Server, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 63AFABAB-CEF2-4B4F-8F20-3F42999D232C
// Assembly location: F:\Server-2023无限制雪域端\Server.exe

using Library;
using Library.Network;
using Library.Network.ServerPackets;
using Server.DBModels;
using Server.Envir;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Zircon.Server.Models.Monsters
{
  public class BlueBlossom : MonsterObject
  {
    protected override bool InAttackRange()
    {
      return this.Target.CurrentMap == this.CurrentMap && !(this.Target.CurrentLocation == this.CurrentLocation) && Functions.InRange(this.CurrentLocation, this.Target.CurrentLocation, 8);
    }

    public override bool ShouldAttackTarget(MapObject ob) => this.CanAttackTarget(ob);

    public override bool CanAttackTarget(MapObject ob) => this.CanHelpTarget(ob);

    public override bool CanHelpTarget(MapObject ob)
    {
      return base.CanHelpTarget(ob) && ob.CurrentHP < ob.Stats[Stat.Health] && ob.Buffs.All<BuffInfo>((Func<BuffInfo, bool>) (x => x.Type != BuffType.Heal));
    }

    public override void ProcessAction(DelayedAction action)
    {
      if (action.Type == ActionType.DelayAttack)
        this.Heal((MapObject) action.Data[0]);
      else
        base.ProcessAction(action);
    }

    public override void ProcessSearch() => this.ProperSearch();

    public void Heal(MapObject ob)
    {
      if (ob?.Node == null || ob.Dead)
        return;
      MapObject mapObject = ob;
      TimeSpan maxValue = TimeSpan.MaxValue;
      Stats stats = new Stats();
      stats[Stat.Healing] = this.Stats[Stat.Healing];
      stats[Stat.HealingCap] = this.Stats[Stat.HealingCap];
      TimeSpan tickRate = TimeSpan.FromSeconds(1.0);
      mapObject.BuffAdd(BuffType.Heal, maxValue, stats, false, false, tickRate);
    }

    protected override void Attack()
    {
      this.Direction = Functions.DirectionFromPoint(this.CurrentLocation, this.Target.CurrentLocation);
      this.Broadcast((Packet) new ObjectRangeAttack()
      {
        ObjectID = this.ObjectID,
        Direction = this.Direction,
        Location = this.CurrentLocation,
        Targets = new List<uint>() { this.Target.ObjectID }
      });
      this.UpdateAttackTime();
      this.ActionList.Add(new DelayedAction(SEnvir.Now.AddMilliseconds((double) (400 + Functions.Distance(this.CurrentLocation, this.Target.CurrentLocation) * 48)), ActionType.DelayAttack, new object[1]
      {
        (object) this.Target
      }));
    }
  }
}
