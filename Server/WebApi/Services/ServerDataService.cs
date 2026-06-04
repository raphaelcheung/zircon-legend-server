using Library;
using Library.SystemModels;
using Server.DBModels;
using Server.Envir;
using Zircon.Server.Models;
using Zircon.Server.Models.Monsters;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using S = Library.Network.ServerPackets;

namespace Server.WebApi.Services
{
    /// <summary>
    /// Service for accessing server data
    /// </summary>
    public class ServerDataService
    {
        /// <summary>
        /// Lock for creating new items to prevent index conflicts in high concurrency scenarios
        /// </summary>
        private static readonly object _itemCreationLock = new object();

        /// <summary>
        /// Lock for updating item data to prevent race conditions
        /// </summary>
        private static readonly object _itemUpdateLock = new object();

        /// <summary>
        /// Lock for updating monster data to prevent race conditions
        /// </summary>
        private static readonly object _monsterUpdateLock = new object();

        /// <summary>
        /// Lock for updating NPC data to prevent race conditions
        /// </summary>
        private static readonly object _npcUpdateLock = new object();

        /// <summary>
        /// Lock for creating new store items to prevent index conflicts in high concurrency scenarios
        /// </summary>
        private static readonly object _storeCreationLock = new object();

        /// <summary>
        /// Lock for updating store data to prevent race conditions
        /// </summary>
        private static readonly object _storeUpdateLock = new object();

        /// <summary>
        /// Lock for creating new quests to prevent index conflicts in high concurrency scenarios
        /// </summary>
        private static readonly object _questCreationLock = new object();

        /// <summary>
        /// Lock for updating quest data to prevent race conditions
        /// </summary>
        private static readonly object _questUpdateLock = new object();

        /// <summary>
        /// Get server start time
        /// </summary>
        public DateTime ServerStartTime { get; } = DateTime.UtcNow;

        #region Dashboard

        /// <summary>
        /// Get online player count
        /// </summary>
        public int GetOnlinePlayerCount()
        {
            return SEnvir.Players?.Count ?? 0;
        }

        /// <summary>
        /// Get total account count
        /// </summary>
        public int GetTotalAccountCount()
        {
            return SEnvir.AccountInfoList?.Count ?? 0;
        }

        /// <summary>
        /// Get total character count
        /// </summary>
        public int GetTotalCharacterCount()
        {
            return SEnvir.CharacterInfoList?.Count ?? 0;
        }

        /// <summary>
        /// Get server uptime
        /// </summary>
        public TimeSpan GetUptime()
        {
            return DateTime.UtcNow - ServerStartTime;
        }

        /// <summary>
        /// Get server status
        /// </summary>
        public bool IsServerRunning()
        {
            return SEnvir.Started;
        }

        #endregion

        #region Players

        /// <summary>
        /// Get online players
        /// </summary>
        public List<OnlinePlayerDto> GetOnlinePlayers()
        {
            var players = SEnvir.Players;
            if (players == null) return new List<OnlinePlayerDto>();

            var result = new List<OnlinePlayerDto>();
            foreach (var player in players)
            {
                try
                {
                    result.Add(new OnlinePlayerDto
                    {
                        CharacterName = player.Character?.CharacterName ?? "Unknown",
                        AccountEmail = player.Character?.Account?.EMailAddress ?? "Unknown",
                        Level = player.Level,
                        Class = player.Class.ToString(),
                        MapName = player.CurrentMap?.Info?.Description ?? "Unknown",
                        OnlineTime = player.Character != null ?
                            (DateTime.UtcNow - player.Character.LastLogin).TotalMinutes : 0
                    });
                }
                catch
                {
                    // Skip invalid player
                }
            }
            return result;
        }

        /// <summary>
        /// Kick a player by name
        /// </summary>
        public bool KickPlayer(string characterName)
        {
            var players = SEnvir.Players;
            if (players == null) return false;

            var player = players.FirstOrDefault(p =>
                string.Equals(p.Character?.CharacterName, characterName, StringComparison.OrdinalIgnoreCase));

            if (player?.Connection != null)
            {
                player.Connection.TryDisconnect();
                return true;
            }
            return false;
        }

        #endregion

        #region Accounts

        /// <summary>
        /// Authenticate account with email and password
        /// </summary>
        public AccountInfo? Authenticate(string email, string password)
        {
            var accounts = SEnvir.AccountInfoList;
            if (accounts == null) return null;

            for (int i = 0; i < accounts.Count; i++)
            {
                var account = accounts[i];
                if (string.Equals(account.EMailAddress, email, StringComparison.OrdinalIgnoreCase))
                {
                    // Check password using the same algorithm as game server
                    if (account.Password != null && SEnvir.PasswordMatch(password, account.Password))
                    {
                        return account;
                    }
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Get account by email
        /// </summary>
        public AccountInfo? GetAccountByEmail(string email)
        {
            var accounts = SEnvir.AccountInfoList;
            if (accounts == null) return null;

            for (int i = 0; i < accounts.Count; i++)
            {
                var account = accounts[i];
                if (string.Equals(account.EMailAddress, email, StringComparison.OrdinalIgnoreCase))
                {
                    return account;
                }
            }
            return null;
        }

        /// <summary>
        /// Check if super admin exists
        /// </summary>
        public bool HasSuperAdmin()
        {
            var accounts = SEnvir.AccountInfoList;
            if (accounts == null) return false;

            for (int i = 0; i < accounts.Count; i++)
            {
                if (accounts[i].Identify == AccountIdentity.SuperAdmin)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Initialize super admin account
        /// </summary>
        public bool InitializeSuperAdmin()
        {
            if (HasSuperAdmin()) return false;

            var accounts = SEnvir.AccountInfoList;
            if (accounts == null) return false;

            const string defaultPassword = "123456";
            string email = SEnvir.SuperAdmin;

            // Check if the default admin account exists
            AccountInfo? account = null;
            for (int i = 0; i < accounts.Count; i++)
            {
                if (string.Equals(accounts[i].EMailAddress, email, StringComparison.OrdinalIgnoreCase))
                {
                    account = accounts[i];
                    break;
                }
            }

            if (account != null)
            {
                // Upgrade existing account to SuperAdmin
                account.Identify = AccountIdentity.SuperAdmin;
                account.Password = SEnvir.CreateHash(defaultPassword);
                account.RealPassword = SEnvir.CreateHash(Functions.CalcMD5($"{email}-{defaultPassword}"));
                account.Activated = true;
            }
            else
            {
                // Create new super admin account
                account = accounts.CreateNewObject();
                account.EMailAddress = email;
                account.Password = SEnvir.CreateHash(defaultPassword);
                account.RealPassword = SEnvir.CreateHash(Functions.CalcMD5($"{email}-{defaultPassword}"));
                account.Identify = AccountIdentity.SuperAdmin;
                account.Activated = true;
                account.CreationDate = DateTime.UtcNow;
                account.CreationIP = "127.0.0.1";
            }

            // Save to database
            SEnvir.SaveUserDatas();
            return true;
        }

        /// <summary>
        /// Get accounts with pagination
        /// </summary>
        public (List<AccountDto> accounts, int total) GetAccounts(int page, int pageSize, string? search = null)
        {
            var accounts = SEnvir.AccountInfoList;
            if (accounts == null) return (new List<AccountDto>(), 0);

            var query = new List<AccountInfo>();
            for (int i = 0; i < accounts.Count; i++)
            {
                var account = accounts[i];
                if (string.IsNullOrEmpty(search) ||
                    account.EMailAddress?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
                {
                    query.Add(account);
                }
            }

            var total = query.Count;
            var result = query
                .OrderByDescending(a => a.CreationDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AccountDto
                {
                    Email = a.EMailAddress ?? "",
                    Identity = a.Identify.ToString(),
                    Banned = a.Banned,
                    BanReason = a.BanReason,
                    CreationDate = a.CreationDate,
                    LastLogin = a.LastLogin,
                    LastIP = a.LastIP,
                    CharacterCount = a.Characters?.Count(c => !c.Deleted) ?? 0,
                    Gold = a.Gold,
                    GameGold = a.GameGold
                })
                .ToList();

            return (result, total);
        }

        /// <summary>
        /// Get account details with characters
        /// </summary>
        public AccountDetailDto? GetAccountDetail(string email)
        {
            var account = GetAccountByEmail(email);
            if (account == null) return null;

            return new AccountDetailDto
            {
                Email = account.EMailAddress ?? "",
                Identity = account.Identify.ToString(),
                Banned = account.Banned,
                BanReason = account.BanReason,
                ExpiryDate = account.ExpiryDate,
                CreationDate = account.CreationDate,
                CreationIP = account.CreationIP,
                LastLogin = account.LastLogin,
                LastIP = account.LastIP,
                Gold = account.Gold,
                GameGold = account.GameGold,
                Characters = account.Characters?
                    .Where(c => !c.Deleted)
                    .Select(c => new CharacterDto
                    {
                        Name = c.CharacterName ?? "",
                        Level = c.Level,
                        Class = c.Class.ToString(),
                        Gender = c.Gender.ToString()
                    })
                    .ToList() ?? new List<CharacterDto>()
            };
        }

        /// <summary>
        /// Ban account
        /// </summary>
        public bool BanAccount(string email, string reason, DateTime? expiryDate = null)
        {
            var account = GetAccountByEmail(email);
            if (account == null) return false;

            account.Banned = true;
            account.BanReason = reason;
            account.ExpiryDate = expiryDate ?? DateTime.MaxValue;
            return true;
        }

        /// <summary>
        /// Unban account
        /// </summary>
        public bool UnbanAccount(string email)
        {
            var account = GetAccountByEmail(email);
            if (account == null) return false;

            account.Banned = false;
            account.BanReason = "";
            account.ExpiryDate = DateTime.MinValue;
            return true;
        }

        /// <summary>
        /// Change account identity
        /// </summary>
        public bool ChangeAccountIdentity(string email, AccountIdentity newIdentity)
        {
            var account = GetAccountByEmail(email);
            if (account == null) return false;

            account.Identify = newIdentity;
            SEnvir.SaveUserDatas();
            return true;
        }

        /// <summary>
        /// Reset account password
        /// </summary>
        public bool ResetPassword(string email, string newPassword)
        {
            var account = GetAccountByEmail(email);
            if (account == null) return false;

            account.Password = SEnvir.CreateHash(newPassword);
            account.RealPassword = SEnvir.CreateHash(Functions.CalcMD5($"{email}-{newPassword}"));
            SEnvir.SaveUserDatas();
            return true;
        }

        /// <summary>
        /// Create new account
        /// </summary>
        public (bool success, string message) CreateAccount(string email, string password, AccountIdentity identity = AccountIdentity.Normal)
        {
            var accounts = SEnvir.AccountInfoList;
            if (accounts == null) return (false, "Server not ready");

            // Check if email already exists
            if (GetAccountByEmail(email) != null)
            {
                return (false, "Email already exists");
            }

            var account = accounts.CreateNewObject();
            account.EMailAddress = email;
            account.Password = SEnvir.CreateHash(password);
            account.RealPassword = SEnvir.CreateHash(Functions.CalcMD5($"{email}-{password}"));
            account.Identify = identity;
            account.Activated = true;
            account.CreationDate = DateTime.UtcNow;
            account.CreationIP = "WebAPI";

            // Save to database
            SEnvir.SaveUserDatas();
            return (true, "Account created successfully");
        }

        #endregion

        #region Characters

        /// <summary>
        /// Get characters with pagination
        /// </summary>
        public (List<CharacterListDto> characters, int total) GetCharacters(int page, int pageSize, string? search = null)
        {
            var characters = SEnvir.CharacterInfoList;
            if (characters == null) return (new List<CharacterListDto>(), 0);

            var query = new List<CharacterInfo>();
            for (int i = 0; i < characters.Count; i++)
            {
                var character = characters[i];
                if (character.Deleted) continue;

                if (string.IsNullOrEmpty(search) ||
                    character.CharacterName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
                {
                    query.Add(character);
                }
            }

            var total = query.Count;
            var result = query
                .OrderByDescending(c => c.Level)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CharacterListDto
                {
                    Name = c.CharacterName ?? "",
                    AccountEmail = c.Account?.EMailAddress ?? "",
                    Level = c.Level,
                    Class = c.Class.ToString(),
                    Gender = c.Gender.ToString(),
                    Gold = 0, // CharacterInfo does not have Gold property
                    LastLogin = c.LastLogin
                })
                .ToList();

            return (result, total);
        }

        /// <summary>
        /// Get character details
        /// </summary>
        public CharacterDetailDto? GetCharacterDetail(string name)
        {
            var character = SEnvir.GetCharacter(name);
            if (character == null) return null;

            return new CharacterDetailDto
            {
                Name = character.CharacterName ?? "",
                AccountEmail = character.Account?.EMailAddress ?? "",
                Level = character.Level,
                Class = character.Class.ToString(),
                Gender = character.Gender.ToString(),
                Gold = 0, // CharacterInfo does not have Gold property
                Experience = (long)character.Experience,
                CurrentHP = character.CurrentHP,
                CurrentMP = character.CurrentMP,
                PKPoints = 0, // CharacterInfo does not have PKPoints property
                LastLogin = character.LastLogin,
                MapName = character.CurrentMap?.Description ?? "Unknown"
            };
        }

        /// <summary>
        /// Set character level
        /// </summary>
        public bool SetCharacterLevel(string characterName, int level)
        {
            var characters = SEnvir.CharacterInfoList;
            if (characters == null) return false;

            CharacterInfo? character = null;
            for (int i = 0; i < characters.Count; i++)
            {
                if (string.Equals(characters[i].CharacterName, characterName, StringComparison.OrdinalIgnoreCase))
                {
                    character = characters[i];
                    break;
                }
            }

            if (character == null) return false;

            character.Level = level;
            SEnvir.SaveUserDatas();
            return true;
        }

        #endregion

        #region Game Data

        /// <summary>
        /// Get items with pagination
        /// </summary>
        public (List<ItemInfoDto> items, int total) GetItems(int page, int pageSize, string? search = null)
        {
            var items = SEnvir.ItemInfoList;
            if (items == null) return (new List<ItemInfoDto>(), 0);

            var query = new List<ItemInfo>();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (string.IsNullOrEmpty(search) ||
                    item.ItemName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
                {
                    query.Add(item);
                }
            }

            var total = query.Count;
            var result = query
                .OrderBy(i => i.Index)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new ItemInfoDto
                {
                    Index = i.Index,
                    Name = i.ItemName ?? "",
                    Type = i.ItemType.ToString(),
                    RequiredType = i.RequiredType.ToString(),
                    RequiredAmount = i.RequiredAmount,
                    Price = i.Price,
                    StackSize = i.StackSize
                })
                .ToList();

            return (result, total);
        }

        /// <summary>
        /// Get item detail by index
        /// </summary>
        public ItemInfoDetailDto? GetItemDetail(int index)
        {
            var items = SEnvir.ItemInfoList;
            if (items == null) return null;

            ItemInfo? item = null;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Index == index)
                {
                    item = items[i];
                    break;
                }
            }

            if (item == null) return null;

            return new ItemInfoDetailDto
            {
                Index = item.Index,
                Name = item.ItemName ?? "",
                Type = item.ItemType.ToString(),
                RequiredType = item.RequiredType.ToString(),
                RequiredAmount = item.RequiredAmount,
                RequiredClass = item.RequiredClass.ToString(),
                RequiredGender = item.RequiredGender.ToString(),
                Price = item.Price,
                StackSize = item.StackSize,
                Shape = item.Shape,
                Effect = item.Effect.ToString(),
                Image = item.Image,
                Durability = item.Durability,
                Weight = item.Weight,
                SellRate = item.SellRate,
                StartItem = item.StartItem,
                CanRepair = item.CanRepair,
                CanSell = item.CanSell,
                CanStore = item.CanStore,
                CanTrade = item.CanTrade,
                CanDrop = item.CanDrop,
                CanDeathDrop = item.CanDeathDrop,
                CanAutoPot = item.CanAutoPot,
                Description = item.Description ?? "",
                Rarity = item.Rarity.ToString(),
                BuffIcon = item.BuffIcon,
                PartCount = item.PartCount,
                BlockMonsterDrop = item.BlockMonsterDrop
            };
        }

        /// <summary>
        /// Update item info
        /// </summary>
        public (bool success, string message) UpdateItem(int index, UpdateItemRequest request)
        {
            // Use lock to prevent race conditions in high concurrency scenarios
            lock (_itemUpdateLock)
            {
                var items = SEnvir.ItemInfoList;
                if (items == null) return (false, "服务器数据未就绪");

                ItemInfo? item = null;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].Index == index)
                    {
                        item = items[i];
                        break;
                    }
                }

                if (item == null) return (false, "物品不存在");

                // Update properties
                if (request.Name != null) item.ItemName = request.Name;
                if (request.Type != null && Enum.TryParse<ItemType>(request.Type, out var itemType)) item.ItemType = itemType;
                if (request.RequiredType != null && Enum.TryParse<RequiredType>(request.RequiredType, out var reqType)) item.RequiredType = reqType;
                if (request.RequiredAmount.HasValue) item.RequiredAmount = request.RequiredAmount.Value;
                if (request.RequiredClass != null && Enum.TryParse<RequiredClass>(request.RequiredClass, out var reqClass)) item.RequiredClass = reqClass;
                if (request.RequiredGender != null && Enum.TryParse<RequiredGender>(request.RequiredGender, out var reqGender)) item.RequiredGender = reqGender;
                if (request.Price.HasValue) item.Price = request.Price.Value;
                if (request.StackSize.HasValue) item.StackSize = request.StackSize.Value;
                if (request.Shape.HasValue) item.Shape = request.Shape.Value;
                if (request.Effect != null && Enum.TryParse<ItemEffect>(request.Effect, out var effect)) item.Effect = effect;
                if (request.Image.HasValue) item.Image = request.Image.Value;
                if (request.Durability.HasValue) item.Durability = request.Durability.Value;
                if (request.Weight.HasValue) item.Weight = request.Weight.Value;
                if (request.SellRate.HasValue) item.SellRate = request.SellRate.Value;
                if (request.StartItem.HasValue) item.StartItem = request.StartItem.Value;
                if (request.CanRepair.HasValue) item.CanRepair = request.CanRepair.Value;
                if (request.CanSell.HasValue) item.CanSell = request.CanSell.Value;
                if (request.CanStore.HasValue) item.CanStore = request.CanStore.Value;
                if (request.CanTrade.HasValue) item.CanTrade = request.CanTrade.Value;
                if (request.CanDrop.HasValue) item.CanDrop = request.CanDrop.Value;
                if (request.CanDeathDrop.HasValue) item.CanDeathDrop = request.CanDeathDrop.Value;
                if (request.CanAutoPot.HasValue) item.CanAutoPot = request.CanAutoPot.Value;
                if (request.Description != null) item.Description = request.Description;
                if (request.Rarity != null && Enum.TryParse<Rarity>(request.Rarity, out var rarity)) item.Rarity = rarity;
                if (request.BuffIcon.HasValue) item.BuffIcon = request.BuffIcon.Value;
                if (request.PartCount.HasValue) item.PartCount = request.PartCount.Value;
                if (request.BlockMonsterDrop.HasValue) item.BlockMonsterDrop = request.BlockMonsterDrop.Value;

                return (true, "物品更新成功");
            }
        }

