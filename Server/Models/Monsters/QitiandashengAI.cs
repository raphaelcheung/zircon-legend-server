// Decompiled with JetBrains decompiler
// Type: Server.Models.Monsters.QitiandashengAI
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
  public class QitiandashengAI : SpittingSpider
  {
    public DateTime CastTime;
    public TimeSpan CastDelay = TimeSpan.FromSeconds(15.0);

    protected override bool InAttackRange()
    {
      if (Target.CurrentMap != CurrentMap || Target.CurrentLocation == CurrentLocation)
        return false;
      int x1 = Target.CurrentLocation.X;
      Point currentLocation = CurrentLocation;
      int x2 = currentLocation.X;
      int num1 = Math.Abs(x1 - x2);
      currentLocation = Target.CurrentLocation;
      int y1 = currentLocation.Y;
      currentLocation = CurrentLocation;
      int y2 = currentLocation.Y;
      int num2 = Math.Abs(y1 - y2);
      return num1 <= 3 && num2 <= 3 && (num1 == 0 || num1 == num2 || num2 == 0);
    }

    public override void ProcessTarget()
    {
      if (Target == null)
        return;
      if (CanAttack && SEnvir.Now > CastTime)
      {
        List<MapObject> targets = GetTargets(CurrentMap, CurrentLocation, ViewRange);
        if (targets.Count > 0)
        {
          foreach (MapObject mapObject in targets)
          {
            if (CurrentHP <= Stats[Stat.Health] / 2 || SEnvir.Random.Next(2) <= 0)
              QierBian(mapObject.CurrentLocation);
          }
          UpdateAttackTime();
          Broadcast((Packet) new ObjectMagic()
          {
            ObjectID = ObjectID,
            Direction = Direction,
            CurrentLocation = CurrentLocation,
            Cast = true,
            Type = MagicType.None,
            Targets = new List<uint>()
            {
              Target.ObjectID
            }
          });
          CastTime = SEnvir.Now + CastDelay;
        }
      }
      if (!InAttackRange())
      {
        if (CurrentLocation == Target.CurrentLocation)
        {
          MirDirection mirDirection = (MirDirection) SEnvir.Random.Next(8);
          int i = SEnvir.Random.Next(2) == 0 ? 1 : -1;
          for (int index = 0; index < 8 && !Walk(mirDirection); ++index)
            mirDirection = Functions.ShiftDirection(mirDirection, i);
        }
        else
          MoveTo(Target.CurrentLocation);
      }
      else
      {
        if (!CanAttack)
          return;
        Attack();
      }
    }

    protected override void Attack()
    {
      Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
      if (SEnvir.Random.Next(3) == 0 || !Functions.InRange(Target.CurrentLocation, CurrentLocation, 2))
      {
        Broadcast((Packet) new ObjectRangeAttack()
        {
          ObjectID = ObjectID,
          Direction = Direction,
          Location = CurrentLocation,
          Targets = new List<uint>()
        });
        LineAttack(3);
      }
      else
      {
        Broadcast((Packet) new ObjectAttack()
        {
          ObjectID = ObjectID,
          Direction = Direction,
          Location = CurrentLocation
        });
        using (List<MapObject>.Enumerator enumerator = GetTargets(CurrentMap, Functions.Move(CurrentLocation, Direction), 1).GetEnumerator())
        {
          if (enumerator.MoveNext())
          {
            MapObject current = enumerator.Current;
            ActionList.Add(new DelayedAction(SEnvir.Now.AddMilliseconds(400.0), ActionType.DelayAttack, new object[3]
            {
              (object) current,
              (object) GetDC(),
              (object) AttackElement
            }));
          }
        }
      }
      UpdateAttackTime();
    }
  }
}
