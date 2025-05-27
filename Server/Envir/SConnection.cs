using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using Library;
using Library.Network;
using Library.SystemModels;
using Server.DBModels;
using Server.Envir.Translations;
using Zircon.Server.Models;
using G = Library.Network.GeneralPackets;
using C = Library.Network.ClientPackets;
using S = Library.Network.ServerPackets;
using System.Security.Principal;
using Library.Network.ClientPackets;
using System.Numerics;
using System.Text;

namespace Server.Envir
{

    public sealed class SConnection : BaseConnection
    {
        private static int SessionCount;
        private bool Upgrading = false;

        protected override TimeSpan TimeOutDelay { get { return Config.TimeOut; } }

        private DateTime PingTime;
        private bool PingSent;
        public int Ping { get; private set; }

        public GameStage Stage { get; set; }
        public AccountInfo Account { get; set; }
        public PlayerObject Player { get; set; }
        public string IPAddress { get; private set; }
        public int SessionID { get; private set; }

        public SConnection Observed;
        public List<SConnection> Observers = new List<SConnection>();

        public List<AuctionInfo> MPSearchResults = new List<AuctionInfo>();
        public HashSet<AuctionInfo> VisibleResults = new HashSet<AuctionInfo>();

        public StringMessages Language { get; set; }

        public SConnection(TcpClient client, string realIp) : base(client)
        {
            IPAddress = realIp;
            SessionID = ++SessionCount;


            Language = (StringMessages) ConfigReader.ConfigObjects[typeof(EnglishMessages)]; //Todo Language Selections

            OnException += (o, e) =>
            {
                if (e is SocketException sex)
                {
                    SEnvir.Log($"网络崩溃: 账号={(Account != null ? Account.EMailAddress : "empty")}, 角色={(Player != null ? Player.Name : "empty")}.");
                    File.AppendAllText("./datas/Errors.txt", sex.StackTrace + Environment.NewLine);

                    if (sex.InnerException != null && sex.InnerException.StackTrace != null)
                        File.AppendAllText("./datas/Errors.txt", sex.InnerException.StackTrace + Environment.NewLine);
                    return;
                }

                SEnvir.Log(string.Format("崩溃: 账号={0}, 角色={1}.", (Account != null ? Account.EMailAddress : "empty"), Player != null ? Player.Name : "empty"));
                SEnvir.Log(e.ToString());

                if (e.StackTrace != null)
                    SEnvir.Log(e.StackTrace.ToString());

                File.AppendAllText("./datas/Errors.txt", e.StackTrace + Environment.NewLine);
            };

            SEnvir.Log(string.Format("[连接] IP:{0}", IPAddress));

            UpdateTimeOut();
            BeginReceive();

            Enqueue(new G.Connected());
        }

        public override void Disconnect()
        {
            if (!Connected) return;

            base.Disconnect();

            CleanUp();

            if (!SEnvir.Connections.Contains(this))
                throw new InvalidOperationException("Connection was not found in list");

            SEnvir.Connections.Remove(this);
            SEnvir.IPCount[IPAddress]--;
            SEnvir.DBytesSent += TotalBytesSent;
            SEnvir.DBytesReceived += TotalBytesReceived;
        }

        public override void SendDisconnect(Packet p)
        {
            base.SendDisconnect(p);

            CleanUp();
        }
        public override void TryDisconnect()
        {
            if (Stage == GameStage.Game)
            {
                if (SEnvir.Now >= Player.CombatTime.AddSeconds(10))
                {
                    Disconnect();
                    return;
                }

                if (!Disconnecting)
                {
                    Disconnecting = true;
                    TimeOutTime = Time.Now.AddSeconds(2);
                }

                if (SEnvir.Now <= TimeOutTime) return;
            }

            Disconnect();
        }
        public override void TrySendDisconnect(Packet p)
        {
            if (Stage == GameStage.Game)
            {
                if (SEnvir.Now >= Player.CombatTime.AddSeconds(10))
                {
                    Disconnect();
                    return;
                }

                if (!Disconnecting)
                {
                    base.SendDisconnect(p);

                    TimeOutTime = Time.Now.AddSeconds(2);
                }

                if (SEnvir.Now <= TimeOutTime) return;

            }

            SendDisconnect(p);
        }

