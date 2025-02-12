﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Library;
using Library.Network;
using Library.SystemModels;
using Server.DBModels;
using Server.Envir;
using S = Library.Network.ServerPackets;

namespace Zircon.Server.Models.Monsters
{
    public sealed class Companion : MonsterObject
    {
        public override bool Blocking { get { return false; } }

        public int BagWeight;

        public ItemObject TargetItem;
        public UserCompanion UserCompanion;
        public PlayerObject CompanionOwner;

        public CompanionLevelInfo LevelInfo;


        public UserItem[] Inventory;
        public UserItem[] Equipment;

        public Companion(UserCompanion companion)
        {
            Visible = false;
            PreventSpellCheck = true;
            UserCompanion = companion;

            MonsterInfo = companion.Info.MonsterInfo;

            Equipment = new UserItem[Globals.CompanionEquipmentSize];

            foreach (UserItem item in companion.Items)
            {
                if (item.Slot < Globals.EquipmentOffSet) continue;

                if (item.Slot - Globals.EquipmentOffSet >= Equipment.Length)
                {
                    SEnvir.Log("[Bag Companion Equipment] Slot: {item.Slot}, Character: {UserCompanion.Character.CharacterName}, Companion: {UserCompanion.Name}");
                    continue;
                }

                Equipment[item.Slot - Globals.EquipmentOffSet] = item;
            }

            Inventory = new UserItem[Globals.CompanionInventorySize];

            foreach (UserItem item in companion.Items)
            {
                if (item.Slot >= Globals.EquipmentOffSet) continue;

                if (item.Slot >= Inventory.Length)
                {
                    SEnvir.Log("[Bag Companion Inventory] Slot: {item.Slot}, Character: {UserCompanion.Character.CharacterName}, Companion: {UserCompanion.Name}");
                    continue;
                }

                Inventory[item.Slot] = item;
            }
        }


        public override void ProcessAI()
        {
            if (!(CompanionOwner?.VisibleObjects?.Contains(this) ?? false))
                Recall();

            if (TargetItem != null && (TargetItem.Node == null || TargetItem.CurrentMap != CurrentMap || !Functions.InRange(CurrentLocation, TargetItem.CurrentLocation, ViewRange)))
                TargetItem = null;

            ProcessSearch();
            ProcessRoam();
            ProcessTarget();
        }

        public override void RefreshStats()
        {
            Stats.Clear();
            Stats.Add(MonsterInfo.Stats);

            LevelInfo = SEnvir.CompanionLevelInfoList.Binding.First(x => x.Level == UserCompanion.Level);

            MoveDelay = MonsterInfo.MoveDelay;
            AttackDelay = MonsterInfo.AttackDelay;

            foreach (UserItem item in Equipment)
            {
                if (item == null) continue;

                Stats.Add(item.Info.Stats);
                Stats.Add(item.Stats);
            }

            Stats[Stat.CompanionBagWeight] += LevelInfo.InventoryWeight;
            Stats[Stat.CompanionInventory] += LevelInfo.InventorySpace;
            
            RefreshWeight();
        }

        public void RefreshWeight()
        {
            BagWeight = 0;

            foreach (UserItem item in Inventory)
            {
                if (item == null) continue;

                BagWeight += item.Weight;
            }

            CompanionOwner.Enqueue(new S.CompanionWeightUpdate { BagWeight = BagWeight, MaxBagWeight = Stats[Stat.CompanionBagWeight], InventorySize = Stats[Stat.CompanionInventory] });
        }
        
        public void Recall()
        {
            Cell cell = CompanionOwner.CurrentMap.GetCell(Functions.Move(CompanionOwner.CurrentLocation, CompanionOwner.Direction, -1));

            if (cell == null || cell.Movements != null)
                cell = CompanionOwner.CurrentCell;

            Teleport(CompanionOwner.CurrentMap, cell.Location);
        }