        /// <summary>
        /// Create new item
        /// </summary>
        public (bool success, string message, ItemInfoDetailDto? item) CreateItem(AddItemRequest request)
        {
            // Use lock to prevent index conflicts in high concurrency scenarios
            lock (_itemCreationLock)
            {
                var items = SEnvir.ItemInfoList;
                if (items == null) return (false, "服务器数据未就绪", null);

                // Check if item index already exists
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].Index == request.Index)
                    {
                        return (false, $"物品ID {request.Index} 已存在", null);
                    }
                }

                // Check if requested index is valid
                if (request.Index <= 0)
                {
                    return (false, "物品ID必须大于0", null);
                }

                // Store current index
                int oldIndex = items.Index;

                try
                {
                    // Set collection index to request.Index - 1 so that CreateNewObject creates item with request.Index
                    items.Index = request.Index - 1;

                    // Create new item (Index will be automatically set to request.Index)
                    var newItem = items.CreateNewObject();
                    newItem.ItemName = request.Name;

                // Parse and set all properties
                if (Enum.TryParse<ItemType>(request.Type, out var itemType)) newItem.ItemType = itemType;
                if (Enum.TryParse<RequiredType>(request.RequiredType, out var reqType)) newItem.RequiredType = reqType;
                newItem.RequiredAmount = request.RequiredAmount;
                if (Enum.TryParse<RequiredClass>(request.RequiredClass, out var reqClass)) newItem.RequiredClass = reqClass;
                if (Enum.TryParse<RequiredGender>(request.RequiredGender, out var reqGender)) newItem.RequiredGender = reqGender;
                newItem.Price = request.Price;
                newItem.StackSize = request.StackSize;
                newItem.Shape = request.Shape;
                if (Enum.TryParse<ItemEffect>(request.Effect, out var effect)) newItem.Effect = effect;
                newItem.Image = request.Image;
                newItem.Durability = request.Durability;
                newItem.Weight = request.Weight;
                newItem.SellRate = request.SellRate;
                newItem.StartItem = request.StartItem;
                newItem.CanRepair = request.CanRepair;
                newItem.CanSell = request.CanSell;
                newItem.CanStore = request.CanStore;
                newItem.CanTrade = request.CanTrade;
                newItem.CanDrop = request.CanDrop;
                newItem.CanDeathDrop = request.CanDeathDrop;
                newItem.CanAutoPot = request.CanAutoPot;
                newItem.Description = request.Description;
                if (Enum.TryParse<Rarity>(request.Rarity, out var rarity)) newItem.Rarity = rarity;
                newItem.BuffIcon = request.BuffIcon;
                newItem.PartCount = request.PartCount;
                newItem.BlockMonsterDrop = request.BlockMonsterDrop;

                SEnvir.Log($"创建新物品: Index={newItem.Index}, Name={newItem.ItemName}, Type={newItem.ItemType}");

                    return (true, "物品创建成功", new ItemInfoDetailDto
                    {
                        Index = newItem.Index,
                        Name = newItem.ItemName ?? "",
                        Type = newItem.ItemType.ToString(),
                        RequiredType = newItem.RequiredType.ToString(),
                        RequiredAmount = newItem.RequiredAmount,
                        RequiredClass = newItem.RequiredClass.ToString(),
                        RequiredGender = newItem.RequiredGender.ToString(),
                        Price = newItem.Price,
                        StackSize = newItem.StackSize,
                        Shape = newItem.Shape,
                        Effect = newItem.Effect.ToString(),
                        Image = newItem.Image,
                        Durability = newItem.Durability,
                        Weight = newItem.Weight,
                        SellRate = newItem.SellRate,
                        StartItem = newItem.StartItem,
                        CanRepair = newItem.CanRepair,
                        CanSell = newItem.CanSell,
                        CanStore = newItem.CanStore,
                        CanTrade = newItem.CanTrade,
                        CanDrop = newItem.CanDrop,
                        CanDeathDrop = newItem.CanDeathDrop,
                        CanAutoPot = newItem.CanAutoPot,
                        Description = newItem.Description ?? "",
                        Rarity = newItem.Rarity.ToString(),
                        BuffIcon = newItem.BuffIcon,
                        PartCount = newItem.PartCount,
                        BlockMonsterDrop = newItem.BlockMonsterDrop
                    });
                }
                catch (Exception ex)
                {
                    SEnvir.Log($"创建物品失败: {ex.Message}");
                    return (false, $"创建失败: {ex.Message}", null);
                }
                finally
                {
                    // Restore collection index to maximum index
                    items.Index = Math.Max(oldIndex, items.Index);
                }
            }
        }

        /// <summary>
        /// Give item to character
        /// </summary>
        public (bool success, string message) GiveItemToCharacter(string characterName, int itemIndex, int count = 1)
        {
            // Find item info
            var items = SEnvir.ItemInfoList;
            if (items == null) return (false, "服务器数据未就绪");

            ItemInfo? itemInfo = null;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Index == itemIndex)
                {
                    itemInfo = items[i];
                    break;
                }
            }

            if (itemInfo == null) return (false, "物品不存在");

            // Check if player is online
            var player = SEnvir.Players?.FirstOrDefault(p =>
                string.Equals(p.Character?.CharacterName, characterName, StringComparison.OrdinalIgnoreCase));

            if (player == null)
            {
                return (false, "角色不在线，请确保角色在线后再试");
            }

            // Create item check
            int giveCount = Math.Min(count, itemInfo.StackSize > 0 ? itemInfo.StackSize : 1);
            var itemCheck = new ItemCheck(itemInfo, giveCount, UserItemFlags.None, TimeSpan.Zero);

            if (!player.CanGainItems(false, itemCheck))
            {
                return (false, "角色背包已满");
            }

            // Create and give item
            var userItem = SEnvir.CreateFreshItem(itemInfo);
            if (userItem == null) return (false, "创建物品失败");

            userItem.Count = giveCount;
            player.GainItem(userItem);

            return (true, $"成功给予 {player.Character?.CharacterName} {giveCount} 个 {itemInfo.ItemName}");
        }

        /// <summary>
        /// Get all item types
        /// </summary>
        public List<EnumValueDto> GetItemTypes()
        {
            return Enum.GetValues<ItemType>()
                .Select(t => new EnumValueDto
                {
                    Value = t.ToString(),
                    Label = GetEnumDescription(t)
                })
                .ToList();
        }

        /// <summary>
        /// Get all required types
        /// </summary>
        public List<EnumValueDto> GetRequiredTypes()
        {
            return Enum.GetValues<RequiredType>()
                .Select(t => new EnumValueDto
                {
                    Value = t.ToString(),
                    Label = GetRequiredTypeLabel(t)
                })
                .ToList();
        }

        /// <summary>
        /// Get all required classes
        /// </summary>
        public List<EnumValueDto> GetRequiredClasses()
        {
            return Enum.GetValues<RequiredClass>()
                .Select(t => new EnumValueDto
                {
                    Value = t.ToString(),
                    Label = GetEnumDescription(t)
                })
                .ToList();
        }

        /// <summary>
        /// Get all rarity types
        /// </summary>
        public List<EnumValueDto> GetRarities()
        {
            return Enum.GetValues<Rarity>()
                .Select(t => new EnumValueDto
                {
                    Value = t.ToString(),
                    Label = GetEnumDescription(t)
                })
                .ToList();
        }

        private string GetEnumDescription<T>(T value) where T : Enum
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
                .FirstOrDefault() as System.ComponentModel.DescriptionAttribute;
            return attribute?.Description ?? value.ToString();
        }

        private string GetRequiredTypeLabel(RequiredType type)
        {
            return type switch
            {
                RequiredType.Level => "等级",
                RequiredType.MaxLevel => "最高等级",
                RequiredType.AC => "防御",
                RequiredType.MR => "魔防",
                RequiredType.DC => "攻击",
                RequiredType.MC => "魔法",
                RequiredType.SC => "道术",
                RequiredType.Health => "生命",
                RequiredType.Mana => "魔法值",
                RequiredType.Accuracy => "准确",
                RequiredType.Agility => "敏捷",
                RequiredType.CompanionLevel => "伙伴等级",
                RequiredType.MaxCompanionLevel => "最高伙伴等级",
                RequiredType.RebirthLevel => "转生等级",
                RequiredType.MaxRebirthLevel => "最高转生等级",
                _ => type.ToString()
            };
        }

        /// <summary>
        /// Get maps
        /// </summary>
        public List<MapInfoDto> GetMaps()
        {
            var maps = SEnvir.MapInfoList;
            if (maps == null) return new List<MapInfoDto>();

            var result = new List<MapInfoDto>();
            for (int i = 0; i < maps.Count; i++)
            {
                var map = maps[i];
                result.Add(new MapInfoDto
                {
                    Index = map.Index,
                    FileName = map.FileName ?? "",
                    MapName = map.Description ?? "",
                    MiniMapIndex = map.MiniMap,
                    Light = map.Light.ToString(),
                    AllowRT = map.AllowRT
                });
            }
            return result.OrderBy(m => m.Index).ToList();
        }

        /// <summary>
        /// Get map regions for NPC location selection
        /// </summary>
        public List<MapRegionDto> GetMapRegions()
        {
            var regions = SEnvir.MapRegionList;
            if (regions == null) return new List<MapRegionDto>();

            var result = new List<MapRegionDto>();
            for (int i = 0; i < regions.Count; i++)
            {
                var region = regions[i];
                result.Add(new MapRegionDto
                {
                    Index = region.Index,
                    Description = region.Description ?? "",
                    MapIndex = region.Map?.Index ?? 0,
                    MapName = region.Map?.Description ?? "",
                    ServerDescription = region.ServerDescription ?? ""
                });
            }
            return result.OrderBy(r => r.MapName).ThenBy(r => r.Description).ToList();
        }

        /// <summary>
        /// Get monsters with pagination
        /// </summary>
        public (List<MonsterInfoDto> monsters, int total) GetMonsters(int page, int pageSize, string? search = null)
        {
            var monsters = SEnvir.MonsterInfoList;
            if (monsters == null) return (new List<MonsterInfoDto>(), 0);

            var query = new List<MonsterInfo>();
            for (int i = 0; i < monsters.Count; i++)
            {
                var monster = monsters[i];
                if (string.IsNullOrEmpty(search) ||
                    monster.MonsterName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
                {
                    query.Add(monster);
                }
            }

            var total = query.Count;
            var result = query
                .OrderBy(m => m.Index)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MonsterInfoDto
                {
                    Index = m.Index,
                    Name = m.MonsterName ?? "",
                    Level = m.Level,
                    IsBoss = m.IsBoss,
                    Experience = (int)m.Experience,
                    ViewRange = m.ViewRange
                })
                .ToList();

            return (result, total);
        }

        /// <summary>
        /// Get NPCs
        /// </summary>
        public List<NpcInfoDto> GetNpcs()
        {
            var npcs = SEnvir.NPCInfoList;
            if (npcs == null) return new List<NpcInfoDto>();

            var result = new List<NpcInfoDto>();
            for (int i = 0; i < npcs.Count; i++)
            {
                var npc = npcs[i];
                result.Add(new NpcInfoDto
                {
                    Index = npc.Index,
                    Name = npc.NPCName ?? "",
                    Image = npc.Image,
                    MapName = npc.Region?.Map?.Description ?? "Unknown"
                });
            }
            return result.OrderBy(n => n.Index).ToList();
        }

        /// <summary>
        /// Get NPC detail by index
        /// </summary>
        public NpcInfoDetailDto? GetNpcDetail(int index)
        {
            var npcs = SEnvir.NPCInfoList;
            if (npcs == null) return null;

            NPCInfo? npcInfo = null;
            for (int i = 0; i < npcs.Count; i++)
            {
                if (npcs[i].Index == index)
                {
                    npcInfo = npcs[i];
                    break;
                }
            }

            if (npcInfo == null) return null;

            var dto = new NpcInfoDetailDto
            {
                Index = npcInfo.Index,
                Name = npcInfo.NPCName ?? "",
                Image = npcInfo.Image,
                MapName = npcInfo.Region?.Map?.Description ?? "",
                MapIndex = npcInfo.Region?.Map?.Index,
                RegionIndex = npcInfo.Region?.Index,
                RegionDescription = npcInfo.Region?.Description ?? "",
                EntryPageSay = npcInfo.EntryPage?.Say ?? "",
                EntryPageDescription = npcInfo.EntryPage?.Description ?? ""
            };

            // Get buttons from entry page with their destination page goods
            if (npcInfo.EntryPage?.Buttons != null)
            {
                foreach (var button in npcInfo.EntryPage.Buttons)
                {
                    var buttonDto = new NpcButtonDto
                    {
                        Index = button.Index,
                        ButtonId = button.ButtonID,
                        DestinationDescription = button.DestinationPage?.Description ?? "",
                        DestinationPageIndex = button.DestinationPage?.Index
                    };

                    // Get goods from destination page
                    if (button.DestinationPage?.Goods != null)
                    {
                        foreach (var good in button.DestinationPage.Goods)
                        {
                            buttonDto.Goods.Add(new NpcGoodDto
                            {
                                Index = good.Index,
                                ItemIndex = good.Item?.Index ?? 0,
                                ItemName = good.Item?.ItemName ?? "",
                                Rate = good.Rate,
                                Cost = good.Cost
                            });
                        }
                    }

                    dto.Buttons.Add(buttonDto);
                }
            }

            // Get goods from entry page (if any)
            if (npcInfo.EntryPage?.Goods != null)
            {
                foreach (var good in npcInfo.EntryPage.Goods)
                {
                    dto.Goods.Add(new NpcGoodDto
                    {
                        Index = good.Index,
                        ItemIndex = good.Item?.Index ?? 0,
                        ItemName = good.Item?.ItemName ?? "",
                        Rate = good.Rate,
                        Cost = good.Cost
                    });
                }
            }

            return dto;
        }

        /// <summary>
        /// Update NPC by index
        /// </summary>
        public (bool success, string message) UpdateNpc(int index, UpdateNpcRequest request)
        {
            // Use lock to prevent race conditions in high concurrency scenarios
            lock (_npcUpdateLock)
            {
                var npcs = SEnvir.NPCInfoList;
                if (npcs == null) return (false, "NPC列表为空");

                NPCInfo? npcInfo = null;
                for (int i = 0; i < npcs.Count; i++)
                {
                    if (npcs[i].Index == index)
                    {
                        npcInfo = npcs[i];
                        break;
                    }
                }

                if (npcInfo == null) return (false, "NPC不存在");

                try
                {
                    // Update basic properties
                    if (request.Name != null)
                        npcInfo.NPCName = request.Name;
                    if (request.Image.HasValue)
                        npcInfo.Image = request.Image.Value;
                    if (request.EntryPageSay != null && npcInfo.EntryPage != null)
                        npcInfo.EntryPage.Say = request.EntryPageSay;

                    // Update region (NPC location)
                    if (request.RegionIndex.HasValue)
                    {
                        var regions = SEnvir.MapRegionList;
                        if (regions != null)
                        {
                            MapRegion? newRegion = null;
                            for (int i = 0; i < regions.Count; i++)
                            {
                                if (regions[i].Index == request.RegionIndex.Value)
                                {
                                    newRegion = regions[i];
                                    break;
                                }
                            }
                            if (newRegion != null)
                            {
                                npcInfo.Region = newRegion;
                            }
                            else
                            {
                                return (false, $"区域 {request.RegionIndex.Value} 不存在");
                            }
                        }
                    }

                    // Update buttons
                    if (request.Buttons != null && npcInfo.EntryPage != null)
                    {
                        var entryPage = npcInfo.EntryPage;
                        var items = SEnvir.ItemInfoList;

                        foreach (var buttonUpdate in request.Buttons)
                        {
                            if (buttonUpdate.Delete && buttonUpdate.Index.HasValue)
                            {
                                // Find and delete existing button
                                NPCButton? toDelete = null;
                                foreach (var btn in entryPage.Buttons)
                                {
                                    if (btn.Index == buttonUpdate.Index.Value)
                                    {
                                        toDelete = btn;
                                        break;
                                    }
                                }
                                if (toDelete != null)
                                {
                                    entryPage.Buttons.Remove(toDelete);
                                    toDelete.Delete();
                                }
                            }
                            else if (buttonUpdate.Index.HasValue)
                            {
                                // Update existing button
                                NPCButton? existingBtn = null;
                                foreach (var btn in entryPage.Buttons)
                                {
                                    if (btn.Index == buttonUpdate.Index.Value)
                                    {
                                        existingBtn = btn;
                                        break;
                                    }
                                }
                                if (existingBtn != null)
                                {
                                    existingBtn.ButtonID = buttonUpdate.ButtonId;
                                    // Note: DestinationPage linking would require more complex logic
                                }
                            }
                            else
                            {
                                // Create new button
                                var newButton = entryPage.Buttons.AddNew();
                                newButton.ButtonID = buttonUpdate.ButtonId;
                            }
                        }
                    }

                    // Update goods (supports both entry page and button destination pages)
                    if (request.Goods != null && npcInfo.EntryPage != null)
                    {
                        var items = SEnvir.ItemInfoList;

                        foreach (var goodUpdate in request.Goods)
                        {
                            // Determine target page (entry page or button's destination page)
                            NPCPage? targetPage = null;
                            if (goodUpdate.ButtonIndex.HasValue)
                            {
                                // Find button and get its destination page
                                foreach (var btn in npcInfo.EntryPage.Buttons)
                                {
                                    if (btn.Index == goodUpdate.ButtonIndex.Value)
                                    {
                                        targetPage = btn.DestinationPage;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                targetPage = npcInfo.EntryPage;
                            }

                            if (targetPage == null) continue;

                            if (goodUpdate.Delete && goodUpdate.Index.HasValue)
                            {
                                // Find and delete existing good
                                NPCGood? toDelete = null;
                                foreach (var good in targetPage.Goods)
                                {
                                    if (good.Index == goodUpdate.Index.Value)
                                    {
                                        toDelete = good;
                                        break;
                                    }
                                }
                                if (toDelete != null)
                                {
                                    targetPage.Goods.Remove(toDelete);
                                    toDelete.Delete();
                                }
                            }
                            else if (goodUpdate.Index.HasValue)
                            {
                                // Update existing good
                                NPCGood? existingGood = null;
                                foreach (var good in targetPage.Goods)
                                {
                                    if (good.Index == goodUpdate.Index.Value)
                                    {
                                        existingGood = good;
                                        break;
                                    }
                                }
                                if (existingGood != null)
                                {
                                    existingGood.Rate = goodUpdate.Rate;
                                    // Update item if changed
                                    if (items != null && existingGood.Item?.Index != goodUpdate.ItemIndex)
                                    {
                                        ItemInfo? newItem = null;
                                        for (int i = 0; i < items.Count; i++)
                                        {
                                            if (items[i].Index == goodUpdate.ItemIndex)
                                            {
                                                newItem = items[i];
                                                break;
                                            }
                                        }
                                        if (newItem != null)
                                        {
                                            existingGood.Item = newItem;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Create new good
                                if (items != null)
                                {
                                    ItemInfo? item = null;
                                    for (int i = 0; i < items.Count; i++)
                                    {
                                        if (items[i].Index == goodUpdate.ItemIndex)
                                        {
                                            item = items[i];
                                            break;
                                        }
                                    }
                                    if (item != null)
                                    {
                                        var newGood = targetPage.Goods.AddNew();
                                        newGood.Item = item;
                                        newGood.Rate = goodUpdate.Rate;
                                    }
                                }
                            }
                        }
                    }

                    return (true, "NPC更新成功");
                }
                catch (Exception ex)
                {
                    return (false, $"更新失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Get magics (skills)
        /// </summary>
        public List<MagicInfoDto> GetMagics()
        {
            var magics = SEnvir.MagicInfoList;
            if (magics == null) return new List<MagicInfoDto>();

            var result = new List<MagicInfoDto>();
            for (int i = 0; i < magics.Count; i++)
            {
                var magic = magics[i];
                result.Add(new MagicInfoDto
                {
                    Index = magic.Index,
                    Name = magic.Name ?? "",
                    Class = magic.Class.ToString(),
                    School = magic.School.ToString(),
                    BaseCost = magic.BaseCost,
                    MaxLevel = 0 // MagicInfo uses MaxLevelPower instead
                });
            }
            return result.OrderBy(m => m.Index).ToList();
        }

        /// <summary>
        /// Get class information
        /// </summary>
        public List<ClassInfoDto> GetClasses()
        {
            return new List<ClassInfoDto>
            {
                new ClassInfoDto { Name = "Warrior", Description = "战士", Enabled = Config.AllowWarrior },
                new ClassInfoDto { Name = "Wizard", Description = "法师", Enabled = Config.AllowWizard },
                new ClassInfoDto { Name = "Taoist", Description = "道士", Enabled = Config.AllowTaoist },
                new ClassInfoDto { Name = "Assassin", Description = "刺客", Enabled = Config.AllowAssassin }
            };
        }

        #region Base Stats

        /// <summary>
        /// Get base stats with optional class filter
        /// </summary>
        public (List<BaseStatDto> stats, int total) GetBaseStats(string? classFilter = null, int page = 1, int pageSize = 50)
        {
            var baseStats = SEnvir.BaseStatList?.Binding;
            if (baseStats == null) return (new List<BaseStatDto>(), 0);

            IEnumerable<BaseStat> query = baseStats;

            // Filter by class
            if (!string.IsNullOrEmpty(classFilter) && Enum.TryParse<MirClass>(classFilter, true, out var mirClass))
            {
                query = query.Where(s => s.Class == mirClass);
            }

            var ordered = query.OrderBy(s => s.Class).ThenBy(s => s.Level).ToList();
            int total = ordered.Count;

            var paged = ordered.Skip((page - 1) * pageSize).Take(pageSize);

            var result = paged.Select(s => new BaseStatDto
            {
                Index = s.Index,
                Class = s.Class.ToString(),
                ClassDescription = GetClassDescription(s.Class),
                Level = s.Level,
                Health = s.Health,
                Mana = s.Mana,
                BagWeight = s.BagWeight,
                WearWeight = s.WearWeight,
                HandWeight = s.HandWeight,
                Accuracy = s.Accuracy,
                Agility = s.Agility,
                MinAC = s.MinAC,
                MaxAC = s.MaxAC,
                MinMR = s.MinMR,
                MaxMR = s.MaxMR,
                MinDC = s.MinDC,
                MaxDC = s.MaxDC,
                MinMC = s.MinMC,
                MaxMC = s.MaxMC,
                MinSC = s.MinSC,
                MaxSC = s.MaxSC
            }).ToList();

            return (result, total);
        }

        /// <summary>
        /// Get base stat by index
        /// </summary>
        public BaseStatDto? GetBaseStat(int index)
        {
            var baseStats = SEnvir.BaseStatList?.Binding;
            if (baseStats == null) return null;

            var stat = baseStats.FirstOrDefault(s => s.Index == index);
            if (stat == null) return null;

            return new BaseStatDto
            {
                Index = stat.Index,
                Class = stat.Class.ToString(),
                ClassDescription = GetClassDescription(stat.Class),
                Level = stat.Level,
                Health = stat.Health,
                Mana = stat.Mana,
                BagWeight = stat.BagWeight,
                WearWeight = stat.WearWeight,
                HandWeight = stat.HandWeight,
                Accuracy = stat.Accuracy,
                Agility = stat.Agility,
                MinAC = stat.MinAC,
                MaxAC = stat.MaxAC,
                MinMR = stat.MinMR,
                MaxMR = stat.MaxMR,
                MinDC = stat.MinDC,
                MaxDC = stat.MaxDC,
                MinMC = stat.MinMC,
                MaxMC = stat.MaxMC,
                MinSC = stat.MinSC,
                MaxSC = stat.MaxSC
            };
        }

        /// <summary>
        /// Create new base stat
        /// </summary>
        public (bool success, string message, BaseStatDto? stat) CreateBaseStat(CreateBaseStatRequest request)
        {
            if (!Enum.TryParse<MirClass>(request.Class, true, out var mirClass))
            {
                return (false, "无效的职业类型", null);
            }

            var baseStats = SEnvir.BaseStatList;
            if (baseStats == null) return (false, "基础属性列表不可用", null);

            // Check if already exists
            var existing = baseStats.Binding.FirstOrDefault(s => s.Class == mirClass && s.Level == request.Level);
            if (existing != null)
            {
                return (false, $"职业 {GetClassDescription(mirClass)} 等级 {request.Level} 的基础属性已存在", null);
            }

            // Create new base stat
            var newStat = baseStats.CreateNewObject();
            newStat.Class = mirClass;
            newStat.Level = request.Level;
            newStat.Health = request.Health;
            newStat.Mana = request.Mana;
            newStat.BagWeight = request.BagWeight;
            newStat.WearWeight = request.WearWeight;
            newStat.HandWeight = request.HandWeight;
            newStat.Accuracy = request.Accuracy;
            newStat.Agility = request.Agility;
            newStat.MinAC = request.MinAC;
            newStat.MaxAC = request.MaxAC;
            newStat.MinMR = request.MinMR;
            newStat.MaxMR = request.MaxMR;
            newStat.MinDC = request.MinDC;
            newStat.MaxDC = request.MaxDC;
            newStat.MinMC = request.MinMC;
            newStat.MaxMC = request.MaxMC;
            newStat.MinSC = request.MinSC;
            newStat.MaxSC = request.MaxSC;

            var dto = new BaseStatDto
            {
                Index = newStat.Index,
                Class = newStat.Class.ToString(),
                ClassDescription = GetClassDescription(newStat.Class),
                Level = newStat.Level,
                Health = newStat.Health,
                Mana = newStat.Mana,
                BagWeight = newStat.BagWeight,
                WearWeight = newStat.WearWeight,
                HandWeight = newStat.HandWeight,
                Accuracy = newStat.Accuracy,
                Agility = newStat.Agility,
                MinAC = newStat.MinAC,
                MaxAC = newStat.MaxAC,
                MinMR = newStat.MinMR,
                MaxMR = newStat.MaxMR,
                MinDC = newStat.MinDC,
                MaxDC = newStat.MaxDC,
                MinMC = newStat.MinMC,
                MaxMC = newStat.MaxMC,
                MinSC = newStat.MinSC,
                MaxSC = newStat.MaxSC
            };

            return (true, "创建成功", dto);
        }

        /// <summary>
        /// Update base stat
        /// </summary>
        public (bool success, string message) UpdateBaseStat(int index, UpdateBaseStatRequest request)
        {
            var baseStats = SEnvir.BaseStatList?.Binding;
            if (baseStats == null) return (false, "基础属性列表不可用");

            var stat = baseStats.FirstOrDefault(s => s.Index == index);
            if (stat == null) return (false, "未找到指定的基础属性");

            // Update fields
            if (request.Health.HasValue) stat.Health = request.Health.Value;
            if (request.Mana.HasValue) stat.Mana = request.Mana.Value;
            if (request.BagWeight.HasValue) stat.BagWeight = request.BagWeight.Value;
            if (request.WearWeight.HasValue) stat.WearWeight = request.WearWeight.Value;
            if (request.HandWeight.HasValue) stat.HandWeight = request.HandWeight.Value;
            if (request.Accuracy.HasValue) stat.Accuracy = request.Accuracy.Value;
            if (request.Agility.HasValue) stat.Agility = request.Agility.Value;
            if (request.MinAC.HasValue) stat.MinAC = request.MinAC.Value;
            if (request.MaxAC.HasValue) stat.MaxAC = request.MaxAC.Value;
            if (request.MinMR.HasValue) stat.MinMR = request.MinMR.Value;
            if (request.MaxMR.HasValue) stat.MaxMR = request.MaxMR.Value;
            if (request.MinDC.HasValue) stat.MinDC = request.MinDC.Value;
            if (request.MaxDC.HasValue) stat.MaxDC = request.MaxDC.Value;
            if (request.MinMC.HasValue) stat.MinMC = request.MinMC.Value;
            if (request.MaxMC.HasValue) stat.MaxMC = request.MaxMC.Value;
            if (request.MinSC.HasValue) stat.MinSC = request.MinSC.Value;
            if (request.MaxSC.HasValue) stat.MaxSC = request.MaxSC.Value;

            return (true, "更新成功");
        }

        /// <summary>
        /// Delete base stat
        /// </summary>
        public (bool success, string message) DeleteBaseStat(int index)
        {
            var baseStats = SEnvir.BaseStatList?.Binding;
            if (baseStats == null) return (false, "基础属性列表不可用");

            var stat = baseStats.FirstOrDefault(s => s.Index == index);
            if (stat == null) return (false, "未找到指定的基础属性");

            stat.Delete();
            return (true, "删除成功");
        }

        #endregion

        #region Quests

        /// <summary>
        /// Get quests with pagination
        /// </summary>
        public (List<QuestInfoDto> quests, int total) GetQuests(int page, int pageSize, string? search = null)
        {
            var quests = SEnvir.QuestInfoList;
            if (quests == null) return (new List<QuestInfoDto>(), 0);

            var query = new List<QuestInfo>();
            for (int i = 0; i < quests.Count; i++)
            {
                var quest = quests[i];
                if (string.IsNullOrEmpty(search) ||
                    quest.QuestName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
                {
                    query.Add(quest);
                }
            }

            var total = query.Count;
            var result = query
                .OrderBy(q => q.Index)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(q => new QuestInfoDto
                {
                    Index = q.Index,
                    QuestName = q.QuestName ?? "",
                    StartNPC = q.StartNPC != null ? new NpcInfoDto
                    {
                        Index = q.StartNPC.Index,
                        Name = q.StartNPC.NPCName ?? ""
                    } : null,
                    FinishNPC = q.FinishNPC != null ? new NpcInfoDto
                    {
                        Index = q.FinishNPC.Index,
                        Name = q.FinishNPC.NPCName ?? ""
                    } : null,
                    TasksCount = q.Tasks?.Count ?? 0,
                    RewardsCount = q.Rewards?.Count ?? 0
                })
                .ToList();

            return (result, total);
        }

        /// <summary>
        /// Get quest detail by index
        /// </summary>
        public QuestInfoDetailDto? GetQuestDetail(int index)
        {
            var quests = SEnvir.QuestInfoList;
            if (quests == null) return null;

            QuestInfo? questInfo = null;
            for (int i = 0; i < quests.Count; i++)
            {
                if (quests[i].Index == index)
                {
                    questInfo = quests[i];
                    break;
                }
            }

            if (questInfo == null) return null;

            var dto = new QuestInfoDetailDto
            {
                Index = questInfo.Index,
                QuestName = questInfo.QuestName ?? "",
                AcceptText = questInfo.AcceptText ?? "",
                ProgressText = questInfo.ProgressText ?? "",
                CompletedText = questInfo.CompletedText ?? "",
                ArchiveText = questInfo.ArchiveText ?? "",
                StartNPC = questInfo.StartNPC != null ? new NpcInfoDto
                {
                    Index = questInfo.StartNPC.Index,
                    Name = questInfo.StartNPC.NPCName ?? ""
                } : null,
                FinishNPC = questInfo.FinishNPC != null ? new NpcInfoDto
                {
                    Index = questInfo.FinishNPC.Index,
                    Name = questInfo.FinishNPC.NPCName ?? ""
                } : null
            };

            // Load requirements
            if (questInfo.Requirements != null)
            {
                foreach (var req in questInfo.Requirements)
                {
                    dto.Requirements.Add(new QuestRequirementDto
                    {
                        Requirement = req.Requirement.ToString(),
                        IntParameter1 = req.IntParameter1,
                        QuestParameter = req.QuestParameter != null ? new QuestInfoDto
                        {
                            Index = req.QuestParameter.Index,
                            QuestName = req.QuestParameter.QuestName ?? ""
                        } : null,
                        Class = req.Class.ToString()
                    });
                }
            }

            // Load tasks
            if (questInfo.Tasks != null)
            {
                foreach (var task in questInfo.Tasks)
                {
                    var taskDto = new QuestTaskDto
                    {
                        Task = task.Task.ToString(),
                        ItemParameter = task.ItemParameter != null ? new ItemInfoDto
                        {
                            Index = task.ItemParameter.Index,
                            Name = task.ItemParameter.ItemName ?? ""
                        } : null,
                        MobDescription = task.MobDescription,
                        Amount = task.Amount
                    };

                    // Load monster details
                    if (task.MonsterDetails != null)
                    {
                        foreach (var detail in task.MonsterDetails)
                        {
                            taskDto.MonsterDetails.Add(new QuestTaskMonsterDetailsDto
                            {
                                Index = detail.Index,
                                MonsterIndex = detail.Monster?.Index,
                                MonsterName = detail.Monster?.MonsterName,
                                MapIndex = detail.Map?.Index,
                                MapName = detail.Map?.Description,
                                Chance = detail.Chance,
                                Amount = detail.Amount,
                                DropSet = detail.DropSet
                            });
                        }
                    }

                    dto.Tasks.Add(taskDto);
                }
            }

            // Load rewards
            if (questInfo.Rewards != null)
            {
                foreach (var reward in questInfo.Rewards)
                {
                    dto.Rewards.Add(new QuestRewardDto
                    {
                        Index = reward.Index,
                        Item = reward.Item != null ? new ItemInfoDto
                        {
                            Index = reward.Item.Index,
                            Name = reward.Item.ItemName ?? ""
                        } : null,
                        Amount = reward.Amount,
                        Class = reward.Class.ToString(),
                        Choice = reward.Choice,
                        Bound = reward.Bound,
                        Duration = reward.Duration
                    });
                }
            }

            return dto;
        }

        /// <summary>
        /// Create new quest
        /// </summary>
        public (bool success, string message, QuestInfoDetailDto? quest) CreateQuest(AddQuestRequest request)
        {
            // Use lock to prevent index conflicts in high concurrency scenarios
            lock (_questCreationLock)
            {
                var quests = SEnvir.QuestInfoList;
                if (quests == null) return (false, "服务器数据未就绪", null);

                if (string.IsNullOrWhiteSpace(request.QuestName))
                {
                    return (false, "任务名称不能为空", null);
                }

                var newQuest = quests.CreateNewObject();
                newQuest.QuestName = request.QuestName;

                // Set NPCs
                var npcs = SEnvir.NPCInfoList;
                if (npcs != null)
                {
                    for (int i = 0; i < npcs.Count; i++)
                    {
                        if (npcs[i].Index == request.StartNPC)
                        {
                            newQuest.StartNPC = npcs[i];
                            break;
                        }
                    }
                    for (int i = 0; i < npcs.Count; i++)
                    {
                        if (npcs[i].Index == request.FinishNPC)
                        {
                            newQuest.FinishNPC = npcs[i];
                            break;
                        }
                    }
                }

                // Save to database
                SEnvir.SaveUserDatas();

                return (true, "任务创建成功", GetQuestDetail(newQuest.Index));
            }
        }

        /// <summary>
        /// Update quest by index
        /// </summary>
        public (bool success, string message) UpdateQuest(int index, UpdateQuestRequest request)
        {
            // Use lock to prevent race conditions in high concurrency scenarios
            lock (_questUpdateLock)
            {
                var quests = SEnvir.QuestInfoList;
                if (quests == null) return (false, "服务器数据未就绪");

                QuestInfo? questInfo = null;
                for (int i = 0; i < quests.Count; i++)
                {
                    if (quests[i].Index == index)
                    {
                        questInfo = quests[i];
                        break;
                    }
                }

                if (questInfo == null) return (false, "任务不存在");

                // Update basic properties
                if (request.QuestName != null) questInfo.QuestName = request.QuestName;
                if (request.AcceptText != null) questInfo.AcceptText = request.AcceptText;
                if (request.ProgressText != null) questInfo.ProgressText = request.ProgressText;
                if (request.CompletedText != null) questInfo.CompletedText = request.CompletedText;
                if (request.ArchiveText != null) questInfo.ArchiveText = request.ArchiveText;

                // Update NPCs
                var npcs = SEnvir.NPCInfoList;
                if (npcs != null && request.StartNPC.HasValue)
                {
                    for (int i = 0; i < npcs.Count; i++)
                    {
                        if (npcs[i].Index == request.StartNPC.Value)
                        {
                            questInfo.StartNPC = npcs[i];
                            break;
                        }
                    }
                }
                if (npcs != null && request.FinishNPC.HasValue)
                {
                    for (int i = 0; i < npcs.Count; i++)
                    {
                        if (npcs[i].Index == request.FinishNPC.Value)
                        {
                            questInfo.FinishNPC = npcs[i];
                            break;
                        }
                    }
                }

                // Note: Requirements, Tasks, and Rewards update logic would be more complex
                // and would require handling create/update/delete operations for child collections
                // For now, we only update basic quest properties

                // Save to database
                SEnvir.SaveUserDatas();

                return (true, "任务更新成功");
            }
        }

        /// <summary>
        /// Delete quest by index
        /// </summary>
        public (bool success, string message) DeleteQuest(int index)
        {
            // Use lock to prevent race conditions in high concurrency scenarios
            lock (_questUpdateLock)
            {
                var quests = SEnvir.QuestInfoList;
                if (quests == null) return (false, "任务列表不可用");

                QuestInfo? quest = null;
                for (int i = 0; i < quests.Count; i++)
                {
                    if (quests[i].Index == index)
                    {
                        quest = quests[i];
                        break;
                    }
                }

                if (quest == null) return (false, "任务不存在");

                // Check if any user quest references this quest
                var userQuests = SEnvir.UserQuestList;
                if (userQuests != null)
                {
                    for (int i = 0; i < userQuests.Count; i++)
                    {
                        if (userQuests[i].QuestInfo == quest)
                        {
                            return (false, "该任务正在被玩家使用，无法删除");
                        }
                    }
                }

                quest.Delete();

                // Save to database
                SEnvir.SaveUserDatas();

                return (true, "删除成功");
            }
        }

        #endregion

        #region Store

        /// <summary>
        /// Get store items with pagination
        /// </summary>
        public (List<StoreInfoDto> items, int total) GetStoreItems(int page, int pageSize, string? search = null)
        {
            var storeItems = SEnvir.StoreInfoList;
            if (storeItems == null) return (new List<StoreInfoDto>(), 0);

            var query = new List<StoreInfo>();
            for (int i = 0; i < storeItems.Count; i++)
            {
                var storeItem = storeItems[i];
                if (string.IsNullOrEmpty(search) ||
                    (storeItem.Item?.ItemName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true))
                {
                    query.Add(storeItem);
                }
            }

            var total = query.Count;
            var result = query
                .OrderBy(s => s.Index)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new StoreInfoDto
                {
                    Index = s.Index,
                    ItemIndex = s.Item?.Index ?? 0,
                    ItemName = s.Item?.ItemName ?? "",
                    Price = s.Price,
                    HuntGoldPrice = s.HuntGoldPrice,
                    Filter = s.Filter ?? "",
                    Available = s.Available,
                    Duration = s.Duration
                })
                .ToList();

            return (result, total);
        }

        /// <summary>
        /// Get store item detail by index
        /// </summary>
        public StoreInfoDto? GetStoreItemDetail(int index)
        {
            var storeItems = SEnvir.StoreInfoList;
            if (storeItems == null) return null;

            StoreInfo? storeItem = null;
            for (int i = 0; i < storeItems.Count; i++)
            {
                if (storeItems[i].Index == index)
                {
                    storeItem = storeItems[i];
                    break;
                }
            }

            if (storeItem == null) return null;

            return new StoreInfoDto
            {
                Index = storeItem.Index,
                ItemIndex = storeItem.Item?.Index ?? 0,
                ItemName = storeItem.Item?.ItemName ?? "",
                Price = storeItem.Price,
                HuntGoldPrice = storeItem.HuntGoldPrice,
                Filter = storeItem.Filter ?? "",
                Available = storeItem.Available,
                Duration = storeItem.Duration
            };
        }

        /// <summary>
        /// Add new store item
        /// </summary>
        public (bool success, string message, StoreInfoDto? item) AddStoreItem(AddStoreItemRequest request)
        {
            // Use lock to prevent index conflicts in high concurrency scenarios
            lock (_storeCreationLock)
            {
                var storeItems = SEnvir.StoreInfoList;
                if (storeItems == null) return (false, "服务器数据未就绪", null);

                // Find item info
                var items = SEnvir.ItemInfoList;
                if (items == null) return (false, "物品列表不可用", null);

                ItemInfo? itemInfo = null;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].Index == request.ItemIndex)
                    {
                        itemInfo = items[i];
                        break;
                    }
                }

                if (itemInfo == null) return (false, $"物品ID {request.ItemIndex} 不存在", null);

                // Check if item already exists in store
                for (int i = 0; i < storeItems.Count; i++)
                {
                    if (storeItems[i].Item == itemInfo)
                    {
                        return (false, $"物品 {itemInfo.ItemName} 已在商城中", null);
                    }
                }

                var newStoreItem = storeItems.CreateNewObject();
                newStoreItem.Item = itemInfo;
                newStoreItem.Price = request.Price;
                newStoreItem.HuntGoldPrice = request.HuntGoldPrice;
                newStoreItem.Filter = request.Filter;
                newStoreItem.Available = request.Available;
                newStoreItem.Duration = request.Duration;

                // Save to database
                SEnvir.SaveUserDatas();

                return (true, "商城商品添加成功", GetStoreItemDetail(newStoreItem.Index));
            }
        }

        /// <summary>
        /// Update store item
        /// </summary>
        public (bool success, string message) UpdateStoreItem(int index, UpdateStoreItemRequest request)
        {
            // Use lock to prevent race conditions in high concurrency scenarios
            lock (_storeUpdateLock)
            {
                var storeItems = SEnvir.StoreInfoList;
                if (storeItems == null) return (false, "服务器数据未就绪");

                StoreInfo? storeItem = null;
                for (int i = 0; i < storeItems.Count; i++)
                {
                    if (storeItems[i].Index == index)
                    {
                        storeItem = storeItems[i];
                        break;
                    }
                }

                if (storeItem == null) return (false, "商城商品不存在");

                // Update properties
                if (request.Price.HasValue) storeItem.Price = request.Price.Value;
                if (request.HuntGoldPrice.HasValue) storeItem.HuntGoldPrice = request.HuntGoldPrice.Value;
                if (request.Filter != null) storeItem.Filter = request.Filter;
                if (request.Available.HasValue) storeItem.Available = request.Available.Value;
                if (request.Duration.HasValue) storeItem.Duration = request.Duration.Value;

                // Save to database
                SEnvir.SaveUserDatas();

                return (true, "商城商品更新成功");
            }
        }

        /// <summary>
        /// Delete store item
        /// </summary>
        public (bool success, string message) DeleteStoreItem(int index)
        {
            // Use lock to prevent race conditions in high concurrency scenarios
            lock (_storeUpdateLock)
            {
                var storeItems = SEnvir.StoreInfoList;
                if (storeItems == null) return (false, "商城列表不可用");

                StoreInfo? storeItem = null;
                for (int i = 0; i < storeItems.Count; i++)
                {
                    if (storeItems[i].Index == index)
                    {
                        storeItem = storeItems[i];
                        break;
                    }
                }

                if (storeItem == null) return (false, "商城商品不存在");

                storeItem.Delete();

                // Save to database
                SEnvir.SaveUserDatas();

                return (true, "删除成功");
            }
        }

        #endregion

        private string GetClassDescription(MirClass mirClass)
        {
            return mirClass switch
            {
                MirClass.Warrior => "战士",
                MirClass.Wizard => "法师",
                MirClass.Taoist => "道士",
                MirClass.Assassin => "刺客",
                _ => mirClass.ToString()
            };
        }

        #endregion

        /// <summary>
        /// Get magic detail by index
        /// </summary>
        public MagicInfoDetailDto? GetMagicDetail(int index)
        {
            var magics = SEnvir.MagicInfoList;
            if (magics == null) return null;

            MagicInfo? magicInfo = null;
            for (int i = 0; i < magics.Count; i++)
            {
                if (magics[i].Index == index)
                {
                    magicInfo = magics[i];
                    break;
                }
            }

            if (magicInfo == null) return null;

            return new MagicInfoDetailDto
            {
                Index = magicInfo.Index,
                Name = magicInfo.Name ?? "",
                Class = magicInfo.Class.ToString(),
                School = magicInfo.School.ToString(),
                BaseCost = magicInfo.BaseCost,
                MaxLevel = Config.技能最高等级,
                LevelCost = magicInfo.LevelCost,
                Delay = magicInfo.Delay,
                Description = magicInfo.Description ?? ""
            };
        }

        /// <summary>
        /// Update magic by index
        /// </summary>
        public (bool success, string message) UpdateMagic(int index, UpdateMagicRequest request)
        {
            var magics = SEnvir.MagicInfoList;
            if (magics == null) return (false, "技能列表为空");

            MagicInfo? magicInfo = null;
            for (int i = 0; i < magics.Count; i++)
            {
                if (magics[i].Index == index)
                {
                    magicInfo = magics[i];
                    break;
                }
            }

            if (magicInfo == null) return (false, "技能不存在");

            try
            {
                if (request.BaseCost.HasValue)
                    magicInfo.BaseCost = request.BaseCost.Value;
                if (request.LevelCost.HasValue)
                    magicInfo.LevelCost = request.LevelCost.Value;
                if (request.Delay.HasValue)
                    magicInfo.Delay = request.Delay.Value;

                return (true, "技能更新成功");
            }
            catch (Exception ex)
            {
                return (false, $"更新失败: {ex.Message}");
            }
        }

        /// <summary>
        /// Grant magic to character
        /// </summary>
        public (bool success, string message) GrantMagicToCharacter(string characterName, int magicIndex, int level)
        {
            // Find magic info
            var magics = SEnvir.MagicInfoList;
            if (magics == null) return (false, "技能列表为空");

            MagicInfo? magicInfo = null;
            for (int i = 0; i < magics.Count; i++)
            {
                if (magics[i].Index == magicIndex)
                {
                    magicInfo = magics[i];
                    break;
                }
            }

            if (magicInfo == null) return (false, "技能不存在");

            // Validate level
            if (level < 0 || level > Config.技能最高等级)
            {
                return (false, $"技能等级必须在0-{Config.技能最高等级}之间");
            }

            // Find online player
            PlayerObject? player = null;
            foreach (var p in SEnvir.Players)
            {
                if (string.Equals(p.Name, characterName, StringComparison.OrdinalIgnoreCase))
                {
                    player = p;
                    break;
                }
            }

            if (player == null)
            {
                return (false, $"角色 {characterName} 未在线");
            }

            try
            {
                // Check if player already has this magic
                if (player.Magics.TryGetValue(magicInfo.Magic, out var existingMagic))
                {
                    // Update existing magic level
                    existingMagic.Level = level;
                    existingMagic.Experience = 0;
                    player.Enqueue(new S.MagicLeveled { InfoIndex = magicInfo.Index, Level = level, Experience = 0 });
                    player.RefreshStats();
                    return (true, $"已将 {player.Name} 的技能 {magicInfo.Name} 等级设置为 {level}");
                }
                else
                {
                    // Create new magic
                    var newMagic = SEnvir.UserMagicList.CreateNewObject();
                    newMagic.Character = player.Character;
                    newMagic.Info = magicInfo;
                    newMagic.Level = level;
                    player.Magics[magicInfo.Magic] = newMagic;
                    player.Enqueue(new S.NewMagic { Magic = newMagic.ToClientInfo() });
                    player.RefreshStats();
                    return (true, $"已给予 {player.Name} 技能 {magicInfo.Name}，等级 {level}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"给予技能失败: {ex.Message}");
            }
        }


        #region Map Management

        /// <summary>
        /// Get map detail by index
        /// </summary>
        public MapInfoDetailDto? GetMapDetail(int index)
        {
            var maps = SEnvir.MapInfoList;
            if (maps == null) return null;

            MapInfo? mapInfo = null;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].Index == index)
                {
                    mapInfo = maps[i];
                    break;
                }
            }

            if (mapInfo == null) return null;

            // Get map instance for size info
            var map = SEnvir.GetMap(mapInfo);
            int width = map?.Width ?? 0;
            int height = map?.Height ?? 0;
            int monsterCount = 0;
            int playerCount = 0;

            if (map != null)
            {
                monsterCount = map.Objects?.Count(o => o is MonsterObject && !(o is NPCObject)) ?? 0;
                playerCount = map.Players?.Count ?? 0;
            }

            return new MapInfoDetailDto
            {
                Index = mapInfo.Index,
                FileName = mapInfo.FileName ?? "",
                MapName = mapInfo.Description ?? "",
                MiniMapIndex = mapInfo.MiniMap,
                Light = mapInfo.Light.ToString(),
                Fight = mapInfo.Fight.ToString(),
                AllowRT = mapInfo.AllowRT,
                AllowTT = mapInfo.AllowTT,
                AllowRecall = mapInfo.AllowRecall,
                CanHorse = mapInfo.CanHorse,
                CanMine = mapInfo.CanMine,
                CanMarriageRecall = mapInfo.CanMarriageRecall,
                MinimumLevel = mapInfo.MinimumLevel,
                MaximumLevel = mapInfo.MaximumLevel,
                MonsterHealth = mapInfo.MonsterHealth,
                MonsterDamage = mapInfo.MonsterDamage,
                DropRate = mapInfo.DropRate,
                ExperienceRate = mapInfo.ExperienceRate,
                GoldRate = mapInfo.GoldRate,
                Width = width,
                Height = height,
                MonsterCount = monsterCount,
                PlayerCount = playerCount
            };
        }

        /// <summary>
        /// Update map info
        /// </summary>
        public (bool success, string message) UpdateMap(int index, UpdateMapRequest request)
        {
            var maps = SEnvir.MapInfoList;
            if (maps == null) return (false, "服务器数据未就绪");

            MapInfo? mapInfo = null;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].Index == index)
                {
                    mapInfo = maps[i];
                    break;
                }
            }

            if (mapInfo == null) return (false, "地图不存在");

            // Update properties
            if (request.MapName != null) mapInfo.Description = request.MapName;
            if (request.Light != null && Enum.TryParse<LightSetting>(request.Light, out var light)) mapInfo.Light = light;
            if (request.Fight != null && Enum.TryParse<FightSetting>(request.Fight, out var fight)) mapInfo.Fight = fight;
            if (request.AllowRT.HasValue) mapInfo.AllowRT = request.AllowRT.Value;
            if (request.AllowTT.HasValue) mapInfo.AllowTT = request.AllowTT.Value;
            if (request.AllowRecall.HasValue) mapInfo.AllowRecall = request.AllowRecall.Value;
            if (request.CanHorse.HasValue) mapInfo.CanHorse = request.CanHorse.Value;
            if (request.CanMine.HasValue) mapInfo.CanMine = request.CanMine.Value;
            if (request.CanMarriageRecall.HasValue) mapInfo.CanMarriageRecall = request.CanMarriageRecall.Value;
            if (request.MinimumLevel.HasValue) mapInfo.MinimumLevel = request.MinimumLevel.Value;
            if (request.MaximumLevel.HasValue) mapInfo.MaximumLevel = request.MaximumLevel.Value;
            if (request.MonsterHealth.HasValue) mapInfo.MonsterHealth = request.MonsterHealth.Value;
            if (request.MonsterDamage.HasValue) mapInfo.MonsterDamage = request.MonsterDamage.Value;
            if (request.DropRate.HasValue) mapInfo.DropRate = request.DropRate.Value;
            if (request.ExperienceRate.HasValue) mapInfo.ExperienceRate = request.ExperienceRate.Value;
            if (request.GoldRate.HasValue) mapInfo.GoldRate = request.GoldRate.Value;

            return (true, "地图更新成功");
        }

        /// <summary>
        /// Teleport player to map
        /// </summary>
        public (bool success, string message) TeleportPlayer(string characterName, int mapIndex, int? x = null, int? y = null)
        {
            // Find player
            var player = SEnvir.Players?.FirstOrDefault(p =>
                string.Equals(p.Character?.CharacterName, characterName, StringComparison.OrdinalIgnoreCase));

            if (player == null)
            {
                return (false, "角色不在线");
            }

            // Find map
            var maps = SEnvir.MapInfoList;
            if (maps == null) return (false, "服务器数据未就绪");

            MapInfo? mapInfo = null;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].Index == mapIndex)
                {
                    mapInfo = maps[i];
                    break;
                }
            }

            if (mapInfo == null) return (false, "地图不存在");

            var map = SEnvir.GetMap(mapInfo);
            if (map == null) return (false, "地图实例不存在");

            // Determine location
            Point location;
            if (x.HasValue && y.HasValue)
            {
                location = new Point(x.Value, y.Value);
            }
            else
            {
                location = map.GetRandomLocation();
            }

            // Teleport
            var result = player.Teleport(map, location);
            if (!string.IsNullOrEmpty(result))
            {
                return (false, result);
            }

            return (true, $"成功将 {player.Character?.CharacterName} 传送到 {mapInfo.Description}");
        }

        /// <summary>
        /// Clear monsters on map
        /// </summary>
        public (bool success, string message, int count) ClearMonstersOnMap(int mapIndex)
        {
            var maps = SEnvir.MapInfoList;
            if (maps == null) return (false, "服务器数据未就绪", 0);

            MapInfo? mapInfo = null;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].Index == mapIndex)
                {
                    mapInfo = maps[i];
                    break;
                }
            }

            if (mapInfo == null) return (false, "地图不存在", 0);

            var map = SEnvir.GetMap(mapInfo);
            if (map == null) return (false, "地图实例不存在", 0);

            int count = 0;
            var objectsToRemove = map.Objects?
                .Where(o => o is MonsterObject && !(o is NPCObject) && !(o is Companion))
                .ToList() ?? new List<MapObject>();

            foreach (var obj in objectsToRemove)
            {
                if (obj is MonsterObject monster)
                {
                    monster.Despawn();
                    count++;
                }
            }

            return (true, $"成功清除 {mapInfo.Description} 上的 {count} 个怪物", count);
        }

        #endregion

        #region Monster Management

        /// <summary>
        /// Get monster detail by index
        /// </summary>
        public MonsterInfoDetailDto? GetMonsterDetail(int index)
        {
            var monsters = SEnvir.MonsterInfoList;
            if (monsters == null) return null;

            MonsterInfo? monster = null;
            for (int i = 0; i < monsters.Count; i++)
            {
                if (monsters[i].Index == index)
                {
                    monster = monsters[i];
                    break;
                }
            }

            if (monster == null) return null;

            return new MonsterInfoDetailDto
            {
                Index = monster.Index,
                Name = monster.MonsterName ?? "",
                Level = monster.Level,
                IsBoss = monster.IsBoss,
                Experience = (int)monster.Experience,
                ViewRange = monster.ViewRange,
                AI = monster.AI,
                AttackDelay = monster.AttackDelay,
                MoveDelay = monster.MoveDelay,
                CoolEye = monster.CoolEye,
                Undead = monster.Undead,
                CanPush = monster.CanPush,
                CanTame = monster.CanTame,
                Stats = GetMonsterStats(monster)
            };
        }

        /// <summary>
        /// Get monster stats from MonsterInfo
        /// </summary>
        private MonsterStatDto GetMonsterStats(MonsterInfo monster)
        {
            var stats = new MonsterStatDto();

            // Extract stats from MonsterInfoStats
            foreach (MonsterInfoStat stat in monster.MonsterInfoStats)
            {
                switch (stat.Stat)
                {
                    case Stat.Health:
                        stats.Health = stat.Amount;
                        break;
                    case Stat.MinDC:
                        stats.MinDC = stat.Amount;
                        break;
                    case Stat.MaxDC:
                        stats.MaxDC = stat.Amount;
                        break;
                    case Stat.MinSC:
                        stats.MinSC = stat.Amount;
                        break;
                    case Stat.MaxSC:
                        stats.MaxSC = stat.Amount;
                        break;
                    case Stat.MinMC:
                        stats.MinMC = stat.Amount;
                        break;
                    case Stat.MaxMC:
                        stats.MaxMC = stat.Amount;
                        break;
                    case Stat.MinAC:
                        stats.MinAC = stat.Amount;
                        break;
                    case Stat.MaxAC:
                        stats.MaxAC = stat.Amount;
                        break;
                    case Stat.MinMR:
                        stats.MinMR = stat.Amount;
                        break;
                    case Stat.MaxMR:
                        stats.MaxMR = stat.Amount;
                        break;
                    case Stat.Accuracy:
                        stats.Accuracy = stat.Amount;
                        break;
                    case Stat.Agility:
                        stats.Agility = stat.Amount;
                        break;
                }
            }

            return stats;
        }

        /// <summary>
        /// Update monster info
        /// </summary>
        public (bool success, string message) UpdateMonster(int index, UpdateMonsterRequest request)
        {
            // Use lock to prevent race conditions in high concurrency scenarios
            lock (_monsterUpdateLock)
            {
                var monsters = SEnvir.MonsterInfoList;
                if (monsters == null) return (false, "服务器数据未就绪");

                MonsterInfo? monster = null;
                for (int i = 0; i < monsters.Count; i++)
                {
                    if (monsters[i].Index == index)
                    {
                        monster = monsters[i];
                        break;
                    }
                }
                if (monster == null) return (false, "怪物不存在");

                // Update basic properties
                if (request.Name != null) monster.MonsterName = request.Name;
                if (request.Level.HasValue) monster.Level = request.Level.Value;
                if (request.IsBoss.HasValue) monster.IsBoss = request.IsBoss.Value;
                if (request.Experience.HasValue) monster.Experience = request.Experience.Value;
                if (request.ViewRange.HasValue) monster.ViewRange = request.ViewRange.Value;
                if (request.AI.HasValue) monster.AI = request.AI.Value;
                if (request.AttackDelay.HasValue) monster.AttackDelay = request.AttackDelay.Value;
                if (request.MoveDelay.HasValue) monster.MoveDelay = request.MoveDelay.Value;
                if (request.CoolEye.HasValue) monster.CoolEye = request.CoolEye.Value;
                if (request.Undead.HasValue) monster.Undead = request.Undead.Value;
                if (request.CanPush.HasValue) monster.CanPush = request.CanPush.Value;
                if (request.CanTame.HasValue) monster.CanTame = request.CanTame.Value;

                // Update stats if provided
                if (request.Stats != null)
                {
                    UpdateMonsterStats(monster, request.Stats);
                }

                return (true, "怪物更新成功");
            }
        }

        /// <summary>
        /// Update monster stats (Health, AC, DC, etc.)
        /// </summary>
        private void UpdateMonsterStats(MonsterInfo monster, MonsterStatDto stats)
        {
            // Clear existing stats and add new ones
            monster.MonsterInfoStats.Clear();

            // Add each stat if it has a value
            if (stats.Health > 0)
            {
                var healthStat = monster.MonsterInfoStats.AddNew();
                healthStat.Stat = Stat.Health;
                healthStat.Amount = stats.Health;
            }

            if (stats.MinDC > 0 || stats.MaxDC > 0)
            {
                if (stats.MinDC > 0)
                {
                    var minDCStat = monster.MonsterInfoStats.AddNew();
                    minDCStat.Stat = Stat.MinDC;
                    minDCStat.Amount = stats.MinDC;
                }
                if (stats.MaxDC > 0)
                {
                    var maxDCStat = monster.MonsterInfoStats.AddNew();
                    maxDCStat.Stat = Stat.MaxDC;
                    maxDCStat.Amount = stats.MaxDC;
                }
            }

            if (stats.MinSC > 0 || stats.MaxSC > 0)
            {
                if (stats.MinSC > 0)
                {
                    var minSCStat = monster.MonsterInfoStats.AddNew();
                    minSCStat.Stat = Stat.MinSC;
                    minSCStat.Amount = stats.MinSC;
                }
                if (stats.MaxSC > 0)
                {
                    var maxSCStat = monster.MonsterInfoStats.AddNew();
                    maxSCStat.Stat = Stat.MaxSC;
                    maxSCStat.Amount = stats.MaxSC;
                }
            }

            if (stats.MinMC > 0 || stats.MaxMC > 0)
            {
                if (stats.MinMC > 0)
                {
                    var minMCStat = monster.MonsterInfoStats.AddNew();
                    minMCStat.Stat = Stat.MinMC;
                    minMCStat.Amount = stats.MinMC;
                }
                if (stats.MaxMC > 0)
                {
                    var maxMCStat = monster.MonsterInfoStats.AddNew();
                    maxMCStat.Stat = Stat.MaxMC;
                    maxMCStat.Amount = stats.MaxMC;
                }
            }

            if (stats.MinAC > 0 || stats.MaxAC > 0)
            {
                if (stats.MinAC > 0)
                {
                    var minACStat = monster.MonsterInfoStats.AddNew();
                    minACStat.Stat = Stat.MinAC;
                    minACStat.Amount = stats.MinAC;
                }
                if (stats.MaxAC > 0)
                {
                    var maxACStat = monster.MonsterInfoStats.AddNew();
                    maxACStat.Stat = Stat.MaxAC;
                    maxACStat.Amount = stats.MaxAC;
                }
            }

            if (stats.MinMR > 0 || stats.MaxMR > 0)
            {
                if (stats.MinMR > 0)
                {
                    var minMRStat = monster.MonsterInfoStats.AddNew();
                    minMRStat.Stat = Stat.MinMR;
                    minMRStat.Amount = stats.MinMR;
                }
                if (stats.MaxMR > 0)
                {
                    var maxMRStat = monster.MonsterInfoStats.AddNew();
                    maxMRStat.Stat = Stat.MaxMR;
                    maxMRStat.Amount = stats.MaxMR;
                }
            }

            if (stats.Accuracy > 0)
            {
                var accuracyStat = monster.MonsterInfoStats.AddNew();
                accuracyStat.Stat = Stat.Accuracy;
                accuracyStat.Amount = stats.Accuracy;
            }

            if (stats.Agility > 0)
            {
                var agilityStat = monster.MonsterInfoStats.AddNew();
                agilityStat.Stat = Stat.Agility;
                agilityStat.Amount = stats.Agility;
            }

            // Refresh the monster's stats cache
            monster.StatsChanged();
        }

        /// <summary>
        /// Clear all instances of a monster type from server
        /// </summary>
        public (bool success, string message, int count) ClearMonstersByType(int monsterIndex)
        {
            var monsters = SEnvir.MonsterInfoList;
            if (monsters == null) return (false, "服务器数据未就绪", 0);

            MonsterInfo? monsterInfo = null;
            for (int i = 0; i < monsters.Count; i++)
            {
                if (monsters[i].Index == monsterIndex)
                {
                    monsterInfo = monsters[i];
                    break;
                }
            }

            if (monsterInfo == null) return (false, "怪物类型不存在", 0);

            int count = 0;
            var objectsToRemove = SEnvir.Objects?
                .Where(o => o is MonsterObject m && m.MonsterInfo == monsterInfo && !(o is NPCObject) && !(o is Companion))
                .ToList() ?? new List<MapObject>();

            foreach (var obj in objectsToRemove)
            {
                if (obj is MonsterObject monster)
                {
                    monster.Despawn();
                    count++;
                }
            }

            return (true, $"成功清除服务器上 {count} 个 {monsterInfo.MonsterName}", count);
        }

        /// <summary>
        /// Spawn monster near player
        /// </summary>
        public (bool success, string message) SpawnMonsterNearPlayer(string characterName, int monsterIndex, int count = 1, int range = 3)
        {
            // Find player
            var player = SEnvir.Players?.FirstOrDefault(p =>
                string.Equals(p.Character?.CharacterName, characterName, StringComparison.OrdinalIgnoreCase));

            if (player == null)
            {
                return (false, "角色不在线");
            }

            // Find monster info
            var monsters = SEnvir.MonsterInfoList;
            if (monsters == null) return (false, "服务器数据未就绪");

            MonsterInfo? monsterInfo = null;
            for (int i = 0; i < monsters.Count; i++)
            {
                if (monsters[i].Index == monsterIndex)
                {
                    monsterInfo = monsters[i];
                    break;
                }
            }

            if (monsterInfo == null) return (false, "怪物类型不存在");

            var map = player.CurrentMap;
            if (map == null) return (false, "玩家当前地图不存在");

            int spawned = 0;
            for (int i = 0; i < count; i++)
            {
                var mob = MonsterObject.GetMonster(monsterInfo);
                if (mob == null) continue;

                // Find a random location near the player
                Point location = Point.Empty;
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    int offsetX = SEnvir.Random.Next(-range, range + 1);
                    int offsetY = SEnvir.Random.Next(-range, range + 1);
                    var testPoint = new Point(player.CurrentLocation.X + offsetX, player.CurrentLocation.Y + offsetY);

                    if (map.GetCell(testPoint) != null)
                    {
                        var cell = map.GetCell(testPoint);
                        if (cell != null && cell.Movements == null)
                        {
                            location = testPoint;
                            break;
                        }
                    }
                }

                if (location == Point.Empty)
                {
                    location = player.CurrentLocation;
                }

                mob.CurrentMap = map;
                mob.CurrentLocation = location;

                if (mob.Spawn(map.Info, location))
                {
                    spawned++;
                }
            }

            if (spawned == 0)
            {
                return (false, "无法在玩家附近生成怪物");
            }

            return (true, $"成功在 {player.Character?.CharacterName} 附近生成 {spawned} 个 {monsterInfo.MonsterName}");
        }

        #endregion

        #region Logs

        /// <summary>
        /// Get system logs
        /// </summary>
        public List<string> GetSystemLogs(int count = 100)
        {
            var logs = new List<string>();
            var displayLogs = SEnvir.DisplayLogs.ToArray();

            foreach (var log in displayLogs.TakeLast(count))
            {
                logs.Add(log);
            }

            return logs;
        }

        /// <summary>
        /// Get chat logs
        /// </summary>
        public List<string> GetChatLogs(int count = 100)
        {
            var logs = new List<string>();
            var chatLogs = SEnvir.DisplayChatLogs.ToArray();

            foreach (var log in chatLogs.TakeLast(count))
            {
                logs.Add(log);
            }

            return logs;
        }

        #endregion

        #region Item Drops

        /// <summary>
        /// Get monsters that drop this item
        /// </summary>
        public List<MonsterDropDto> GetItemDrops(int itemIndex)
        {
            var items = SEnvir.ItemInfoList;
            if (items == null) return new List<MonsterDropDto>();

            ItemInfo? item = null;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Index == itemIndex)
                {
                    item = items[i];
                    break;
                }
            }

            if (item == null) return new List<MonsterDropDto>();

            var drops = SEnvir.DropInfoList;
            if (drops == null) return new List<MonsterDropDto>();

            var result = new List<MonsterDropDto>();
            for (int i = 0; i < drops.Count; i++)
            {
                var drop = drops[i];
                if (drop.Item == item)
                {
                    result.Add(new MonsterDropDto
                    {
                        DropId = drop.Index,
                        MonsterIndex = drop.Monster?.Index ?? 0,
                        MonsterName = drop.Monster?.MonsterName ?? "",
                        ItemIndex = itemIndex,
                        ItemName = item.ItemName ?? "",
                        Chance = drop.Chance,
                        Amount = drop.Amount,
                        DropSet = drop.DropSet,
                        PartOnly = drop.PartOnly,
                        EasterEvent = drop.EasterEvent
                    });
                }
            }

            return result.OrderBy(d => d.MonsterName).ToList();
        }

        #endregion

        #region Monster Drops Management

        /// <summary>
        /// Get drops for a monster
        /// </summary>
        public List<DropInfoDto> GetMonsterDrops(int monsterIndex)
        {
            var monsters = SEnvir.MonsterInfoList;
            if (monsters == null) return new List<DropInfoDto>();

            MonsterInfo? monster = null;
            for (int i = 0; i < monsters.Count; i++)
            {
                if (monsters[i].Index == monsterIndex)
                {
                    monster = monsters[i];
                    break;
                }
            }

            if (monster == null) return new List<DropInfoDto>();

            var drops = SEnvir.DropInfoList;
            if (drops == null) return new List<DropInfoDto>();

            var result = new List<DropInfoDto>();
            for (int i = 0; i < drops.Count; i++)
            {
                var drop = drops[i];
                if (drop.Monster == monster)
                {
                    result.Add(new DropInfoDto
                    {
                        Index = drop.Index,
                        ItemIndex = drop.Item?.Index ?? 0,
                        ItemName = drop.Item?.ItemName ?? "",
                        Chance = drop.Chance,
                        Amount = drop.Amount,
                        DropSet = drop.DropSet,
                        PartOnly = drop.PartOnly,
                        EasterEvent = drop.EasterEvent
                    });
                }
            }

            return result.OrderBy(d => d.ItemName).ToList();
        }

        /// <summary>
        /// Add drop to monster
        /// </summary>
        public (bool success, string message, DropInfoDto? drop) AddMonsterDrop(int monsterIndex, AddDropRequest request)
        {
            var monsters = SEnvir.MonsterInfoList;
            if (monsters == null) return (false, "服务器数据未就绪", null);

            MonsterInfo? monster = null;
            for (int i = 0; i < monsters.Count; i++)
            {
                if (monsters[i].Index == monsterIndex)
                {
                    monster = monsters[i];
                    break;
                }
            }

            if (monster == null) return (false, "怪物不存在", null);

            var items = SEnvir.ItemInfoList;
            if (items == null) return (false, "服务器数据未就绪", null);

            ItemInfo? item = null;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Index == request.ItemIndex)
                {
                    item = items[i];
                    break;
                }
            }

            if (item == null) return (false, "物品不存在", null);

            var dropList = SEnvir.DropInfoList;
            if (dropList == null) return (false, "掉落列表不可用", null);

            var newDrop = dropList.CreateNewObject();
            newDrop.Monster = monster;
            newDrop.Item = item;
            newDrop.Chance = request.Chance;
            newDrop.Amount = request.Amount;
            newDrop.DropSet = request.DropSet;
            newDrop.PartOnly = request.PartOnly;
            newDrop.EasterEvent = request.EasterEvent;

            var dto = new DropInfoDto
            {
                Index = newDrop.Index,
                ItemIndex = item.Index,
                ItemName = item.ItemName ?? "",
                Chance = newDrop.Chance,
                Amount = newDrop.Amount,
                DropSet = newDrop.DropSet,
                PartOnly = newDrop.PartOnly,
                EasterEvent = newDrop.EasterEvent
            };

            return (true, "添加成功", dto);
        }

        /// <summary>
        /// Update monster drop
        /// </summary>
        public (bool success, string message) UpdateMonsterDrop(int monsterIndex, int dropId, UpdateDropRequest request)
        {
            var dropList = SEnvir.DropInfoList;
            if (dropList == null) return (false, "掉落列表不可用");

            DropInfo? drop = null;
            for (int i = 0; i < dropList.Count; i++)
            {
                if (dropList[i].Index == dropId)
                {
                    drop = dropList[i];
                    break;
                }
            }

            if (drop == null) return (false, "掉落信息不存在");

            // Verify the drop belongs to the monster
            if (drop.Monster?.Index != monsterIndex)
            {
                return (false, "掉落信息不属于该怪物");
            }

            if (request.Chance.HasValue) drop.Chance = request.Chance.Value;
            if (request.Amount.HasValue) drop.Amount = request.Amount.Value;
            if (request.DropSet.HasValue) drop.DropSet = request.DropSet.Value;
            if (request.PartOnly.HasValue) drop.PartOnly = request.PartOnly.Value;
            if (request.EasterEvent.HasValue) drop.EasterEvent = request.EasterEvent.Value;

            return (true, "更新成功");
        }

        /// <summary>
        /// Delete monster drop
        /// </summary>
        public (bool success, string message) DeleteMonsterDrop(int monsterIndex, int dropId)
        {
            var dropList = SEnvir.DropInfoList;
            if (dropList == null) return (false, "掉落列表不可用");

            DropInfo? drop = null;
            for (int i = 0; i < dropList.Count; i++)
            {
                if (dropList[i].Index == dropId)
                {
                    drop = dropList[i];
                    break;
                }
            }

            if (drop == null) return (false, $"掉落信息不存在，DropId={dropId}");

            // Verify the drop matches the expected item
            if (drop.Item == null)
            {
                return (false, "掉落信息未关联物品");
            }

            // Verify the drop belongs to the monster
            if (drop.Monster?.Index != monsterIndex)
            {
                return (false, $"掉落信息不属于该怪物，期望MonsterIndex={monsterIndex}，实际MonsterIndex={drop.Monster?.Index}");
            }

            SEnvir.Log($"正在删除掉落记录: DropId={dropId}, MonsterIndex={monsterIndex}, ItemIndex={drop.Item.Index}, MonsterName={drop.Monster?.MonsterName}");

            try
            {
                drop.Delete();
                SEnvir.Log($"删除掉落记录成功: DropId={dropId}");
                return (true, "删除成功");
            }
            catch (Exception ex)
            {
                SEnvir.Log($"删除掉落记录失败: DropId={dropId}, 错误={ex.Message}");
                return (false, $"删除失败: {ex.Message}");
            }
        }

        #endregion

        #region Map Respawns Management

        /// <summary>
        /// Get respawns for a map
        /// </summary>
        public List<RespawnInfoDto> GetMapRespawns(int mapIndex)
        {
            var maps = SEnvir.MapInfoList;
            if (maps == null) return new List<RespawnInfoDto>();

            MapInfo? map = null;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].Index == mapIndex)
                {
                    map = maps[i];
                    break;
                }
            }

            if (map == null) return new List<RespawnInfoDto>();

            var respawns = SEnvir.RespawnInfoList;
            if (respawns == null) return new List<RespawnInfoDto>();

            var result = new List<RespawnInfoDto>();
            for (int i = 0; i < respawns.Count; i++)
            {
                var respawn = respawns[i];
                if (respawn.Region?.Map == map)
                {
                    result.Add(new RespawnInfoDto
                    {
                        Index = respawn.Index,
                        MonsterIndex = respawn.Monster?.Index ?? 0,
                        MonsterName = respawn.Monster?.MonsterName ?? "",
                        RegionIndex = respawn.Region?.Index,
                        RegionDescription = respawn.Region?.Description ?? "",
                        Delay = respawn.Delay,
                        Count = respawn.Count,
                        DropSet = respawn.DropSet,
                        Announce = respawn.Announce,
                        EasterEventChance = respawn.EasterEventChance
                    });
                }
            }

            return result.OrderBy(r => r.RegionDescription).ThenBy(r => r.MonsterName).ToList();
        }

        /// <summary>
        /// Add respawn to map
        /// </summary>
        public (bool success, string message, RespawnInfoDto? respawn) AddMapRespawn(int mapIndex, AddRespawnRequest request)
        {
            var maps = SEnvir.MapInfoList;
            if (maps == null) return (false, "服务器数据未就绪", null);

            MapInfo? map = null;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].Index == mapIndex)
                {
                    map = maps[i];
                    break;
                }
            }

            if (map == null) return (false, "地图不存在", null);

            var monsters = SEnvir.MonsterInfoList;
            if (monsters == null) return (false, "服务器数据未就绪", null);

            MonsterInfo? monster = null;
            for (int i = 0; i < monsters.Count; i++)
            {
                if (monsters[i].Index == request.MonsterIndex)
                {
                    monster = monsters[i];
                    break;
                }
            }

            if (monster == null) return (false, "怪物不存在", null);

            MapRegion? region = null;
            if (request.RegionIndex.HasValue)
            {
                var regions = SEnvir.MapRegionList;
                if (regions != null)
                {
                    for (int i = 0; i < regions.Count; i++)
                    {
                        if (regions[i].Index == request.RegionIndex.Value && regions[i].Map == map)
                        {
                            region = regions[i];
                            break;
                        }
                    }
                }
            }

            var respawnList = SEnvir.RespawnInfoList;
            if (respawnList == null) return (false, "刷新列表不可用", null);

            var newRespawn = respawnList.CreateNewObject();
            newRespawn.Monster = monster;
            newRespawn.Region = region;
            newRespawn.Delay = request.Delay;
            newRespawn.Count = request.Count;
            newRespawn.DropSet = request.DropSet;
            newRespawn.Announce = request.Announce;
            newRespawn.EasterEventChance = request.EasterEventChance;
            newRespawn.EventSpawn = request.EventSpawn;

            var dto = new RespawnInfoDto
            {
                Index = newRespawn.Index,
                MonsterIndex = monster.Index,
                MonsterName = monster.MonsterName ?? "",
                RegionIndex = region?.Index,
                RegionDescription = region?.Description ?? "",
                Delay = newRespawn.Delay,
                Count = newRespawn.Count,
                DropSet = newRespawn.DropSet,
                Announce = newRespawn.Announce,
                EasterEventChance = newRespawn.EasterEventChance
            };

            return (true, "添加成功", dto);
        }

        /// <summary>
        /// Update map respawn
        /// </summary>
        public (bool success, string message) UpdateMapRespawn(int mapIndex, int respawnId, UpdateRespawnRequest request)
        {
            var respawnList = SEnvir.RespawnInfoList;
            if (respawnList == null) return (false, "刷新列表不可用");

            RespawnInfo? respawn = null;
            for (int i = 0; i < respawnList.Count; i++)
            {
                if (respawnList[i].Index == respawnId)
                {
                    respawn = respawnList[i];
                    break;
                }
            }

            if (respawn == null) return (false, "刷新信息不存在");

            // Verify the respawn belongs to the map
            if (respawn.Region?.Map?.Index != mapIndex)
            {
                return (false, "刷新信息不属于该地图");
            }

            // Update monster if changed
            if (request.MonsterIndex.HasValue)
            {
                var monsters = SEnvir.MonsterInfoList;
                if (monsters != null)
                {
                    MonsterInfo? newMonster = null;
                    for (int i = 0; i < monsters.Count; i++)
                    {
                        if (monsters[i].Index == request.MonsterIndex.Value)
                        {
                            newMonster = monsters[i];
                            break;
                        }
                    }
                    if (newMonster != null)
                    {
                        respawn.Monster = newMonster;
                    }
                }
            }

            // Update region if changed
            if (request.RegionIndex.HasValue)
            {
                var regions = SEnvir.MapRegionList;
                if (regions != null)
                {
                    MapRegion? newRegion = null;
                    for (int i = 0; i < regions.Count; i++)
                    {
                        if (regions[i].Index == request.RegionIndex.Value && regions[i].Map == respawn.Region?.Map)
                        {
                            newRegion = regions[i];
                            break;
                        }
                    }
                    respawn.Region = newRegion;
                }
            }

            if (request.Delay.HasValue) respawn.Delay = request.Delay.Value;
            if (request.Count.HasValue) respawn.Count = request.Count.Value;
            if (request.DropSet.HasValue) respawn.DropSet = request.DropSet.Value;
            if (request.Announce.HasValue) respawn.Announce = request.Announce.Value;
            if (request.EasterEventChance.HasValue) respawn.EasterEventChance = request.EasterEventChance.Value;
            if (request.EventSpawn.HasValue) respawn.EventSpawn = request.EventSpawn.Value;

            return (true, "更新成功");
        }

        /// <summary>
        /// Delete map respawn
        /// </summary>
        public (bool success, string message) DeleteMapRespawn(int mapIndex, int respawnId)
        {
            var respawnList = SEnvir.RespawnInfoList;
            if (respawnList == null) return (false, "刷新列表不可用");

            RespawnInfo? respawn = null;
            for (int i = 0; i < respawnList.Count; i++)
            {
                if (respawnList[i].Index == respawnId)
                {
                    respawn = respawnList[i];
                    break;
                }
            }

            if (respawn == null) return (false, "刷新信息不存在");

            // Verify the respawn belongs to the map
            if (respawn.Region?.Map?.Index != mapIndex)
            {
                return (false, "刷新信息不属于该地图");
            }

            respawn.Delete();
            return (true, "删除成功");
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Create password hash using the same algorithm as game server (SEnvir.CreateHash)
        /// </summary>
        private byte[] CreatePasswordHash(string password)
        {
            return SEnvir.CreateHash(password);
        }

        #endregion
    }

    #region DTOs

    public class OnlinePlayerDto
    {
        public string CharacterName { get; set; } = "";
        public string AccountEmail { get; set; } = "";
        public int Level { get; set; }
        public string Class { get; set; } = "";
        public string MapName { get; set; } = "";
        public double OnlineTime { get; set; }
    }

    public class AccountDto
    {
        public string Email { get; set; } = "";
        public string Identity { get; set; } = "";
        public bool Banned { get; set; }
        public string? BanReason { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastLogin { get; set; }
        public string? LastIP { get; set; }
        public int CharacterCount { get; set; }
        public long Gold { get; set; }
        public int GameGold { get; set; }
    }

    public class AccountDetailDto : AccountDto
    {
        public DateTime ExpiryDate { get; set; }
        public string? CreationIP { get; set; }
        public List<CharacterDto> Characters { get; set; } = new();
    }

    public class CharacterDto
    {
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public string Class { get; set; } = "";
        public string Gender { get; set; } = "";
    }

    public class CharacterListDto : CharacterDto
    {
        public string AccountEmail { get; set; } = "";
        public long Gold { get; set; }
        public DateTime LastLogin { get; set; }
    }

    public class CharacterDetailDto : CharacterListDto
    {
        public long Experience { get; set; }
        public int CurrentHP { get; set; }
        public int CurrentMP { get; set; }
        public int PKPoints { get; set; }
        public string MapName { get; set; } = "";
    }

    public class ItemInfoDto
    {
        public int Index { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string RequiredType { get; set; } = "";
        public int RequiredAmount { get; set; }
        public int Price { get; set; }
        public int StackSize { get; set; }
    }

    public class ItemInfoDetailDto : ItemInfoDto
    {
        public string RequiredClass { get; set; } = "";
        public string RequiredGender { get; set; } = "";
        public int Shape { get; set; }
        public string Effect { get; set; } = "";
        public int Image { get; set; }
        public int Durability { get; set; }
        public int Weight { get; set; }
        public decimal SellRate { get; set; }
        public bool StartItem { get; set; }
        public bool CanRepair { get; set; }
        public bool CanSell { get; set; }
        public bool CanStore { get; set; }
        public bool CanTrade { get; set; }
        public bool CanDrop { get; set; }
        public bool CanDeathDrop { get; set; }
        public bool CanAutoPot { get; set; }
        public string Description { get; set; } = "";
        public string Rarity { get; set; } = "";
        public int BuffIcon { get; set; }
        public int PartCount { get; set; }
        public bool BlockMonsterDrop { get; set; }
    }

    public class UpdateItemRequest
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? RequiredType { get; set; }
        public int? RequiredAmount { get; set; }
        public string? RequiredClass { get; set; }
        public string? RequiredGender { get; set; }
        public int? Price { get; set; }
        public int? StackSize { get; set; }
        public int? Shape { get; set; }
        public string? Effect { get; set; }
        public int? Image { get; set; }
        public int? Durability { get; set; }
        public int? Weight { get; set; }
        public decimal? SellRate { get; set; }
        public bool? StartItem { get; set; }
        public bool? CanRepair { get; set; }
        public bool? CanSell { get; set; }
        public bool? CanStore { get; set; }
        public bool? CanTrade { get; set; }
        public bool? CanDrop { get; set; }
        public bool? CanDeathDrop { get; set; }
        public bool? CanAutoPot { get; set; }
        public string? Description { get; set; }
        public string? Rarity { get; set; }
        public int? BuffIcon { get; set; }
        public int? PartCount { get; set; }
        public bool? BlockMonsterDrop { get; set; }
    }

    public class AddItemRequest
    {
        public int Index { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string RequiredType { get; set; } = "Level";
        public int RequiredAmount { get; set; } = 0;
        public string RequiredClass { get; set; } = "All";
        public string RequiredGender { get; set; } = "None";
        public int Price { get; set; } = 0;
        public int StackSize { get; set; } = 1;
        public int Shape { get; set; } = 0;
        public string Effect { get; set; } = "None";
        public int Image { get; set; } = 0;
        public int Durability { get; set; } = 0;
        public int Weight { get; set; } = 0;
        public decimal SellRate { get; set; } = 0.5m;
        public bool StartItem { get; set; } = false;
        public bool CanRepair { get; set; } = false;
        public bool CanSell { get; set; } = false;
        public bool CanStore { get; set; } = false;
        public bool CanTrade { get; set; } = false;
        public bool CanDrop { get; set; } = false;
        public bool CanDeathDrop { get; set; } = false;
        public bool CanAutoPot { get; set; } = false;
        public string Description { get; set; } = "";
        public string Rarity { get; set; } = "Common";
        public int BuffIcon { get; set; } = 0;
        public int PartCount { get; set; } = 0;
        public bool BlockMonsterDrop { get; set; } = false;
    }

    public class GiveItemRequest
    {
        public string CharacterName { get; set; } = "";
        public int ItemIndex { get; set; }
        public int Count { get; set; } = 1;
    }

    public class EnumValueDto
    {
        public string Value { get; set; } = "";
        public string Label { get; set; } = "";
    }

    public class MapInfoDto
    {
        public int Index { get; set; }
        public string FileName { get; set; } = "";
        public string MapName { get; set; } = "";
        public int MiniMapIndex { get; set; }
        public string Light { get; set; } = "";
        public bool AllowRT { get; set; }
    }

    public class MapInfoDetailDto : MapInfoDto
    {
        public string Fight { get; set; } = "";
        public bool AllowTT { get; set; }
        public bool AllowRecall { get; set; }
        public bool CanHorse { get; set; }
        public bool CanMine { get; set; }
        public bool CanMarriageRecall { get; set; }
        public int MinimumLevel { get; set; }
        public int MaximumLevel { get; set; }
        public int MonsterHealth { get; set; }
        public int MonsterDamage { get; set; }
        public int DropRate { get; set; }
        public int ExperienceRate { get; set; }
        public int GoldRate { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int MonsterCount { get; set; }
        public int PlayerCount { get; set; }
    }

    public class UpdateMapRequest
    {
        public string? MapName { get; set; }
        public string? Light { get; set; }
        public string? Fight { get; set; }
        public bool? AllowRT { get; set; }
        public bool? AllowTT { get; set; }
        public bool? AllowRecall { get; set; }
        public bool? CanHorse { get; set; }
        public bool? CanMine { get; set; }
        public bool? CanMarriageRecall { get; set; }
        public int? MinimumLevel { get; set; }
        public int? MaximumLevel { get; set; }
        public int? MonsterHealth { get; set; }
        public int? MonsterDamage { get; set; }
        public int? DropRate { get; set; }
        public int? ExperienceRate { get; set; }
        public int? GoldRate { get; set; }
    }

    public class TeleportRequest
    {
        public string CharacterName { get; set; } = "";
        public int MapIndex { get; set; }
        public int? X { get; set; }
        public int? Y { get; set; }
    }

    public class MonsterInfoDto
    {
        public int Index { get; set; }
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public bool IsBoss { get; set; }
        public int Experience { get; set; }
        public int ViewRange { get; set; }
    }

    public class MonsterInfoDetailDto : MonsterInfoDto
    {
        public int AI { get; set; }
        public int AttackDelay { get; set; }
        public int MoveDelay { get; set; }
        public int CoolEye { get; set; }
        public bool Undead { get; set; }
        public bool CanPush { get; set; }
        public bool CanTame { get; set; }
        public MonsterStatDto? Stats { get; set; }
    }

    public class MonsterStatDto
    {
        public int Health { get; set; }
        public int MinDC { get; set; }
        public int MaxDC { get; set; }
        public int MinSC { get; set; }
        public int MaxSC { get; set; }
        public int MinMC { get; set; }
        public int MaxMC { get; set; }
        public int MinAC { get; set; }
        public int MaxAC { get; set; }
        public int MinMR { get; set; }
        public int MaxMR { get; set; }
        public int Accuracy { get; set; }
        public int Agility { get; set; }
    }

    public class UpdateMonsterRequest
    {
        public string? Name { get; set; }
        public int? Level { get; set; }
        public bool? IsBoss { get; set; }
        public long? Experience { get; set; }
        public int? ViewRange { get; set; }
        public int? AI { get; set; }
        public int? AttackDelay { get; set; }
        public int? MoveDelay { get; set; }
        public int? CoolEye { get; set; }
        public bool? Undead { get; set; }
        public bool? CanPush { get; set; }
        public bool? CanTame { get; set; }
        public MonsterStatDto? Stats { get; set; }
    }

    public class SpawnMonsterRequest
    {
        public string CharacterName { get; set; } = "";
        public int MonsterIndex { get; set; }
        public int Count { get; set; } = 1;
        public int Range { get; set; } = 3;
    }

    public class NpcInfoDto
    {
        public int Index { get; set; }
        public string Name { get; set; } = "";
        public int Image { get; set; }
        public string MapName { get; set; } = "";
    }

    public class MagicInfoDto
    {
        public int Index { get; set; }
        public string Name { get; set; } = "";
        public string Class { get; set; } = "";
        public string School { get; set; } = "";
        public int BaseCost { get; set; }
        public int MaxLevel { get; set; }
    }

    public class MagicInfoDetailDto : MagicInfoDto
    {
        public int LevelCost { get; set; }
        public int Range { get; set; }
        public int Delay { get; set; }
        public string Description { get; set; } = "";
    }

    public class ClassInfoDto
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool Enabled { get; set; }
    }

    // NPC DTOs
    public class NpcInfoDetailDto : NpcInfoDto
    {
        public int? MapIndex { get; set; }
        public int? RegionIndex { get; set; }
        public string RegionDescription { get; set; } = "";
        public string EntryPageSay { get; set; } = "";
        public string EntryPageDescription { get; set; } = "";
        public List<NpcButtonDto> Buttons { get; set; } = new();
        public List<NpcGoodDto> Goods { get; set; } = new();
    }

    public class NpcButtonDto
    {
        public int Index { get; set; }
        public int ButtonId { get; set; }
        public string DestinationDescription { get; set; } = "";
        public int? DestinationPageIndex { get; set; }
        public List<NpcGoodDto> Goods { get; set; } = new();
    }

    public class NpcGoodDto
    {
        public int Index { get; set; }
        public int ItemIndex { get; set; }
        public string ItemName { get; set; } = "";
        public decimal Rate { get; set; }
        public int Cost { get; set; }
    }

    public class UpdateNpcRequest
    {
        public string? Name { get; set; }
        public int? Image { get; set; }
        public string? EntryPageSay { get; set; }
        public int? RegionIndex { get; set; }
        public List<NpcButtonUpdateDto>? Buttons { get; set; }
        public List<NpcGoodUpdateDto>? Goods { get; set; }
    }

    public class NpcButtonUpdateDto
    {
        public int? Index { get; set; }  // null for new button
        public int ButtonId { get; set; }
        public int? DestinationPageIndex { get; set; }
        public bool Delete { get; set; } = false;
    }

    public class NpcGoodUpdateDto
    {
        public int? Index { get; set; }  // null for new good
        public int? ButtonIndex { get; set; }  // null = entry page, otherwise = button's destination page
        public int ItemIndex { get; set; }
        public decimal Rate { get; set; } = 1M;
        public bool Delete { get; set; } = false;
    }

    public class MapRegionDto
    {
        public int Index { get; set; }
        public string Description { get; set; } = "";
        public int MapIndex { get; set; }
        public string MapName { get; set; } = "";
        public string ServerDescription { get; set; } = "";
    }

    // Magic/Skill DTOs
    public class UpdateMagicRequest
    {
        public int? BaseCost { get; set; }
        public int? LevelCost { get; set; }
        public int? Range { get; set; }
        public int? Delay { get; set; }
    }

    public class GrantMagicRequest
    {
        public string CharacterName { get; set; } = "";
        public int MagicIndex { get; set; }
        public int Level { get; set; } = 1;
    }

    // Base Stats DTOs
    public class BaseStatDto
    {
        public int Index { get; set; }
        public string Class { get; set; } = "";
        public string ClassDescription { get; set; } = "";
        public int Level { get; set; }
        public int Health { get; set; }
        public int Mana { get; set; }
        public int BagWeight { get; set; }
        public int WearWeight { get; set; }
        public int HandWeight { get; set; }
        public int Accuracy { get; set; }
        public int Agility { get; set; }
        public int MinAC { get; set; }
        public int MaxAC { get; set; }
        public int MinMR { get; set; }
        public int MaxMR { get; set; }
        public int MinDC { get; set; }
        public int MaxDC { get; set; }
        public int MinMC { get; set; }
        public int MaxMC { get; set; }
        public int MinSC { get; set; }
        public int MaxSC { get; set; }
    }

    public class CreateBaseStatRequest
    {
        public string Class { get; set; } = "";
        public int Level { get; set; }
        public int Health { get; set; }
        public int Mana { get; set; }
        public int BagWeight { get; set; }
        public int WearWeight { get; set; }
        public int HandWeight { get; set; }
        public int Accuracy { get; set; }
        public int Agility { get; set; }
        public int MinAC { get; set; }
        public int MaxAC { get; set; }
        public int MinMR { get; set; }
        public int MaxMR { get; set; }
        public int MinDC { get; set; }
        public int MaxDC { get; set; }
        public int MinMC { get; set; }
        public int MaxMC { get; set; }
        public int MinSC { get; set; }
        public int MaxSC { get; set; }
    }

    public class UpdateBaseStatRequest
    {
        public int? Health { get; set; }
        public int? Mana { get; set; }
        public int? BagWeight { get; set; }
        public int? WearWeight { get; set; }
        public int? HandWeight { get; set; }
        public int? Accuracy { get; set; }
        public int? Agility { get; set; }
        public int? MinAC { get; set; }
        public int? MaxAC { get; set; }
        public int? MinMR { get; set; }
        public int? MaxMR { get; set; }
        public int? MinDC { get; set; }
        public int? MaxDC { get; set; }
        public int? MinMC { get; set; }
        public int? MaxMC { get; set; }
        public int? MinSC { get; set; }
        public int? MaxSC { get; set; }
    }

    // Drop and Respawn DTOs
    public class MonsterDropDto
    {
        public int DropId { get; set; }
        public int MonsterIndex { get; set; }
        public string MonsterName { get; set; } = "";
        public int ItemIndex { get; set; }
        public string ItemName { get; set; } = "";
        public int Chance { get; set; }
        public int Amount { get; set; }
        public int DropSet { get; set; }
        public bool PartOnly { get; set; }
        public bool EasterEvent { get; set; }
    }

    public class DropInfoDto
    {
        public int Index { get; set; }
        public int ItemIndex { get; set; }
        public string ItemName { get; set; } = "";
        public int Chance { get; set; }
        public int Amount { get; set; }
        public int DropSet { get; set; }
        public bool PartOnly { get; set; }
        public bool EasterEvent { get; set; }
    }

    public class AddDropRequest
    {
        public int ItemIndex { get; set; }
        public int Chance { get; set; }
        public int Amount { get; set; } = 1;
        public int DropSet { get; set; }
        public bool PartOnly { get; set; }
        public bool EasterEvent { get; set; }
    }

    public class UpdateDropRequest
    {
        public int? Chance { get; set; }
        public int? Amount { get; set; }
        public int? DropSet { get; set; }
        public bool? PartOnly { get; set; }
        public bool? EasterEvent { get; set; }
    }

    public class RespawnInfoDto
    {
        public int Index { get; set; }
        public int MonsterIndex { get; set; }
        public string MonsterName { get; set; } = "";
        public int? RegionIndex { get; set; }
        public string RegionDescription { get; set; } = "";
        public int Delay { get; set; }
        public int Count { get; set; }
        public int DropSet { get; set; }
        public bool Announce { get; set; }
        public int EasterEventChance { get; set; }
    }

    public class AddRespawnRequest
    {
        public int MonsterIndex { get; set; }
        public int? RegionIndex { get; set; }
        public int Delay { get; set; }
        public int Count { get; set; } = 1;
        public int DropSet { get; set; }
        public bool Announce { get; set; }
        public int EasterEventChance { get; set; }
        public bool EventSpawn { get; set; }
    }

    public class UpdateRespawnRequest
    {
        public int? MonsterIndex { get; set; }
        public int? RegionIndex { get; set; }
        public int? Delay { get; set; }
        public int? Count { get; set; }
        public int? DropSet { get; set; }
        public bool? Announce { get; set; }
        public int? EasterEventChance { get; set; }
        public bool? EventSpawn { get; set; }
    }

    #endregion

    #region Quest

    public class QuestInfoDto
    {
        public int Index { get; set; }
        public string QuestName { get; set; } = "";
        public NpcInfoDto? StartNPC { get; set; }
        public NpcInfoDto? FinishNPC { get; set; }
        public int TasksCount { get; set; }
        public int RewardsCount { get; set; }
    }

    public class QuestInfoDetailDto : QuestInfoDto
    {
        public string AcceptText { get; set; } = "";
        public string ProgressText { get; set; } = "";
        public string CompletedText { get; set; } = "";
        public string ArchiveText { get; set; } = "";
        public List<QuestRequirementDto> Requirements { get; set; } = new();
        public List<QuestTaskDto> Tasks { get; set; } = new();
        public List<QuestRewardDto> Rewards { get; set; } = new();
    }

    public class QuestRequirementDto
    {
        public string Requirement { get; set; } = "";
        public int IntParameter1 { get; set; }
        public QuestInfoDto? QuestParameter { get; set; }
        public string Class { get; set; } = "";
    }

    public class QuestTaskDto
    {
        public string Task { get; set; } = "";
        public ItemInfoDto? ItemParameter { get; set; }
        public string? MobDescription { get; set; }
        public int Amount { get; set; }
        public List<QuestTaskMonsterDetailsDto> MonsterDetails { get; set; } = new();
    }

    public class QuestTaskMonsterDetailsDto
    {
        public int Index { get; set; }
        public int? MonsterIndex { get; set; }
        public string? MonsterName { get; set; }
        public int? MapIndex { get; set; }
        public string? MapName { get; set; }
        public int Chance { get; set; }
        public int Amount { get; set; }
        public int DropSet { get; set; }
    }

    public class QuestRewardDto
    {
        public int Index { get; set; }
        public ItemInfoDto? Item { get; set; }
        public int Amount { get; set; }
        public string Class { get; set; } = "";
        public bool Choice { get; set; }
        public bool Bound { get; set; }
        public int Duration { get; set; }
    }

    public class AddQuestRequest
    {
        public string QuestName { get; set; } = "";
        public int StartNPC { get; set; }
        public int FinishNPC { get; set; }
    }

    public class UpdateQuestRequest
    {
        public string? QuestName { get; set; }
        public string? AcceptText { get; set; }
        public string? ProgressText { get; set; }
        public string? CompletedText { get; set; }
        public string? ArchiveText { get; set; }
        public int? StartNPC { get; set; }
        public int? FinishNPC { get; set; }
        public List<QuestRequirementDto>? Requirements { get; set; }
        public List<QuestTaskDto>? Tasks { get; set; }
        public List<QuestRewardDto>? Rewards { get; set; }
    }

    #endregion

    #region Store

    public class StoreInfoDto
    {
        public int Index { get; set; }
        public int ItemIndex { get; set; }
        public string ItemName { get; set; } = "";
        public int Price { get; set; }
        public int HuntGoldPrice { get; set; }
        public string Filter { get; set; } = "";
        public bool Available { get; set; }
        public int Duration { get; set; }
    }

    public class AddStoreItemRequest
    {
        public int ItemIndex { get; set; }
        public int Price { get; set; }
        public int HuntGoldPrice { get; set; }
        public string Filter { get; set; } = "";
        public bool Available { get; set; } = true;
        public int Duration { get; set; } = 0;
    }

    public class UpdateStoreItemRequest
    {
        public int? Price { get; set; }
        public int? HuntGoldPrice { get; set; }
        public string? Filter { get; set; }
        public bool? Available { get; set; }
        public int? Duration { get; set; }
    }

    #endregion
}