        public void EndObservation()
        {
            Observed.Observers.Remove(this);
            Observed = null;

            if (Account != null)
            {
                Stage = GameStage.Select;
                Enqueue(new S.GameLogout { Characters = Account.GetSelectInfo() });
            }
            else
            {
                Stage = GameStage.Login;
                Enqueue(new S.SelectLogout());
            }
        }
        public void CleanUp()
        {
            Stage = GameStage.Disconnected;

            if (Account != null && Account.Connection == this)
            {
                Account.TempAdmin = false;
                Account.Connection = null;
            }

            Account = null;
            if (Player != null)
                Player.StopGame();
            Player = null;

            if (Observed != null && Observed.Observers != null)
                Observed.Observers.Remove(this);
            Observed = null;

            //   ItemList.Clear();
            //    MagicList.Clear();
        }
        public override void Process()
        {
            if (SEnvir.Now >= PingTime && !PingSent && Stage != GameStage.None)
            {
                PingTime = SEnvir.Now;
                PingSent = true;
                Enqueue(new G.Ping { ObserverPacket = false });
            }

            if (!Disconnecting && ReceiveList.Count > Config.MaxPacket)
            {
                Dictionary<Type, uint> dict = new Dictionary<Type, uint>();
                foreach(var pack in ReceiveList)
                {
                    if (pack == null) continue;

                    if (!dict.TryGetValue(pack.PacketType, out uint counter))
                    {
                        dict.Add(pack.PacketType, 1);
                        continue;
                    }

                    counter++;
                    dict[pack.PacketType] = counter;
                }

                StringBuilder sb = new StringBuilder();
                foreach(var pair in dict)
                {
                    if (sb.Length<= 0) sb.Append($"[{pair.Key.FullName}]x{pair.Value}");
                    else sb.Append($"、[{pair.Key.FullName}]x{pair.Value}");
                }

                dict.Clear();


                var checknum = Account?.LastSum ?? "";
                SEnvir.Log($"网络包太多，断开用户连接：账号={Account?.EMailAddress ?? "空"} 角色={Player?.Character?.CharacterName ?? "空"} IP={IPAddress} 验证码={Account?.LastSum ?? ""}");
                SEnvir.Log($"积压的网络包共有 {ReceiveList.Count} 个：{sb.ToString()}");
                sb.Clear();

                TryDisconnect();
                SEnvir.IPBlocks[IPAddress] = SEnvir.Now.Add(Config.PacketBanTime);


                for (int i = SEnvir.Connections.Count - 1; i >= 0; i--)
                {
                    var con = SEnvir.Connections[i];
                    if (con == null || con.Account == null)
                        continue;

                    if (!string.IsNullOrEmpty(checknum) && con.Account.LastSum == checknum)
                    {
                        SEnvir.Log($"断开相同验证码的连接 账号={con.Account} 角色={con.Player?.Character?.CharacterName ?? ""}");
                        SEnvir.Connections[i].TryDisconnect();
                    }
                    else if (con.IPAddress == IPAddress)
                    {
                        SEnvir.Log($"断开相同IP的连接 账号={con.Account} 角色={con.Player?.Character?.CharacterName ?? ""}");
                        SEnvir.Connections[i].TryDisconnect();
                    }
                }
            }

            base.Process();
        }

        public override void Enqueue(Packet p)
        {
            base.Enqueue(p);

            if (p == null || !p.ObserverPacket) return;

            foreach (SConnection observer in Observers)
                observer.Enqueue(p);
        }

        public void ReceiveChat(string text, MessageType type, uint objectID = 0)
        {

            switch (Stage)
            {
                case GameStage.Game:
                case GameStage.Observer:
                    Enqueue(new S.Chat
                    {
                        Text = text,
                        Type = type,
                        ObjectID = objectID, // && type != guild

                        ObserverPacket = false,
                    });
                    break;
                default:
                    return;
            }
        }

