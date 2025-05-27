using System;
using System.Drawing;
using Library;
using Server.Envir;
using S = Library.Network.ServerPackets;

namespace Zircon.Server.Models.Monsters
{
    public class SpittingSpider : MonsterObject
    {
        protected virtual byte AttackRange { get => 2; }
        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (Target.CurrentLocation == CurrentLocation) return false;

            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);

            if (x > AttackRange || y > AttackRange) return false;


            return x == 0 || x == y || y == 0;
        }

        protected override void Attack()
        {
            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation }); //Animation ?

            UpdateAttackTime();

            LineAttack(AttackRange);
        }
        protected void LineAttack(int distance) => this.LineAttack(distance, this.Direction);

        protected void LineAttack(int distance, MirDirection direction)
        {
            var dc = GetDC();
            for (int distance1 = 1; distance1 <= distance; ++distance1)
            {
                Point location = Functions.Move(this.CurrentLocation, this.Direction, distance1);

                Cell cell = this.CurrentMap.GetCell(location);
                if (cell?.Objects != null)
                {
                    foreach (MapObject ob in cell.Objects)
                    {
                        if (this.CanAttackTarget(ob))
                        {
                            this.ActionList.Add(new DelayedAction(SEnvir.Now.AddMilliseconds(400.0), ActionType.DelayAttack, new object[3]
                            { ob, dc, AttackElement }));
                            break;
                        }
                    }
                }
            }
        }
    }
}
