// Decompiled with JetBrains decompiler
// Type: Server.Models.Monsters.FireBird
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
using System.Drawing;
using System.Linq;

namespace Zircon.Server.Models.Monsters
{
    public class FireBird : MonsterObject
    {
        public int AttackRange = 10;

        protected override bool InAttackRange()
        {
            return Target.CurrentMap == CurrentMap && !(Target.CurrentLocation == CurrentLocation) && Functions.InRange(CurrentLocation, Target.CurrentLocation, AttackRange);
        }

        public override void ProcessTarget()
        {
            if (Target == null)
                return;
            if (InAttackRange() && CanAttack)
                Attack();
            else if (CurrentLocation == Target.CurrentLocation)
            {
                MirDirection mirDirection = (MirDirection)SEnvir.Random.Next(8);
                int i = SEnvir.Random.Next(2) == 0 ? 1 : -1;
                for (int index = 0; index < 8 && !Walk(mirDirection); ++index)
                    mirDirection = Functions.ShiftDirection(mirDirection, i);
            }
            else
            {
                if (Functions.InRange(CurrentLocation, Target.CurrentLocation, 2))
                    return;
                MoveTo(Target.CurrentLocation);
            }
        }

        public override void ProcessAction(DelayedAction action)
        {
            if (action.Type == ActionType.RangeAttack)
                ScorchedEarth((MirDirection)action.Data[0]);
            else
                base.ProcessAction(action);
        }

        protected override void Attack()
        {
            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            UpdateAttackTime();
            if (SEnvir.Random.Next(6) == 0 || !Functions.InRange(Target.CurrentLocation, CurrentLocation, 2))
            {
                RangeAttack();
            }
            else
            {
                Broadcast((Packet)new ObjectAttack()
                {
                    ObjectID = ObjectID,
                    Direction = Direction,
                    Location = CurrentLocation
                });
                foreach (MapObject target in GetTargets(CurrentMap, Functions.Move(CurrentLocation, Direction, 2), 1))
                    ActionList.Add(new DelayedAction(SEnvir.Now.AddMilliseconds(400.0), ActionType.DelayAttack, new object[3]
                    {
                        target,
                        GetDC(),
                        AttackElement
                    }));
            }
        }

        private void RangeAttack()
        {
            if (SEnvir.Random.Next(2) == 0)
            {
                MassCyclone(MagicType.IgyuCyclone, 45);
            }
            else
            {
                foreach (MirDirection mirDirection in Enum.GetValues(typeof(MirDirection)))
                    ActionList.Add(new DelayedAction(SEnvir.Now.AddMilliseconds((double)(500 + 500 * (int)mirDirection)), ActionType.RangeAttack, new object[1]
                    {
                        mirDirection
                    }));
            }
        }

        private void ScorchedEarth(MirDirection direction)
        {
            if (Dead) return;
            UpdateAttackTime();
            LineAoE(10, 0, 0, MagicType.IgyuScorchedEarth, Element.Fire, direction);
        }