        public void Process(C.SelectLanguage p)
        {
            switch (p.Language.ToUpper())
            {
                case "ENGLISH":
                    Language = (StringMessages)ConfigReader.ConfigObjects[typeof(EnglishMessages)]; //Todo Language Selections
                    break;
                case "CHINESE":
                    Language = (StringMessages)ConfigReader.ConfigObjects[typeof(ChineseMessages)]; //Todo Language Selections
                    break;
            }

            var packet = new S.CheckClientHash()
            {
                ClientFileHash = new List<ClientUpgradeItem>(),
            };

            foreach (var pair in SEnvir.ClientFileHash)
            {
                packet.ClientFileHash.Add(pair.Value);
            }

            Enqueue(packet);
        }
        public void Process(G.Disconnect p)
        {
            Disconnecting = true;
        }
        public void Process(G.Connected p)
        {
            //if (Config.CheckVersion)
            //{
            //    Enqueue(new G.CheckVersion());
            //    return;
            //}

            Stage = GameStage.Login;
            Enqueue(new G.GoodVersion());
        }
        public void Process(G.Version p)
        {
            if (Stage != GameStage.None) return;

            //if (!Functions.IsMatch(Config.ClientHash, p.ClientHash))
            //{
            //    SendDisconnect(new G.Disconnect { Reason = DisconnectReason.WrongVersion });
            //    return;
            //}

            Stage = GameStage.Login;
            Enqueue(new G.GoodVersion());
        }
        public void Process(G.Ping p)
        {
            if (Stage == GameStage.None) return;

            int ping = (int) (SEnvir.Now - PingTime).TotalMilliseconds/2;
            PingSent = false;
            PingTime = SEnvir.Now + Config.PingDelay;

            Ping = ping;
            Enqueue(new G.PingResponse { Ping = Ping, ObserverPacket = false });
        }
        public void Process(C.UpgradeClient p)
        {
            if (Upgrading) return;

            if (SEnvir.ClientFileHash.Count <= 0)
            {
                Enqueue(new S.UpgradeClient()
                {
                    FileKey = p.FileKey,
                    TotalSize = 0,
                    Datas = [0],
                });
                return;
            }

            if (string.IsNullOrEmpty(p.FileKey))
            {
                SEnvir.Log($"客户端请求更新的文件名为空 FileKey={p.FileKey}");
                Enqueue(new S.UpgradeClient()
                {
                    FileKey = p.FileKey,
                    TotalSize = 0,
                    Datas = [0],
                });
                return;
            }

            if (!SEnvir.ClientFileHash.TryGetValue(p.FileKey, out var _))
            {
                SEnvir.Log($"更新清单中没有找到请求的文件：{p.FileKey}");

                Enqueue(new S.UpgradeClient()
                {
                    FileKey = p.FileKey,
                    TotalSize = 0,
                    Datas = [0],
                });
                return;
            }

            string filename = Path.Combine(Config.ClientPath, p.FileKey);
            if (!File.Exists(filename))
            {
                SEnvir.Log($"请求更新的文件找不到对应的实体文件：FileKey={p.FileKey} {filename}");

                Enqueue(new S.UpgradeClient()
                {
                    FileKey = p.FileKey,
                    TotalSize = 0,
                    Datas = [0],
                });
                return;
            }

            SEnvir.Log($"[{IPAddress}] 请求更新 {p.FileKey} ...");

            Upgrading = true;
            using (FileStream stream = File.OpenRead(filename))
            {
                long total = stream.Length;
                int index = 0;
                int curSize = 0;
                byte[] datas;

                while(index < total)
                {
                    curSize = Config.UpgradeChunkSize > 1024 ? Config.UpgradeChunkSize : 512 * 1024;
                    if ((curSize + index) > total)
                        curSize = (int)total - index;

                    datas = new byte[curSize];
                    stream.Read(datas, 0, curSize);

                    Enqueue(new S.UpgradeClient()
                    {
                        FileKey = p.FileKey,
                        TotalSize = (int)total,
                        StartIndex = index,
                        Datas = datas,
                    });

                    index += curSize;
                    Thread.Sleep(1);
                }
            }

            Upgrading = false;
            SEnvir.Log($"[{IPAddress}] 更新完成 {p.FileKey}");
        }

        public void Process(C.NewAccount p)
        {
            if (Stage != GameStage.Login) return;
            SEnvir.NewAccount(p, this);
        }
        public void Process(C.ChangePassword p)
        {
            if (Stage != GameStage.Login) return;
            SEnvir.ChangePassword(p, this);
        }
        public void Process(C.RequestPasswordReset p)
        {
            if (Stage != GameStage.Login) return;
            Enqueue(new S.NewAccount { Result = NewAccountResult.Disabled });
            //SEnvir.RequestPasswordReset(p, this);
        }
        public void Process(C.ResetPassword p)
        {
            if (Stage != GameStage.Login) return;
            Enqueue(new S.NewAccount { Result = NewAccountResult.Disabled });
            //SEnvir.ResetPassword(p, this);
        }
        public void Process(C.Activation p)
        {
            if (Stage != GameStage.Login) return;
            SEnvir.Activation(p, this);
        }
        public void Process(C.RequestActivationKey p)
        {
            if (Stage != GameStage.Login) return;

            SEnvir.RequestActivationKey(p, this);
        }
        public void Process(C.Login p)
        {
            if (Stage != GameStage.Login) return;

            SEnvir.Login(p, this);
        }
        public void Process(C.LoginSimple p)
        {
            if (Stage != GameStage.Login) return;
            SEnvir.LoginSimple(p, this);
        }
        public void Process(C.AccountExpand p)
        {
            if (Stage != GameStage.Game) return;

            Enqueue(new S.AccountExpand()
            {
                BlockList = Account.BlockingList.Select(x => x.ToClientInfo()).ToList(),
                Items = Account.Items.Select(x => x.ToClientInfo()).ToList(),
            });
        }
        public void Process(C.Logout p)
        {

            switch (Stage)
            {
                case GameStage.Select:
                    Stage = GameStage.Login;
                    Account.Connection = null;
                    Account = null;

                    Enqueue(new S.SelectLogout());
                    break;
                case GameStage.Game:

                    if (SEnvir.Now < Player.CombatTime.AddSeconds(10)) return;

                    Player.StopGame();

                    Stage = GameStage.Select;

                    Enqueue(new S.GameLogout { Characters = Account.GetSelectInfo() });
                    break;
                case GameStage.Observer:
                    EndObservation();
                    break;
            }
            ;

        }

