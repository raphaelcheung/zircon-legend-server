using System;
using System.Linq;
using Library;
using Library.Network;
using S = Library.Network.ServerPackets;
using Server.DBModels;
using Server.Envir;
using Zircon.Server.Models.Monsters;
using System.Security.Principal;
using System.Text;

namespace Zircon.Server.Models
{
 public sealed class ItemObject : MapObject
    {
        public override ObjectType Race {get{return ObjectType.Item;}}
        public override bool Blocking { get { return false; } }

        public DateTime ExpireTime { get; set; }

        public UserItem Item { get; set; }
        public List<CharacterInfo> OwnerList { get; } = new List<CharacterInfo>();
        public DateTime OwnerExpire { get; set; } = DateTime.MaxValue;

        public bool MonsterDrop { get; set; }

        public bool CharacterContain(CharacterInfo character)
        {
            return OwnerList.Any(c => c.CharacterName == character.CharacterName);
        }

        public string GetCharacterListString()
        {
            StringBuilder sb = new();
            foreach (CharacterInfo character in OwnerList)
            {
                if (sb.Length <= 0) sb.Append(character.CharacterName);
                else sb.Append($"、{character.CharacterName}");
            }

            return sb.ToString();
        }

        public override void Process()
        {
            base.Process();

            if (SEnvir.Now > ExpireTime)
            {
                Despawn();
                return;
            }

            if (SEnvir.Now > OwnerExpire)
                OwnerList.Clear();
        }

        public override void OnDespawned()
        {
            base.OnDespawned();

            if (Item.UserTask != null)
            {
                Item.UserTask.Objects.Remove(this);
                Item.UserTask = null;
                Item.Flags &= ~UserItemFlags.QuestItem;
            }

            Item = null;
            OwnerList.Clear();
        }

        public override void OnSafeDespawn()
        {
            base.OnSafeDespawn();


            if (Item.UserTask != null)
            {
                Item.UserTask.Objects.Remove(this);
                Item.UserTask = null;
                Item.Flags &= ~UserItemFlags.QuestItem;
            }

            Item = null;
            OwnerList.Clear();
        }

        public bool PickUpItem(PlayerObject ob)
        {
            if (OwnerList.Count > 0 && !OwnerList.Contains(ob.Character))
            {
                ob.Connection.ReceiveChat($"无法捡拾他人掉落的物品 【{Item.Info.ItemName}】", MessageType.System);
                return false;
            }

            long amount = 0;

            if (OwnerList.Count > 0 && Item.Info.Effect == ItemEffect.Gold && ob.Character.Account.GuildMember != null && ob.Character.Account.GuildMember.Guild.GuildTax > 0)
                amount = (long)Math.Ceiling(Item.Count * ob.Character.Account.GuildMember.Guild.GuildTax);

            ItemCheck check = new ItemCheck(Item, Item.Count - amount, Item.Flags, Item.ExpireTime);

            if (ob.CanGainItems(false, check))
            {
                if (amount > 0)
                {
                    Item.Count -= amount;

                    ob.Character.Account.GuildMember.Guild.GuildFunds += amount;
                    ob.Character.Account.GuildMember.Guild.DailyGrowth += amount;

                    ob.Character.Account.GuildMember.Guild.DailyContribution += amount;
                    ob.Character.Account.GuildMember.Guild.TotalContribution += amount;

                    ob.Character.Account.GuildMember.DailyContribution += amount;
                    ob.Character.Account.GuildMember.TotalContribution += amount;

                    foreach (GuildMemberInfo member in ob.Character.Account.GuildMember.Guild.Members)
                    {
                        if (member.Account.Connection.Player == null) continue;

                        member.Account.Connection.Enqueue(new S.GuildMemberContribution { Index = ob.Character.Account.GuildMember.Index, Contribution = amount, ObserverPacket = false });
                    }
                }

                Item.UserTask?.Objects?.Remove(this);

                ob.GainItem(Item);
                Despawn();
                return true;
            }

            //Get Max Carry of type
            //Reduce Amount by type.
            //Send updated floor counts
            //Gain New / partial items
            return false;
        }
        public void PickUpItem(Companion ob)
        {
            //SEnvir.Log($"尝试捡拾道具：{Name} 属主：{GetCharacterListString()} 共{OwnerList.Count}个");

            if (OwnerList.Count > 0 && !OwnerList.Contains(ob.CompanionOwner.Character)) return;
            long amount = 0;

            if (OwnerList.Count > 0 && Item.Info.Effect == ItemEffect.Gold && ob.CompanionOwner.Character.Account.GuildMember != null && ob.CompanionOwner.Character.Account.GuildMember.Guild.GuildTax > 0)
                amount = (long)Math.Ceiling(Item.Count * ob.CompanionOwner.Character.Account.GuildMember.Guild.GuildTax);

            ItemCheck check = new ItemCheck(Item, Item.Count - amount, Item.Flags, Item.ExpireTime);

            if (ob.CanGainItems(false, check))
            {
                if (amount > 0)
                {
                    Item.Count -= amount;

                    ob.CompanionOwner.Character.Account.GuildMember.Guild.GuildFunds += amount;
                    ob.CompanionOwner.Character.Account.GuildMember.Guild.DailyGrowth += amount;

                    ob.CompanionOwner.Character.Account.GuildMember.Guild.DailyContribution += amount;
                    ob.CompanionOwner.Character.Account.GuildMember.Guild.TotalContribution += amount;

                    ob.CompanionOwner.Character.Account.GuildMember.DailyContribution += amount;
                    ob.CompanionOwner.Character.Account.GuildMember.TotalContribution += amount;

                    foreach (GuildMemberInfo member in ob.CompanionOwner.Character.Account.GuildMember.Guild.Members)
                    {
                        if (member.Account.Connection.Player == null) continue;

                        member.Account.Connection.Enqueue(new S.GuildMemberContribution { Index = ob.CompanionOwner.Character.Account.GuildMember.Index, Contribution = amount, ObserverPacket = false });
                    }
                }

                Item.UserTask?.Objects?.Remove(this);

                ob.GainItem(Item);
                Despawn();
                return;
            }

            //Get Max Carry of type
            //Reduce Amount by type.
            //Send updated floor counts
            //Gain New / partial items
            return;
        }


        public override void ProcessHPMP()
        {
        }
        public override void ProcessNameColour()
        {
        }
        public override void ProcessBuff()
        {
        }
        public override void ProcessPoison()
        {
        }

        public override bool CanBeSeenBy(PlayerObject ob)
        {
            if (!Config.CanSeeOthersDropped && OwnerList.Count > 0 && !OwnerList.Contains(ob.Character)) return false;

            if (Item.UserTask != null && Item.UserTask.Quest.Character != ob.Character) return false;

            return base.CanBeSeenBy(ob);
        }

        public override void Activate()
        {
            if (Activated) return;

            Activated = true;
            SEnvir.ActiveObjects.Add(this);
        }
        public override void DeActivate()
        {
            return;
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();

            ExpireTime = SEnvir.Now + Config.DropDuration;

            AddAllObjects();

            Activate();
        }
        public override Packet GetInfoPacket(PlayerObject ob)
        {
            return new S.ObjectItem
            {
                ObjectID = ObjectID,  
                Item = Item.ToClientInfo(),
                Location = CurrentLocation,
            };
        }
        public override Packet GetDataPacket(PlayerObject ob)
        {
            return new S.DataObjectItem
            {
                ObjectID = ObjectID,

                MapIndex = CurrentMap.Info.Index,
                CurrentLocation = CurrentLocation,
                 
                ItemIndex = Item.Info.Index,
            };
        }
    }
}