        public override void ProcessSearch()
        {
            if (!CanMove || SEnvir.Now < SearchTime) return;

            int bestDistance = int.MaxValue;

            List<ItemObject> closest = new List<ItemObject>();

            foreach (MapObject ob in CompanionOwner.VisibleObjects)
            {
                if (ob.Race != ObjectType.Item) continue;

                int distance = Functions.Distance(ob.CurrentLocation, CurrentLocation);

                if (distance > ViewRange) continue;

                if (distance > bestDistance) continue;



                ItemObject item = (ItemObject)ob;

                if (item.Item == null) continue;

                if (!item.OwnerList.Contains(CompanionOwner.Character) || !item.MonsterDrop) continue;

                long amount = 0;

                if (item.Item.Info.Effect == ItemEffect.Gold && CompanionOwner.Character.Account.GuildMember != null && CompanionOwner.Character.Account.GuildMember.Guild.GuildTax > 0)
                    amount = (long)Math.Ceiling(item.Item.Count * CompanionOwner.Character.Account.GuildMember.Guild.GuildTax);

                ItemCheck check = new ItemCheck(item.Item, item.Item.Count - amount, item.Item.Flags, item.Item.ExpireTime);
                

                if (!CanGainItems(true, check)) continue;

                int itemIdx = item.Item.Info.Index;
                if (item.Item.Info.Effect == ItemEffect.ItemPart)
                    itemIdx = item.Item.Stats[Stat.ItemIndex];

                if (!CompanionOwner.CompanionMemory.ContainsKey(itemIdx))
                    continue;

                if (distance != bestDistance) closest.Clear();

                closest.Add(item);
                bestDistance = distance;
            }

            if (closest.Count == 0)
            {
                SearchTime = SEnvir.Now.AddSeconds(1);
                return;
            }

            TargetItem = closest[SEnvir.Random.Next(closest.Count)];

        }
        public override void ProcessRoam()
        {
            if (TargetItem != null) return;

            MoveTo(Functions.Move(CompanionOwner.CurrentLocation, CompanionOwner.Direction, -1));
        }
        public override void ProcessTarget()
        {
            if (TargetItem == null) return;

            MoveTo(TargetItem.CurrentLocation);

            if (TargetItem.CurrentLocation != CurrentLocation) return;

            TargetItem.PickUpItem(this);
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

        public void RemoveItem(UserItem item)
        {
            item.Slot = -1;
            item.Character = null;
            item.Account = null;
            item.Mail = null;
            item.Auction = null;
            item.Companion = null;
            item.Guild = null;
            

            item.Flags &= ~UserItemFlags.Locked;
        }

        public bool CanWearItem(UserItem item, CompanionSlot slot)
        {
            if (!Functions.CorrectSlot(item.Info.ItemType, slot) || !CanUseItem(item.Info))
                return false;

            return true;
        }
        public bool CanUseItem(ItemInfo info)
        {
            switch (info.RequiredType)
            {
                case RequiredType.CompanionLevel:
                    if (UserCompanion.Level < info.RequiredAmount) return false;
                    break;
                case RequiredType.MaxCompanionLevel:
                    if (UserCompanion.Level > info.RequiredAmount) return false;
                    break;
            }

            return true;
        }

        public void AutoFeed()
        {
            if (UserCompanion.Hunger > 0) return;

            UserItem item = Equipment[(int) CompanionSlot.Food];

            if (item == null || !CanUseItem(item.Info)) return;


            UserCompanion.Hunger = Math.Min(LevelInfo.MaxHunger, item.Info.Stats[Stat.CompanionHunger]);

            S.ItemChanged result = new S.ItemChanged
            {
                Link = new CellLinkInfo { GridType = GridType.CompanionEquipment, Slot = (int)CompanionSlot.Food },
                Success = true,
            };

            CompanionOwner.Enqueue(result);
            if (item.Count > 1)
            {
                item.Count--;
                result.Link.Count = item.Count;
            }
            else
            {
                RemoveItem(item);
                Equipment[(int)CompanionSlot.Food] = null;
                item.Delete();

                result.Link.Count = 0;
            }
        }

        public void CheckSkills()
        {
            bool result = false;

            if (UserCompanion.Level >= 3 && (UserCompanion.Level3 == null || UserCompanion.Level3.Count == 0))
            {
                UserCompanion.Level3 = GetSkill(3);
                result = true;
            }

            if (UserCompanion.Level >= 5 && (UserCompanion.Level5 == null || UserCompanion.Level5.Count == 0))
            {
                UserCompanion.Level5 = GetSkill(5);
                result = true;
            }

            if (UserCompanion.Level >= 7 && (UserCompanion.Level7 == null || UserCompanion.Level7.Count == 0))
            {
                UserCompanion.Level7 = GetSkill(7);
                result = true;
            }

            if (UserCompanion.Level >= 10 && (UserCompanion.Level10 == null || UserCompanion.Level10.Count == 0))
            {
                UserCompanion.Level10 = GetSkill(10);
                result = true;
            }

            if (UserCompanion.Level >= 11 && (UserCompanion.Level11 == null || UserCompanion.Level11.Count == 0))
            {
                UserCompanion.Level11 = GetSkill(11);
                result = true;
            }

            if (UserCompanion.Level >= 13 && (UserCompanion.Level13 == null || UserCompanion.Level13.Count == 0))
            {
                UserCompanion.Level13 = GetSkill(13);
                result = true;
            }

            if (UserCompanion.Level >= 15 && (UserCompanion.Level15 == null || UserCompanion.Level15.Count == 0))
            {
                UserCompanion.Level15 = GetSkill(15);
                result = true;
            }

            CompanionOwner.CompanionRefreshBuff();

            if (!result) return;

            CompanionOwner.Enqueue(new S.CompanionSkillUpdate
            {
                Level3 = UserCompanion.Level3,
                Level5 = UserCompanion.Level5,
                Level7 = UserCompanion.Level7,
                Level10 = UserCompanion.Level10,
                Level11 = UserCompanion.Level11,
                Level13 = UserCompanion.Level13,
                Level15 = UserCompanion.Level15
            });
            
        }
        public Stats GetSkill(int level)
        {
            int total = 0;

            foreach (CompanionSkillInfo info in SEnvir.CompanionSkillInfoList.Binding)
            {
                if (info.Level != level) continue;

                total += info.Weight;
            }


            Stats lvStats = new Stats();

            int value = SEnvir.Random.Next(total);

            foreach (CompanionSkillInfo info in SEnvir.CompanionSkillInfoList.Binding)
            {
                if (info.Level != level) continue;

                value -= info.Weight;

                if (value >= 0) continue;

                lvStats[info.StatType] = SEnvir.Random.Next( info.MaxAmount) + 1;

                break;
            }


            return lvStats;
        }

        protected override void MoveTo(Point target)
        {
            if (!CanMove || CurrentLocation == target) return;
            
            MirDirection direction = Functions.DirectionFromPoint(CurrentLocation, target);

            int rotation = SEnvir.Random.Next(2) == 0 ? 1 : -1;

            for (int d = 0; d < 8; d++)
            {
                if (Walk(direction)) return;

                direction = Functions.ShiftDirection(direction, rotation);
            }
        }
        public override bool Walk(MirDirection direction)
        {
            Cell cell = CurrentMap.GetCell(Functions.Move(CurrentLocation, direction));
            if (cell == null) return false;

            BuffRemove(BuffType.Invisibility);
            BuffRemove(BuffType.Transparency);

            Direction = direction;

            UpdateMoveTime();

            CurrentCell = cell;//.GetMovement(this);

            RemoveAllObjects();
            AddAllObjects();


            Broadcast(new S.ObjectMove { ObjectID = ObjectID, Direction = direction, Location = CurrentLocation, Distance = 1 });
            return true;
        }

        public bool CanGainItems(bool checkWeight, params ItemCheck[] checks)
        {
            int index = 0;
            foreach (ItemCheck check in checks)
            {
                if ((check.Flags & UserItemFlags.QuestItem) == UserItemFlags.QuestItem) continue;

                long count = check.Count;

                if (check.Info.Effect == ItemEffect.Experience || check.Info.Effect == ItemEffect.Gold) continue;

                if (checkWeight)
                {
                    switch (check.Info.ItemType)
                    {
                        case ItemType.Amulet:
                        case ItemType.Poison:
                            if (BagWeight + check.Info.Weight > Stats[Stat.CompanionBagWeight]) return false;
                            break;
                        default:
                            if (BagWeight + check.Info.Weight * count > Stats[Stat.CompanionBagWeight]) return false;
                            break;
                    }
                }

                if (check.Info.StackSize > 1 && (check.Flags & UserItemFlags.Expirable) != UserItemFlags.Expirable)
                {
                    foreach (UserItem oldItem in Inventory)
                    {
                        if (oldItem == null) continue;

                        if (oldItem.Info != check.Info || oldItem.Count >= check.Info.StackSize) continue;

                        if ((oldItem.Flags & UserItemFlags.Expirable) == UserItemFlags.Expirable) continue;
                        if ((oldItem.Flags & UserItemFlags.Bound) != (check.Flags & UserItemFlags.Bound)) continue;
                        if ((oldItem.Flags & UserItemFlags.Worthless) != (check.Flags & UserItemFlags.Worthless)) continue;
                        if ((oldItem.Flags & UserItemFlags.NonRefinable) != (check.Flags & UserItemFlags.NonRefinable)) continue;
                        if (!oldItem.Stats.Compare(check.Stats)) continue;

                        count -= check.Info.StackSize - oldItem.Count;

                        if (count <= 0) break;
                    }

                    if (count <= 0) break;
                }

                //Start Index
                for (int i = index; i < Stats[Stat.CompanionInventory]; i++)
                {
                    index++;
                    UserItem item = Inventory[i];
                    if (item == null)
                    {
                        count -= check.Info.StackSize;

                        if (count <= 0) break;
                    }
                }

                if (count > 0) return false;
            }

            return true;
        }
        public void GainItem(params UserItem[] items)
        {
            CompanionOwner.Enqueue(new S.CompanionItemsGained { Items = items.Where(x => x.Info.Effect != ItemEffect.Experience).Select(x => x.ToClientInfo()).ToList() });

            HashSet<UserQuest> changedQuests = new HashSet<UserQuest>();

            foreach (UserItem item in items)
            {
                if (item.UserTask != null)
                {
                    if (item.UserTask.Completed) continue;

                    item.UserTask.Amount = Math.Min(item.UserTask.Task.Amount, item.UserTask.Amount + item.Count);

                    changedQuests.Add(item.UserTask.Quest);

                    if (item.UserTask.Completed)
                    {
                        for (int i = item.UserTask.Objects.Count - 1; i >= 0; i--)
                            item.UserTask.Objects[i].Despawn();
                    }

                    item.UserTask = null;
                    item.Flags &= ~UserItemFlags.QuestItem;


                    item.IsTemporary = true;
                    item.Delete();
                    continue;
                }

                if (item.Info.Effect == ItemEffect.Gold)
                {
                    CompanionOwner.Gold += item.Count;
                    item.IsTemporary = true;
                    item.Delete();
                    continue;
                }

                if (item.Info.Effect == ItemEffect.Experience)
                {
                    CompanionOwner.GainExperience(item.Count, false);
                    item.IsTemporary = true;
                    item.Delete();
                    continue;
                }

                bool handled = false;
                if (item.Info.StackSize > 1 && (item.Flags & UserItemFlags.Expirable) != UserItemFlags.Expirable)
                {
                    foreach (UserItem oldItem in Inventory)
                    {
                        if (oldItem == null || oldItem.Info != item.Info || oldItem.Count >= oldItem.Info.StackSize) continue;


                        if ((oldItem.Flags & UserItemFlags.Expirable) == UserItemFlags.Expirable) continue;
                        if ((oldItem.Flags & UserItemFlags.Bound) != (item.Flags & UserItemFlags.Bound)) continue;
                        if ((oldItem.Flags & UserItemFlags.Worthless) != (item.Flags & UserItemFlags.Worthless)) continue;
                        if ((oldItem.Flags & UserItemFlags.NonRefinable) != (item.Flags & UserItemFlags.NonRefinable)) continue;
                        if (!oldItem.Stats.Compare(item.Stats)) continue;

                        if (oldItem.Count + item.Count <= item.Info.StackSize)
                        {
                            oldItem.Count += item.Count;
                            item.IsTemporary = true;
                            item.Delete();
                            handled = true;
                            break;
                        }

                        item.Count -= item.Info.StackSize - oldItem.Count;
                        oldItem.Count = item.Info.StackSize;
                    }
                    if (handled) continue;
                }

                for (int i = 0; i < Stats[Stat.CompanionInventory]; i++)
                {
                    if (Inventory[i] != null) continue;

                    Inventory[i] = item;
                    item.Slot = i;
                    item.Companion = UserCompanion;
                    item.IsTemporary = false;
                    break;
                }
            }

            foreach (UserQuest quest in changedQuests)
                CompanionOwner.Enqueue(new S.QuestChanged { Quest = quest.ToClientInfo() });


            RefreshStats();
        }

        public override bool CanBeSeenBy(PlayerObject ob)
        {
            if (ob == CompanionOwner)
                return base.CanBeSeenBy(ob);

            return CompanionOwner != null && CompanionOwner.CanBeSeenBy(ob);
        }

        public override bool CanDataBeSeenBy(PlayerObject ob)
        {
            return false;
        }

        public override Packet GetInfoPacket(PlayerObject ob)
        {
            return new S.ObjectMonster
            {
                ObjectID = ObjectID,
                MonsterIndex = MonsterInfo.Index,

                Location = CurrentLocation,

                NameColour = NameColour,
                Direction = Direction,

                PetOwner = CompanionOwner.Name,
                
                Poison = Poison,

                Buffs = Buffs.Where(x => x.Visible).Select(x => x.Type).ToList(),

                CompanionObject = new ClientCompanionObject { Name = UserCompanion.Name }
            };
        }
        public override Packet GetDataPacket(PlayerObject ob)
        {
            return null;
        }
    }
}