        public void Process(C.NewCharacter p)
        {
            if (Stage != GameStage.Select) return;

            SEnvir.NewCharacter(p, this);
        }
        public void Process(C.DeleteCharacter p)
        {
            if (Stage != GameStage.Select) return;

            SEnvir.DeleteCharacter(p, this);
        }
        public void Process(C.StartGame p)
        {
            if (Stage != GameStage.Select) return;


            SEnvir.StartGame(p, this);
        }
        public void Process(C.TownRevive p)
        {
            if (Stage != GameStage.Game) return;

            Player.TownRevive();
        }
        public void Process(C.Turn p)
        {
            if (Stage != GameStage.Game) return;

            if (p.Direction < MirDirection.Up || p.Direction > MirDirection.UpLeft) return;

            Player.Turn(p.Direction);
        }
        public void Process(C.Harvest p)
        {
            if (Stage != GameStage.Game) return;

            if (p.Direction < MirDirection.Up || p.Direction > MirDirection.UpLeft) return;

            try { Player.Harvest(p.Direction); }
            catch(Exception ex) 
            { 
                SEnvir.Log($"[{Player.Name}] 发生异常：{ex.Message}"); 

                if (ex.StackTrace != null)
                    SEnvir.Log(ex.StackTrace);

                if (ex.InnerException != null)
                    SEnvir.Log(ex.InnerException);
            }
        }
        public void Process(C.Move p)
        {
            if (Stage != GameStage.Game) return;

            if (p.Direction < MirDirection.Up || p.Direction > MirDirection.UpLeft) return;

            /*  if (p.Distance > 1 && (Player.BagWeight > Player.Stats[Stat.BagWeight] || Player.WearWeight > Player.Stats[Stat.WearWeight]))
              {
                  Enqueue(new S.UserLocation { Direction = Player.Direction, Location = Player.CurrentLocation });
                  return;
              }*/

            Player.Move(p.Direction, p.Distance);
        }
        public void Process(C.Mount p)
        {
            if (Stage != GameStage.Game) return;

            Player.Mount();
        }
        public void Process(C.Attack p)
        {
            if (Stage != GameStage.Game) return;

            if (p.Direction < MirDirection.Up || p.Direction > MirDirection.UpLeft) return;

            Player.Attack(p.Direction, p.AttackMagic);
        }
        public void Process(C.Magic p)
        {
            if (Stage != GameStage.Game) return;

            if (p.Direction < MirDirection.Up || p.Direction > MirDirection.UpLeft) return;

            Player.Magic(p);
        }
        public void Process(C.MagicToggle p)
        {
            if (Stage != GameStage.Game) return;

            Player.MagicToggle(p);
        }
        public void Process(C.Mining p)
        {
            if (Stage != GameStage.Game) return;

            if (p.Direction < MirDirection.Up || p.Direction > MirDirection.UpLeft) return;

            Player.Mining(p.Direction);
        }

        public void Process(C.ItemMove p)
        {
            if (Stage != GameStage.Game) return;

            Player.ItemMove(p);
        }
        public void Process(C.ItemDrop p)
        {
            if (Stage != GameStage.Game) return;

            Player.ItemDrop(p);
        }
        public void Process(C.PickUp p)
        {
            if (Stage != GameStage.Game) return;

            int type = (int)p.PickType;

            if (Enum.IsDefined(typeof(PickType), type))
                Player.PickUp((PickType)type);
        }
        public void Process(C.GoldDrop p)
        {
            if (Stage != GameStage.Game) return;

            Player.GoldDrop(p);
        }
        public void Process(C.ItemUse p)
        {
            if (Stage != GameStage.Game) return;

            Player.ItemUse(p.Link);
        }

        public void Process(C.CheckClientDb p)
        {
            if (string.IsNullOrEmpty(p.Hash)) return;

            if (p.Hash == SEnvir.DbSystemFileHash)
                Enqueue(new S.CheckClientDb()
                {
                    CurrentIndex = 0,
                    Datas = [0],
                    IsUpgrading = false,
                    TotalCount = 0,
                });
            else
            {
                Enqueue(new S.CheckClientDb()
                {
                    CurrentIndex = 0,
                    Datas = SEnvir.DbSystemFile,
                    IsUpgrading = true,
                    TotalCount = 1,
                });
            }
        }

        public void Process(C.BeltLinkChanged p)
        {
            if (Stage != GameStage.Game) return;

            Player.BeltLinkChanged(p);
        }
        public void Process(C.AutoPotionLinkChanged p)
        {
            if (Stage != GameStage.Game) return;

            Player.AutoPotionLinkChanged(p);
        }