        public override void LineAoE(
          int distance,
          int min,
          int max,
          MagicType magic,
          Element element,
          MirDirection dir)
        {
            List<uint> uintList = new List<uint>();
            List<Point> pointList = new List<Point>();
            Broadcast((Packet)new ObjectMagic()
            {
                ObjectID = ObjectID,
                Direction = dir,
                CurrentLocation = CurrentLocation,
                Cast = true,
                Type = magic,
                Targets = uintList,
                Locations = pointList,
                //AttackElement = Element.None
            });
            UpdateAttackTime();
            for (int i = min; i <= max; ++i)
            {
                MirDirection mirDirection = Functions.ShiftDirection(dir, i);
                for (int distance1 = 1; distance1 <= distance; ++distance1)
                {
                    Point location = Functions.Move(CurrentLocation, mirDirection, distance1);
                    Cell cell1 = CurrentMap.GetCell(location);
                    if (cell1 != null)
                    {
                        pointList.Add(cell1.Location);
                        if (cell1.Objects != null)
                        {
                            foreach (MapObject ob in cell1.Objects)
                            {
                                if (CanAttackTarget(ob))
                                    ActionList.Add(new DelayedAction(SEnvir.Now.AddMilliseconds((double)(500 + distance1 * 75)), ActionType.DelayAttack, new object[3]
                                    {
                    ob,
                    GetMC(),
                    element
                                    }));
                            }
                        }
                        switch (mirDirection)
                        {
                            case MirDirection.Up:
                            case MirDirection.Right:
                            case MirDirection.Down:
                            case MirDirection.Left:
                                Cell cell2 = CurrentMap.GetCell(Functions.Move(location, Functions.ShiftDirection(mirDirection, -2)));
                                if (cell2 != null)
                                {
                                    pointList.Add(cell2.Location);
                                    if (cell2?.Objects != null)
                                    {
                                        foreach (MapObject ob in cell2.Objects)
                                        {
                                            if (CanAttackTarget(ob))
                                                ActionList.Add(new DelayedAction(SEnvir.Now.AddMilliseconds((double)(500 + distance1 * 75)), ActionType.DelayAttack, new object[3]
                                                {
                          ob,
                          GetMC(),
                          element
                                                }));
                                        }
                                    }
                                    Cell cell3 = CurrentMap.GetCell(Functions.Move(location, Functions.ShiftDirection(mirDirection, 2)));
                                    if (cell3 != null)
                                    {
                                        pointList.Add(cell3.Location);
                                        if (cell3?.Objects != null)
                                        {
                                            using (List<MapObject>.Enumerator enumerator = cell3.Objects.GetEnumerator())
                                            {
                                                while (enumerator.MoveNext())
                                                {
                                                    MapObject current = enumerator.Current;
                                                    if (CanAttackTarget(current))
                                                        ActionList.Add(new DelayedAction(SEnvir.Now.AddMilliseconds((double)(500 + distance1 * 75)), ActionType.DelayAttack, new object[3]
                                                        {
                              current,
                              GetMC(),
                              element
                                                        }));
                                                }
                                                break;
                                            }
                                        }
                                        else
                                            break;
                                    }
                                    else
                                        continue;
                                }
                                else
                                    continue;
                            case MirDirection.UpRight:
                            case MirDirection.DownRight:
                            case MirDirection.DownLeft:
                            case MirDirection.UpLeft:
                                Cell cell4 = CurrentMap.GetCell(Functions.Move(location, Functions.ShiftDirection(mirDirection, -1)));
                                if (cell4 != null)
                                {
                                    pointList.Add(cell4.Location);
                                    if (cell4?.Objects != null)
                                    {
                                        foreach (MapObject ob in cell4.Objects)
                                        {
                                            if (CanAttackTarget(ob))
                                                ActionList.Add(new DelayedAction(SEnvir.Now.AddMilliseconds((double)(500 + distance1 * 75)), ActionType.DelayAttack, new object[3]
                                                {
                          ob,
                          GetMC(),
                          element
                                                }));
                                        }
                                    }
                                    Cell cell5 = CurrentMap.GetCell(Functions.Move(location, Functions.ShiftDirection(mirDirection, 1)));
                                    if (cell5 != null)
                                    {
                                        pointList.Add(cell5.Location);
                                        if (cell5?.Objects != null)
                                        {
                                            using (List<MapObject>.Enumerator enumerator = cell5.Objects.GetEnumerator())
                                            {
                                                while (enumerator.MoveNext())
                                                {
                                                    MapObject current = enumerator.Current;
                                                    if (CanAttackTarget(current))
                                                        ActionList.Add(new DelayedAction(SEnvir.Now.AddMilliseconds((double)(500 + distance1 * 75)), ActionType.DelayAttack, new object[3]
                                                        {
                              current,
                              GetMC(),
                              element
                                                        }));
                                                }
                                                break;
                                            }
                                        }
                                        else
                                            break;
                                    }
                                    else
                                        continue;
                                }
                                else
                                    continue;
                        }
                    }
                }
            }
        }

        public override void OnYieldReward(PlayerObject player)
        {
        }
    }
}