        public void Process(C.Chat p)
        {
            if (p.Text.Length > Globals.MaxChatLength) return;

            try
            {
                if (Stage == GameStage.Game)
                    Player.Chat(p.Text);

                if (Stage == GameStage.Observer)
                    Observed.Player.ObserverChat(this, p.Text);
            }
            catch(Exception e)
            {
                SEnvir.Log($"发送聊天消息：{p.Text}");
                OnException(this, e);
            }

        }
        public void Process(C.NPCCall p)
        {
            if (Stage != GameStage.Game) return;

            Player.NPCCall(p.ObjectID);
        }
        public void Process(C.NPCButton p)
        {
            if (Stage != GameStage.Game) return;

            Player.NPCButton(p.ButtonID);
        }
        public void Process(C.NPCBuy p)
        {
            if (Stage != GameStage.Game) return;

            Player.NPCBuy(p);
        }
        public void Process(C.NPCSell p)
        {
            if (Stage != GameStage.Game) return;

            Player.NPCSell(p.Links);
        }
        public void Process(C.NPCRepair p)
        {
            if (Stage != GameStage.Game) return;

            Player.NPCRepair(p);
        }
        public void Process(C.NPCRefinementStone p)
        {
            if (Stage != GameStage.Game) return;

            Player.NPCRefinementStone(p);
        }
        public void Process(C.NPCRefine p)
        {
            if (Stage != GameStage.Game) return;

            Player.NPCRefine(p);
        }
        public void Process(C.NPCRefineRetrieve p)
        {
            if (Stage != GameStage.Game) return;

            Player.NPCRefineRetrieve(p.Index);
        }
        public void Process(C.NPCMasterRefine p)
        {
            if (Stage != GameStage.Game) return;

            Player.NPCMasterRefine(p);
        }
        public void Process(C.NPCMasterRefineEvaluate p)
        {
            if (Stage != GameStage.Game) return;

            Player.NPCMasterRefineEvaluate(p);
        }
        public void Process(C.NPCClose p)
        {
            if (Stage != GameStage.Game) return;

            Player.NPC = null;
            Player.NPCPage = null;

            foreach (SConnection con in Observers)
            {
                con.Enqueue(new S.NPCClose());
            }
        }
        public void Process(C.NPCFragment p)
        {
            if (Stage != GameStage.Game) return;

            Player.NPCFragment(p.Links);
        }
        public void Process(C.NPCAccessoryLevelUp p)
        {
            if (Stage != GameStage.Game) return;

            Player.NPCAccessoryLevelUp(p);
        }
        public void Process(C.NPCAccessoryUpgrade p)
        {
            if (Stage != GameStage.Game) return;

            Player.NPCAccessoryUpgrade(p);
        }


        public void Process(C.MagicKey p)
        {
            if (Stage != GameStage.Game) return;


            foreach (KeyValuePair<MagicType, UserMagic> pair in Player.Magics)
            {
                if (pair.Value.Set1Key == p.Set1Key)
                    pair.Value.Set1Key = SpellKey.None;

                if (pair.Value.Set2Key == p.Set2Key)
                    pair.Value.Set2Key = SpellKey.None;

                if (pair.Value.Set3Key == p.Set3Key)
                    pair.Value.Set3Key = SpellKey.None;

                if (pair.Value.Set4Key == p.Set4Key)
                    pair.Value.Set4Key = SpellKey.None;
            }

            UserMagic magic;

            if (!Player.Magics.TryGetValue(p.Magic, out magic)) return;

            magic.Set1Key = p.Set1Key;
            magic.Set2Key = p.Set2Key;
            magic.Set3Key = p.Set3Key;
            magic.Set4Key = p.Set4Key;
        }

        public void Process(C.GroupSwitch p)
        {
            if (Stage != GameStage.Game) return;

            Player.GroupSwitch(p.Allow);
        }
        public void Process(C.GroupInvite p)
        {
            if (Stage != GameStage.Game) return;

            Player.GroupInvite(p.Name);
        }
        public void Process(C.GroupRemove p)
        {
            if (Stage != GameStage.Game) return;

            Player.GroupRemove(p.Name);
        }
        public void Process(C.GroupResponse p)
        {
            if (Stage != GameStage.Game) return;

            if (p.Accept)
                Player.GroupJoin();

            Player.GroupInvitation = null;
        }

        public void Process(C.Inspect p)
        {
            if (Stage == GameStage.Game)
                Player.Inspect(p.Index, this);

            if (Stage == GameStage.Observer)
                Observed.Player.Inspect(p.Index, this);
        }
        public void Process(C.RankRequest p)
        {
            if (Stage != GameStage.Game && Stage != GameStage.Observer && Stage != GameStage.Login) return;

            Enqueue(SEnvir.GetRanks(p, Account?.Identify != AccountIdentity.Normal));
        }

        public void Process(C.ObserverRequest p)
        {
            if (Account == null) return;
            if (!Config.AllowObservation && Account.Identify == AccountIdentity.Normal) return;

            PlayerObject player = SEnvir.GetPlayerByCharacter(p.Name);

            if (player == null || player == Player) return;

            if (!player.Character.Observable && Account.Identify == AccountIdentity.Normal) return;

            if (Stage == GameStage.Game)
                Player.StopGame();

            if (Stage == GameStage.Observer)
            {
                Observed.Observers.Remove(this);
                Observed = null;
            }

            player.SetUpObserver(this);
        }
        public void Process(C.ObservableSwitch p)
        {
            if (Stage != GameStage.Game) return;

            Player.ObservableSwitch(p.Allow);
        }

        public void Process(C.Hermit p)
        {
            if (Stage != GameStage.Game) return;

            Player.AssignHermit(p.Stat);
        }

        public void Process(C.MarketPlaceHistory p)
        {
            if (Stage != GameStage.Game && Stage != GameStage.Observer) return;


            S.MarketPlaceHistory result = new S.MarketPlaceHistory { Index = p.Index, Display = p.Display, ObserverPacket = false };
            Enqueue(result);

            AuctionHistoryInfo info = SEnvir.AuctionHistoryInfoList.Binding.FirstOrDefault(x => x.Info == p.Index && x.PartIndex == p.PartIndex);

            if (info == null) return;

            result.SaleCount = info.SaleCount;
            result.LastPrice = info.LastPrice;

            long average = 0;
            int count = 0;

            foreach (int value in info.Average)
            {
                if (value == 0) break;

                average += value;
                count++;
            }

            if (count == 0) return;
            result.AveragePrice = average/count;
        }
        public void Process(C.MarketPlaceConsign p)
        {
            if (Stage != GameStage.Game) return;

            Player.MarketPlaceConsign(p);
        }

        public void Process(C.MarketPlaceSearch p)
        {
            if (Stage != GameStage.Game && Stage != GameStage.Observer) return;

            MPSearchResults.Clear();
            VisibleResults.Clear();

            HashSet<int> matches = new HashSet<int>();

            foreach (ItemInfo info in SEnvir.ItemInfoList.Binding)
            {
                try
                {
                if (!string.IsNullOrEmpty(p.Name) && info.ItemName.IndexOf(p.Name, StringComparison.OrdinalIgnoreCase) < 0) continue;
                
                matches.Add(info.Index);

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            foreach (AuctionInfo info in SEnvir.AuctionInfoList.Binding)
            {
                if (info.Item == null) continue;

                if (p.ItemTypeFilter && info.Item.Info.ItemType != p.ItemType) continue;

                switch (info.Item.Info.Effect)
                {
                    case ItemEffect.ItemPart:
                        if (!matches.Contains(info.Item.Stats[Stat.ItemIndex])) continue;
                        break;
                    default:
                        if (!matches.Contains(info.Item.Info.Index)) continue;
                        break;
                }

                MPSearchResults.Add(info);
            }

            switch (p.Sort)
            {
                case MarketPlaceSort.Newest:
                    MPSearchResults.Sort((x1, x2) => x2.Index.CompareTo(x1.Index));
                    break;
                case MarketPlaceSort.Oldest:
                    MPSearchResults.Sort((x1, x2) => x1.Index.CompareTo(x2.Index));
                    break;
                case MarketPlaceSort.HighestPrice:
                    MPSearchResults.Sort((x1, x2) => x2.Price.CompareTo(x1.Price));
                    break;
                case MarketPlaceSort.LowestPrice:
                    MPSearchResults.Sort((x1, x2) => x1.Price.CompareTo(x2.Price));
                    break;
            }

            //Send Rows 1 ~ 9
            List<ClientMarketPlaceInfo> results = new List<ClientMarketPlaceInfo>();

            foreach (AuctionInfo info in MPSearchResults)
            {
                if (results.Count >= 9) break;

                results.Add(info.ToClientInfo(Account));
                VisibleResults.Add(info);
            }


            Enqueue(new S.MarketPlaceSearch { Count = MPSearchResults.Count, Results = results, ObserverPacket = false });
        }
        public void Process(C.MarketPlaceSearchIndex p)
        {
            if (Stage != GameStage.Game && Stage != GameStage.Observer) return;

            if (p.Index < 0 || p.Index >= MPSearchResults.Count) return;


            AuctionInfo info = MPSearchResults[p.Index];

            if (info == null || VisibleResults.Contains(info)) return;

            VisibleResults.Add(info);

            Enqueue(new S.MarketPlaceSearchIndex { Index = p.Index, Result = info.ToClientInfo(Account), ObserverPacket = false });
        }
        public void Process(C.MarketPlaceCancelConsign p)
        {
            if (Stage != GameStage.Game) return;

            Player.MarketPlaceCancelConsign(p);
        }
        public void Process(C.MarketPlaceBuy p)
        {
            if (Stage != GameStage.Game) return;

            Player.MarketPlaceBuy(p);
        }
        public void Process(C.MarketPlaceStoreBuy p)
        {
            if (Stage != GameStage.Game) return;

            Player.MarketPlaceStoreBuy(p);
        }

        public void Process(C.MailOpened p)
        {
            if (Stage != GameStage.Game) return;

            MailInfo mail = Account.Mail.FirstOrDefault(x => x.Index == p.Index);

            if (mail == null) return;

            mail.Opened = true;
        }
        public void Process(C.MailGetItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.MailGetItem(p);
        }
        public void Process(C.MailDelete p)
        {
            if (Stage != GameStage.Game) return;

            Player.MailDelete(p.Index);
        }
        public void Process(C.MailSend p)
        {
            if (Stage != GameStage.Game) return;

            Player.MailSend(p);
        }

        public void Process(C.ChangeAttackMode p)
        {
            if (Stage != GameStage.Game) return;

            switch (p.Mode)
            {
                case AttackMode.Peace:
                case AttackMode.Group:
                case AttackMode.Guild:
                case AttackMode.WarRedBrown:
                case AttackMode.All:
                    Player.AttackMode = p.Mode;
                    Enqueue(new S.ChangeAttackMode { Mode = p.Mode });
                    break;
            }
        }
        public void Process(C.ChangePetMode p)
        {
            if (Stage != GameStage.Game) return;

            switch (p.Mode)
            {
                case PetMode.Both:
                case PetMode.Move:
                case PetMode.Attack:
                case PetMode.PvP:
                case PetMode.None:
                    Player.PetMode = p.Mode;
                    Enqueue(new S.ChangePetMode { Mode = p.Mode });
                    break;
            }
        }
        
        public void Process(C.ItemSplit p)
        {
            if (Stage != GameStage.Game) return;

            Player.ItemSplit(p);
        }
        public void Process(C.ItemLock p)
        {
            if (Stage != GameStage.Game) return;

            Player.ItemLock(p);
        }


        public void Process(C.TradeRequest p)
        {
            if (Stage != GameStage.Game) return;

            Player.TradeRequest();
        }
        public void Process(C.TradeRequestResponse p)
        {
            if (Stage != GameStage.Game) return;

            if (p.Accept)
                Player.TradeAccept();

            Player.TradePartnerRequest = null;
        }
        public void Process(C.TradeClose p)
        {
            if (Stage != GameStage.Game) return;

            Player.TradeClose();
        }
        public void Process(C.TradeAddItem p)
        {
            if (Stage != GameStage.Game) return;

            Player.TradeAddItem(p.Cell);
        }
        public void Process(C.TradeAddGold p)
        {
            if (Stage != GameStage.Game) return;

            Player.TradeAddGold(p.Gold);
        }
        public void Process(C.TradeConfirm p)
        {
            if (Stage != GameStage.Game) return;

            Player.TradeConfirm();
        }

        public void Process(C.GuildCreate p)
        {
            if (Stage != GameStage.Game) return;

            Player.GuildCreate(p);
        }
        public void Process(C.GuildEditNotice p)
        {
            if (Stage != GameStage.Game) return;

            Player.GuildEditNotice(p);
        }
        public void Process(C.GuildEditMember p)
        {
            if (Stage != GameStage.Game) return;

            Player.GuildEditMember(p);
        }
        public void Process(C.GuildTax p)
        {
            if (Stage != GameStage.Game) return;

            Player.GuildTax(p);
        }
        public void Process(C.GuildIncreaseMember p)
        {
            if (Stage != GameStage.Game) return;

            Player.GuildIncreaseMember(p);
        }
        public void Process(C.GuildIncreaseStorage p)
        {
            if (Stage != GameStage.Game) return;

            Player.GuildIncreaseStorage(p);
        }
        public void Process(C.GuildInviteMember p)
        {
            if (Stage != GameStage.Game) return;

            Player.GuildInviteMember(p);
        }
        public void Process(C.GuildKickMember p)
        {
            if (Stage != GameStage.Game) return;

            Player.GuildKickMember(p);
        }
        public void Process(C.GuildResponse p)
        {
            if (Stage != GameStage.Game) return;

            if (p.Accept)
                Player.GuildJoin();

            Player.GuildInvitation = null;
        }
        public void Process(C.GuildWar p)
        {
            if (Stage != GameStage.Game) return;

            Player.GuildWar(p.GuildName);
        }
        public void Process(C.GuildRequestConquest p)
        {
            if (Stage != GameStage.Game) return;

            Player.GuildConquest(p.Index);
        }

        public void Process(C.QuestAccept p)
        {
            if (Stage != GameStage.Game) return;

            Player.QuestAccept(p.Index);
        }
        public void Process(C.QuestComplete p)
        {
            if (Stage != GameStage.Game) return;

            Player.QuestComplete(p);
        }
        public void Process(C.QuestTrack p)
        {
            if (Stage != GameStage.Game) return;

            Player.QuestTrack(p);
        }

        public void Process(C.CompanionUnlock p)
        {
            if (Stage != GameStage.Game) return;

            Player.CompanionUnlock(p.Index);
        }
        public void Process(C.CompanionAdopt p)
        {
            if (Stage != GameStage.Game) return;

            Player.CompanionAdopt(p);
        }
        public void Process(C.CompanionRetrieve p)
        {
            if (Stage != GameStage.Game) return;

            Player.CompanionRetrieve(p.Index);
        }

        public void Process(C.CompanionStore p)
        {
            if (Stage != GameStage.Game) return;

            Player.CompanionStore(p.Index);
        }

        public void Process(C.MarriageResponse p)
        {
            if (Stage != GameStage.Game) return;

            if (p.Accept)
                Player.MarriageJoin();

            Player.MarriageInvitation = null;
        }
        public void Process(C.MarriageMakeRing p)
        {
            if (Stage != GameStage.Game) return;

            Player.MarriageMakeRing(p.Slot);

        }
        public void Process(C.MarriageTeleport p)
        {
            if (Stage != GameStage.Game) return;

            Player.MarriageTeleport();
        }

        public void Process(C.BlockAdd p)
        {
            if (Stage != GameStage.Game && Stage != GameStage.Observer) return;

            if (Account == null) return;

            CharacterInfo info = SEnvir.GetCharacter(p.Name);

            if (info == null)
            {
                ReceiveChat(string.Format(Language.CannotFindPlayer, p.Name), MessageType.System);

                return;
            }

            foreach (BlockInfo blockInfo in Account.BlockingList)
            {
                if (blockInfo.BlockedAccount == info.Account)
                {
                    ReceiveChat(string.Format(Language.AlreadyBlocked, p.Name), MessageType.System);
                    return;
                }
            }

            BlockInfo block = SEnvir.BlockInfoList.CreateNewObject();

            block.Account = Account;
            block.BlockedAccount = info.Account;
            block.BlockedName = info.CharacterName;

            Enqueue(new S.BlockAdd { Info = block.ToClientInfo(), ObserverPacket = false });
        }
        public void Process(C.BlockRemove p)
        {
            if (Stage != GameStage.Game && Stage != GameStage.Observer) return;

            BlockInfo block = Account.BlockingList.FirstOrDefault(x => x.Index == p.Index);

            if (block == null) return;

            block.Delete();

            Enqueue(new S.BlockRemove { Index = p.Index, ObserverPacket = false });
        }

        public void Process(C.HelmetToggle p)
        {
            if (Stage != GameStage.Game) return;

            Player.HelmetToggle(p.HideHelmet);
        }

        public void Process(C.GenderChange p)
        {
            if (Stage != GameStage.Game) return;

            
            Player.GenderChange(p);
        }
        public void Process(C.HairChange p)
        {
            if (Stage != GameStage.Game) return;


            Player.HairChange(p);

        }
        public void Process(C.ArmourDye p)
        {
            if (Stage != GameStage.Game) return;


            Player.ArmourDye(p.ArmourColour);
        }
        public void Process(C.NameChange p)
        {
            if (Stage != GameStage.Game) return;

            
            Player.NameChange(p.Name);
        }

        public void Process(C.FortuneCheck p)
        {
            if (Stage != GameStage.Game) return;
            
            Player.FortuneCheck(p.ItemIndex);
        }
        public void Process(C.TeleportRing p)
        {
            if (Stage != GameStage.Game) return;

            Player.TeleportRing(p.Location, p.Index);

        }
        public void Process(C.JoinStarterGuild p)
        {
            if (Stage != GameStage.Game) return;

            Player.JoinStarterGuild();

        }
        public void Process(C.NPCAccessoryReset p)
        {
            if (Stage != GameStage.Game) return;

            Player.NPCAccessoryReset(p);
        }

        public void Process(C.NPCWeaponCraft p)
        {
            if (Stage != GameStage.Game) return;

            Player.NPCWeaponCraft(p);
        }

        public void Process(C.AutoFightConfChanged p)
        {
            if (Stage != GameStage.Game)
                return;
            Player.AutoFightConfChanged(p);
        }

        public void Process(C.SortBagItem p)
        {
            if (Stage != GameStage.Game)
                return;

            Player.SortBagItem();
        }

        public void Process(C.SortStorageItem p)
        {
            if (Stage != GameStage.Game)
                return;

            Player.SortStorageItem();
        }

        public void Process(C.PickUpC p)
        {
            if (Stage != GameStage.Game) return;

            Player.PickUpC(p.Xpos, p.Ypos, p.ItemIdx);
        }

        public void Process(C.PickUpA p)
        {
            if (Stage != GameStage.Game) return;

            Player.PickUp(p.Xpos, p.Ypos, p.ItemIdx);
        }

        public void Process(C.PickUpS p)
        {
            if (Stage != GameStage.Game) return;
            if ((p.UserItems?.Count ?? 0) <= 0 && (p.CompanionItems?.Count ?? 0) <= 0) return;

            if (p.UserItems != null)
                foreach(var item in p.UserItems)
                    Player.PickUp(item.xPos, item.yPos, item.ItemIndex, false);

            if (p.CompanionItems != null)
                foreach (var item in p.CompanionItems)
                    Player.PickUpC(item.xPos, item.yPos, item.ItemIndex, false);
        }

        public void Process(C.PktFilterItem p)
        {
            if (Stage != GameStage.Game) return;

            foreach(var str in p.FilterStr)
                Player.FilterItem(str);
        }
    }


    public enum GameStage
    {
        None,
        Login,
        Select,
        Game,
        Observer,
        Disconnected,
    }
}
