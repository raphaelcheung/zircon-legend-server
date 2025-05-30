﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Library;
using Library.Network;
using Library.SystemModels;
using MirDB;
using Server.DBModels;
using Zircon.Server.Models;
using G = Library.Network.GeneralPackets;
using S = Library.Network.ServerPackets;
using C = Library.Network.ClientPackets;
using System.Security.Principal;
using System.Globalization;
using System.Reflection.PortableExecutable;
using Library.ContentSafe.SensitiveWord;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Server.Envir
{
    public static class SEnvir
    {
        public const string SuperAdmin = "raphael@gm.gm";

        public static TagRebirthInfo[] s_RebirthInfoList =
        [
            new ("", Color.White),
            new ("①", Color.DeepPink),
            new ("②", Color.DeepPink),
            new ("③", Color.DeepPink),
            new ("④", Color.DeepPink),
            new ("⑤", Color.DeepPink),
            new ("⑥", Color.DeepPink),
            new ("⑦", Color.DeepPink),

        ];

        private static DateTime AutoClearUserDatasTime = DateTime.MinValue;

        public static int BigBossCriticalChance { get; set; } = 5;

        public struct TagRebirthInfo(string mark, Color color)
        {
            public string Mark = mark;
            public Color NameColor = color;
        }
        private struct TagBlockInfo()
        {
            public DateTime BlockTime = DateTime.MinValue;
            public string Season = "";
        }

        private static readonly Dictionary<string, TagBlockInfo> dictDeviceBlock = new Dictionary<string, TagBlockInfo>();
        #region Synchronization

        private static readonly SynchronizationContext Context = SynchronizationContext.Current;
        public static void Send(SendOrPostCallback method)
        {
            Context.Send(method, null);
        }
        public static void Post(SendOrPostCallback method)
        {
            Context.Post(method, null);
        }

        public static bool SupportClientUpgrade { get => ClientFileHash.Count > 0; }
        public static Dictionary<string, ClientUpgradeItem> ClientFileHash { get; } = new Dictionary<string, ClientUpgradeItem>();
        #endregion


        #region Logging

        public static ConcurrentQueue<string> DisplayLogs { get; set; } = new ConcurrentQueue<string>();
        public static ConcurrentQueue<string> Logs { get; set; } = new ConcurrentQueue<string>();
        public static void Log(string log, bool hardLog = true)
        {
            DateTime now = Time.Now.ToLocalTime();

            log = $"[{now.ToString("yyyy-MM-dd HH:mm:ss")}]: {log}";

            if (DisplayLogs.Count < 300)
                DisplayLogs.Enqueue(log);

            if (hardLog && Logs.Count < 1000)
                Logs.Enqueue(log);
        }

        public static void Log(Exception ex)
        {
            Log(ex.Message);

            if (!string.IsNullOrEmpty(ex.StackTrace))
                Log(ex.StackTrace);

            if (ex.InnerException != null)
                Log(ex);
        }

        public static int GetWeaponLimitLevel(Rarity r)
        {
            return Config.武器最高精炼等级 - (Rarity.Elite - r) * Config.武器品质每低一档降低精炼上限;
        }
        
        public static ConcurrentQueue<string> DisplayChatLogs = new ConcurrentQueue<string>();
        public static ConcurrentQueue<string> ChatLogs = new ConcurrentQueue<string>();
        public static WordsLibrary? SensitiveWords = null;
        public static void LogChat(string log)
        {
            log = string.Format("[{0:F}]: {1}", Time.Now.ToLocalTime(), log);

            if (DisplayChatLogs.Count < 500)
                DisplayChatLogs.Enqueue(log);

            if (ChatLogs.Count < 1000)
                ChatLogs.Enqueue(log);
        }
        #endregion

        #region Network

        public static Dictionary<string, DateTime> IPBlocks { get; } = new Dictionary<string, DateTime>();
        public static Dictionary<string, int> IPCount = new Dictionary<string, int>();

        public static List<SConnection> Connections = new List<SConnection>();
        public static ConcurrentQueue<SConnection> NewConnections;

        private static TcpListener _listener, _userCountListener;

        private static void StartNetwork(bool log = true)
        {
            try
            {
                NewConnections = new ConcurrentQueue<SConnection>();

                _listener = new TcpListener(IPAddress.Parse(Config.IPAddress), Config.Port);
                _listener.Start();
                _listener.BeginAcceptTcpClient(Connection, null);

                _userCountListener = new TcpListener(IPAddress.Parse(Config.IPAddress), Config.UserCountPort);
                _userCountListener.Start();
                _userCountListener.BeginAcceptTcpClient(CountConnection, null);

                NetworkStarted = true;
                if (log) Log("网络已启动.");
            }
            catch (Exception ex)
            {
                Started = false;
                Log(ex);
            }
        }
        private static void StopNetwork(bool log = true)
        {
            TcpListener expiredListener = _listener;
            TcpListener expiredUserListener = _userCountListener;

            _listener = null;
            _userCountListener = null;

            Started = false;

            expiredListener?.Stop();
            expiredUserListener?.Stop();

            NewConnections = null;

            try
            {
                Packet p = new G.Disconnect { Reason = DisconnectReason.ServerClosing };
                for (int i = Connections.Count - 1; i >= 0; i--)
                    Connections[i].SendDisconnect(p);

                Thread.Sleep(2000);
            }
            catch (Exception ex)
            {
                Log(ex);
            }

            if (log) Log("网络已停止.");
        }

        private static void Connection(IAsyncResult result)
        {
            try
            {
                if (_listener == null || !_listener.Server.IsBound) return;

                TcpClient client = _listener.EndAcceptTcpClient(result);
                string ipAddress;

                NetworkStream stream = client.GetStream();
                
                if (stream.DataAvailable)
                {
                    StreamReader reader = new(stream);

                    // 解析Proxy Protocol头
                    string? line = reader.ReadLine();

                    if (line != null && line.StartsWith("PROXY"))
                    {
                        string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 6 && parts[0] == "PROXY" && !string.IsNullOrEmpty(parts[2]))
                            ipAddress = parts[2]; // 真实的客户端IP地址
                        else
                            ipAddress = client.Client.RemoteEndPoint.ToString().Split(':')[0];
                    }
                    else
                        ipAddress = client.Client.RemoteEndPoint.ToString().Split(':')[0];
                }
                else
                    ipAddress = client.Client.RemoteEndPoint.ToString().Split(':')[0];


                DateTime banDate;
                if (!IPBlocks.TryGetValue(ipAddress, out banDate) || banDate < Now)
                {
                    SConnection Connection = new SConnection(client, ipAddress);

                    if (Connection.Connected)
                        NewConnections.Enqueue(Connection);
                }
                else
                    Log($"IP黑名单尝试连接：IP={ipAddress} 到期={banDate.ToLocalTime()}");
            }
            catch (Exception ex)
            {
                Log(ex);
            }
            finally
            {
                ContinueListen();
            }
        }

        private static void ContinueListen()
        {
            while (NewConnections?.Count >= 15)
                Thread.Sleep(1);

            if (_listener != null && _listener.Server.IsBound)
                _listener.BeginAcceptTcpClient(Connection, null);
        }

        private static void CountConnection(IAsyncResult result)
        {
            try
            {
                if (_userCountListener == null || !_userCountListener.Server.IsBound) return;

                TcpClient client = _userCountListener.EndAcceptTcpClient(result);

                byte[] data = Encoding.ASCII.GetBytes(string.Format("c;/Zircon/{0}/;", Connections.Count));

                client.Client.BeginSend(data, 0, data.Length, SocketFlags.None, CountConnectionEnd, client);
            }
            catch {}
            finally
            {
                if (_userCountListener != null && _userCountListener.Server.IsBound)
                    _userCountListener.BeginAcceptTcpClient(CountConnection, null);
            }
        }
        private static void CountConnectionEnd(IAsyncResult result)
        {
            try
            {
                TcpClient client = result.AsyncState as TcpClient;

                if (client == null) return;

                client.Client.EndSend(result);

                client.Client.Dispose();
            }
            catch {}
        }

        #endregion

        #region WebServer
        private static HttpListener WebListener { get; set; }
        private const string ActivationCommand = "Activation", ResetCommand = "Reset", DeleteCommand = "Delete";
        private const string ActivationKey = "ActivationKey", ResetKey = "ResetKey", DeleteKey = "DeleteKey";

        private const string Completed = "Completed";
        private const string Currency = "GBP";

        private static Dictionary<decimal, int> GoldTable = new Dictionary<decimal, int>
        {
            {5M, 500},
            {10M, 1030},
            {15M, 1590},
            {20M, 2180},
            {30M, 3360},
            {50M, 5750},
            {100M, 12000},
        };

        public const string VerifiedPath = @".\Database\Store\Verified\",
            InvalidPath = @".\Database\Store\Invalid\",
            CompletePath = @".\Database\Store\Complete\";

        private static HttpListener BuyListener, IPNListener;
        public static ConcurrentQueue<IPNMessage> Messages = new ConcurrentQueue<IPNMessage>();
        public static List<IPNMessage> PaymentList = new List<IPNMessage>(), HandledPayments = new List<IPNMessage>();

        public static void StartWebServer(bool log = true)
        {
            try
            {
                WebCommandQueue = new ConcurrentQueue<WebCommand>();

                WebListener = new HttpListener();
                WebListener.Prefixes.Add(Config.WebPrefix);

                WebListener.Start();
                WebListener.BeginGetContext(WebConnection, null);

                BuyListener = new HttpListener();
                BuyListener.Prefixes.Add(Config.BuyPrefix);

                IPNListener = new HttpListener();
                IPNListener.Prefixes.Add(Config.IPNPrefix);

                BuyListener.Start();
                BuyListener.BeginGetContext(BuyConnection, null);

                IPNListener.Start();
                IPNListener.BeginGetContext(IPNConnection, null);



                WebServerStarted = true;

                if (log) Log("Web 服务已启动.");
            }
            catch (Exception ex)
            {
                WebServerStarted = false;
                Log(ex);

                if (WebListener != null && WebListener.IsListening)
                    WebListener.Stop();
                WebListener = null;

                if (BuyListener != null && BuyListener.IsListening)
                    BuyListener.Stop();
                BuyListener = null;

                if (IPNListener != null && IPNListener.IsListening)
                    IPNListener.Stop();
                IPNListener = null;
            }
        }
        public static void StopWebServer(bool log = true)
        {
            HttpListener expiredWebListener = WebListener;
            WebListener?.Stop();
            WebListener = null;

            HttpListener expiredBuyListener = BuyListener;
            expiredBuyListener?.Stop();
            BuyListener = null;
            HttpListener expiredIPNListener = IPNListener;
            expiredIPNListener?.Stop();
            IPNListener = null;
            

            WebServerStarted = false;
            expiredWebListener.Stop();
            expiredBuyListener.Stop();
            expiredIPNListener.Stop();

            if (log) Log("Web 服务已停止.");            
        }

        private static void WebConnection(IAsyncResult result)
        {
            try
            {
                HttpListenerContext context = WebListener?.EndGetContext(result);

                string command = context?.Request?.QueryString["Type"];

                switch (command)
                {
                    case ActivationCommand:
                        Activation(context);
                        break;
                    case ResetCommand:
                        ResetPassword(context);
                        break;
                    case DeleteCommand:
                        DeleteAccount(context);
                        break;
                }
            }
            catch { }
            finally
            {
                if (WebListener != null && WebListener.IsListening)
                    WebListener.BeginGetContext(WebConnection, null);
            }
        }
        private static void Activation(HttpListenerContext context)
        {
            string key = context.Request.QueryString[ActivationKey];

            if (string.IsNullOrEmpty(key)) return;

            AccountInfo account = null;
            for (int i = 0; i < AccountInfoList.Count; i++)
            {
                AccountInfo temp = AccountInfoList[i]; //Different Threads, Caution must be taken to prevent errors
                if (string.Compare(temp.ActivationKey, key, StringComparison.Ordinal) != 0) continue;

                account = temp;
                break;
            }

            if (Config.AllowWebActivation && account != null)
            {
                WebCommandQueue.Enqueue(new WebCommand(CommandType.Activation, account));
                context.Response.Redirect(Config.ActivationSuccessLink);
            }
            else
                context.Response.Redirect(Config.ActivationFailLink);

            context.Response.Close();
        }
        private static void ResetPassword(HttpListenerContext context)
        {
            string key = context.Request.QueryString[ResetKey];

            if (string.IsNullOrEmpty(key)) return;

            AccountInfo account = null;
            for (int i = 0; i < AccountInfoList.Count; i++)
            {
                AccountInfo temp = AccountInfoList[i]; //Different Threads, Caution must be taken to prevent errors
                if (string.Compare(temp.ResetKey, key, StringComparison.Ordinal) != 0) continue;

                account = temp;
                break;
            }

            if (Config.AllowWebResetPassword && account != null && account.ResetTime.AddMinutes(25) > Now)
            {
                WebCommandQueue.Enqueue(new WebCommand(CommandType.PasswordReset, account));
                context.Response.Redirect(Config.ResetSuccessLink);
            }
            else
                context.Response.Redirect(Config.ResetFailLink);

            context.Response.Close();
        }
        private static void DeleteAccount(HttpListenerContext context)
        {
            string key = context.Request.QueryString[DeleteKey];

            AccountInfo account = null;
            for (int i = 0; i < AccountInfoList.Count; i++)
            {
                AccountInfo temp = AccountInfoList[i]; //Different Threads, Caution must be taken to prevent errors
                if (string.Compare(temp.ActivationKey, key, StringComparison.Ordinal) != 0) continue;

                account = temp;
                break;
            }

            if (Config.AllowDeleteAccount && account != null)
            {
                WebCommandQueue.Enqueue(new WebCommand(CommandType.AccountDelete, account));
                context.Response.Redirect(Config.DeleteSuccessLink);
            }
            else
                context.Response.Redirect(Config.DeleteFailLink);

            context.Response.Close();
        }

        private static void BuyConnection(IAsyncResult result)
        {
            try
            {
                //HttpListenerContext context = BuyListener?.EndGetContext(result);

                //string characterName = context?.Request?.QueryString["Character"];

                //CharacterInfo character = null;
                //for (int i = 0; i < CharacterInfoList.Count; i++)
                //{
                //    if (string.Compare(CharacterInfoList[i].CharacterName, characterName, StringComparison.OrdinalIgnoreCase) != 0) continue;

                //    character = CharacterInfoList[i];
                //    break;
                //}

                //if (character?.Account?.Key != context?.Request?.QueryString["Key"])
                //    character = null;

                //string response = character == null ? Properties.Resources.CharacterNotFound : Properties.Resources.BuyGameGold.Replace("$CHARACTERNAME$", character.CharacterName);

                //if (context != null && context.Response != null)
                //using (StreamWriter writer = new StreamWriter(context.Response.OutputStream, context.Request.ContentEncoding))
                //    writer.Write(response);
            }
            catch { }
            finally
            {
                if (BuyListener != null && BuyListener.IsListening) //IsBound ?
                    BuyListener.BeginGetContext(BuyConnection, null);
            }

        }
        private static void IPNConnection(IAsyncResult result)
        {
            const string LiveURL = @"https://ipnpb.paypal.com/cgi-bin/webscr";

            const string verified = "VERIFIED";

            try
            {
                if (IPNListener == null || !IPNListener.IsListening) return;

                HttpListenerContext context = IPNListener.EndGetContext(result);

                string rawMessage;
                using (StreamReader readStream = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                    rawMessage = readStream.ReadToEnd();


                Task.Run(() =>
                {
                    string data = "cmd=_notify-validate&" + rawMessage;

                    HttpWebRequest wRequest = (HttpWebRequest)WebRequest.Create(LiveURL);

                    wRequest.Method = "POST";
                    wRequest.ContentType = "application/x-www-form-urlencoded";
                    wRequest.ContentLength = data.Length;

                    using (StreamWriter writer = new StreamWriter(wRequest.GetRequestStream(), Encoding.ASCII))
                        writer.Write(data);

                    using (StreamReader reader = new StreamReader(wRequest.GetResponse().GetResponseStream()))
                    {
                        IPNMessage message = new IPNMessage { Message = rawMessage, Verified = reader.ReadToEnd() == verified };


                        if (!Directory.Exists(VerifiedPath))
                            Directory.CreateDirectory(VerifiedPath);

                        if (!Directory.Exists(InvalidPath))
                            Directory.CreateDirectory(InvalidPath);

                        string path = (message.Verified ? VerifiedPath : InvalidPath) + Path.GetRandomFileName();

                        File.WriteAllText(path, message.Message);

                        message.FileName = path;


                        Messages.Enqueue(message);
                    }
                });

                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.Close();
            }
            catch (Exception ex)
            {
                Log(ex);
            }
            finally
            {
                if (IPNListener != null && IPNListener.IsListening) //IsBound ?
                    IPNListener.BeginGetContext(IPNConnection, null);
            }
        }
        #endregion

        public static bool Started { get; set; }
        public static bool NetworkStarted { get; set; }
        public static bool WebServerStarted { get; set; }
        public static bool Saving { get; private set; }
        public static Thread EnvirThread { get; private set; }

        public static DateTime Now, StartTime, LastWarTime;

        public static int ProcessObjectCount, LoopCount;

        public static long DBytesSent, DBytesReceived;
        public static long TotalBytesSent, TotalBytesReceived;
        public static long DownloadSpeed, UploadSpeed;
        public static int EMailsSent;

        public static bool ServerBuffChanged;

        public static byte[]? DbSystemFile { get; private set; } = null;
        public static string DbSystemFileHash { get; private set; } = string.Empty;

        public static DBCollection<AutoFightConfig> AutoFightConfList { get; set; }

        #region Database

        private static Session Session;

        public static DBCollection<MapInfo> MapInfoList;
        public static DBCollection<SafeZoneInfo> SafeZoneInfoList;
        public static DBCollection<ItemInfo> ItemInfoList;
        public static DBCollection<RespawnInfo> RespawnInfoList;
        public static DBCollection<MagicInfo> MagicInfoList;

        public static DBCollection<AccountInfo> AccountInfoList;
        public static DBCollection<CharacterInfo> CharacterInfoList;
        public static DBCollection<CharacterBeltLink> BeltLinkList;
        public static DBCollection<AutoPotionLink> AutoPotionLinkList;
        public static DBCollection<UserItem> UserItemList;
        public static DBCollection<RefineInfo> RefineInfoList;
        public static DBCollection<UserItemStat> UserItemStatsList;
        public static DBCollection<UserMagic> UserMagicList;
        public static DBCollection<BuffInfo> BuffInfoList;
        public static DBCollection<MonsterInfo> MonsterInfoList;    
        public static DBCollection<SetInfo> SetInfoList;
        public static DBCollection<AuctionInfo> AuctionInfoList;
        public static DBCollection<MailInfo> MailInfoList;
        public static DBCollection<AuctionHistoryInfo> AuctionHistoryInfoList;
        public static DBCollection<UserDrop> UserDropList;
        public static DBCollection<StoreInfo> StoreInfoList;
        public static DBCollection<BaseStat> BaseStatList;
        public static DBCollection<MovementInfo> MovementInfoList;
        public static DBCollection<NPCInfo> NPCInfoList;
        public static DBCollection<MapRegion> MapRegionList;
        public static DBCollection<GuildInfo> GuildInfoList;
        public static DBCollection<GuildMemberInfo> GuildMemberInfoList;
        public static DBCollection<UserQuest> UserQuestList;
        public static DBCollection<UserQuestTask> UserQuestTaskList;
        public static DBCollection<CompanionInfo> CompanionInfoList;
        public static DBCollection<CompanionLevelInfo> CompanionLevelInfoList;
        public static DBCollection<UserCompanion> UserCompanionList;
        public static DBCollection<UserCompanionUnlock> UserCompanionUnlockList;
        public static DBCollection<CompanionSkillInfo> CompanionSkillInfoList;
        public static DBCollection<BlockInfo> BlockInfoList;
        public static DBCollection<CastleInfo> CastleInfoList;
        public static DBCollection<UserConquest> UserConquestList;
        public static DBCollection<GameGoldPayment> GameGoldPaymentList;
        public static DBCollection<GameStoreSale> GameStoreSaleList;
        public static DBCollection<GuildWarInfo> GuildWarInfoList;
        public static DBCollection<UserConquestStats> UserConquestStatsList;
        public static DBCollection<UserFortuneInfo> UserFortuneInfoList; 
        public static DBCollection<WeaponCraftStatInfo> WeaponCraftStatInfoList;
        public static DBCollection<MonsterInfoStat> MonsterStatList;

        public static ItemInfo GoldInfo, RefinementStoneInfo, FragmentInfo, Fragment2Info, Fragment3Info, FortuneCheckerInfo, ItemPartInfo;

        public static GuildInfo StarterGuild;

        public static MapRegion? LairMapRegion { get; set; }
        public static MapRegion? MysteryShipMapRegion { get; set; }

        public static List<MonsterInfo> BossList = new List<MonsterInfo>();

        public static List<string> WelcomeList = new();

        private static ItemInfo? GoldBar = null;
        private static ItemInfo? GoldBrick = null;
        private static ItemInfo? GoldBox = null;

        public static ItemInfo? GetGoldBar()
        {
            if (GoldBar != null) return GoldBar;

            GoldBar = ItemInfoList.Binding.FirstOrDefault(x => x.ItemName == "金条");
            if (GoldBar == default(ItemInfo))
            {
                GoldBar = null;
                Log($"没有找到 [金条] 道具");
            }
            return GoldBar;
        }
        public static ItemInfo? GetGoldBrick()
        {
            if (GoldBrick != null) return GoldBrick;

            GoldBrick = ItemInfoList.Binding.FirstOrDefault(x => x.ItemName == "金砖");
            if (GoldBrick == default(ItemInfo))
            {
                GoldBrick = null;
                Log($"没有找到 [金砖] 道具");
            }
            return GoldBrick;
        }

        public static ItemInfo? GetGoldBox()
        {
            if (GoldBox != null) return GoldBox;

            GoldBox = ItemInfoList.Binding.FirstOrDefault(x => x.ItemName == "金盒");
            if (GoldBox == default(ItemInfo))
            {
                GoldBox = null;
                Log($"没有找到 [金盒] 道具");
            }
            return GoldBox;
        }
        #endregion

        #region Game Variables
        public static Random Random;

        public static ConcurrentQueue<WebCommand> WebCommandQueue;

        public static Dictionary<MapInfo, Map> Maps = new Dictionary<MapInfo, Map>();
        
        private static long _ObjectID;
        public static uint ObjectID {get{return (uint)Interlocked.Increment(ref _ObjectID);}}

        public static LinkedList<MapObject> Objects = new LinkedList<MapObject>();
        public static List<MapObject> ActiveObjects { get; set; } = new List<MapObject>();

        public static List<PlayerObject> Players = new List<PlayerObject>();
        public static List<ConquestWar> ConquestWars = new List<ConquestWar>();

        public static List<SpawnInfo> Spawns = new List<SpawnInfo>();

        private static float _DayTime;
        public static float DayTime
        {
            get { return _DayTime; }
            set
            {
                if (_DayTime == value) return;

                _DayTime = value;

                Broadcast(new S.DayChanged { DayTime = DayTime });
            }
        }

        public static bool MonsterSieging { get; set; } = false;

        public static LinkedList<CharacterInfo> Rankings { get; set; }
        public static HashSet<CharacterInfo> TopRankings { get; set; }

        public static long ConDelay, SaveDelay;
        #endregion

        public static void StartServer()
        {
            if (Started || EnvirThread != null) return;

            //EnvirThread = new Thread(EnvirLoop) { IsBackground = true };
            //EnvirThread.Start();

            if (Config.武器最高精炼等级 <= 0 || Config.武器最高精炼等级 >= 255)
                Config.武器最高精炼等级 = 20;

            if (Config.武器最高精炼等级 > Globals.WeaponExperienceList.Count)
            {
                Log($"配置项中的“武器最高精炼等级”不应高于 Globals.WeaponExperienceList 中的列表数量，现重置为：{Globals.WeaponExperienceList.Count}");
                Config.武器最高精炼等级 = Globals.WeaponExperienceList.Count;
            }

            if (Config.武器品质每低一档降低精炼上限 < 0 || Config.武器品质每低一档降低精炼上限 >= 255)
            {
                Log($"配置项中的“武器品质每低一档降低精炼上限”无效，现重置为：3");
                Config.武器品质每低一档降低精炼上限 = 3;
            }

            EnvirLoop();
        }
        public static void LoadClientHash()
        {
            ClientFileHash.Clear();
            Config.ClientPath = Config.ClientPath.Trim();

            if (string.IsNullOrEmpty(Config.ClientPath) || !Directory.Exists(Config.ClientPath))
            {
                Log($"客户端更新路径无效，请确保该路径存在并有读取权限：{Config.ClientPath}");
                return;
            }

            Log($"生成客户端更新列表 ...");
            DirectoryInfo di = new DirectoryInfo(Config.ClientPath);
            LoadDirHash(di, @"./");
            //SaveHashFile(hash_file);
            Log($"客户端更新列表已成功生成，共 {ClientFileHash.Count} 个文件");
        }
        private static void SaveHashFile(string filename)
        {
            using(StreamWriter sw = new StreamWriter(filename, false))
            {
                foreach(var item in ClientFileHash)
                {
                    sw.WriteLine($"{item.Key}={item.Value.Size},{item.Value.Hash}");
                }
            }
        }
        private static void LoadDirHash(DirectoryInfo di, string keyroot)
        {
            FileInfo[] files = di.GetFiles();
            byte[] datas;
            string key;
            foreach(var file in files)
            {
                key = Path.Combine(keyroot, file.Name);
                datas = File.ReadAllBytes(file.FullName);
                ClientFileHash[key] = new ClientUpgradeItem()
                {
                    Key = key,
                    Size = datas.Length,
                    Hash = Functions.CalcMD5(datas),
                };
            }

            DirectoryInfo[] directories = di.GetDirectories();
            foreach(var dir in directories)
            {
                LoadDirHash(dir, Path.Combine(keyroot, $"{dir.Name}/"));
            }
        }

        private static void LoadDatabase()
        {

            //@"./datas/Database/"
            DbSystemFile = File.ReadAllBytes(@"./datas/Database/System.db");
            DbSystemFileHash = Functions.CalcMD5(DbSystemFile);

            Random = new Random();

            Session = new Session(SessionMode.Users)
            {
                BackUpDelay = 60,
            };

            MapInfoList = Session.GetCollection<MapInfo>();
            SafeZoneInfoList = Session.GetCollection<SafeZoneInfo>();
            ItemInfoList = Session.GetCollection<ItemInfo>();
            MonsterInfoList = Session.GetCollection<MonsterInfo>();
            RespawnInfoList = Session.GetCollection<RespawnInfo>();
            MagicInfoList = Session.GetCollection<MagicInfo>();

            AccountInfoList = Session.GetCollection<AccountInfo>();
            CharacterInfoList = Session.GetCollection<CharacterInfo>();
            BeltLinkList = Session.GetCollection<CharacterBeltLink>();
            AutoPotionLinkList = Session.GetCollection<AutoPotionLink>();
            UserItemList = Session.GetCollection<UserItem>();
            UserItemStatsList = Session.GetCollection<UserItemStat>();
            RefineInfoList = Session.GetCollection<RefineInfo>();
            UserMagicList = Session.GetCollection<UserMagic>();
            BuffInfoList = Session.GetCollection<BuffInfo>();
            SetInfoList = Session.GetCollection<SetInfo>();
            AuctionInfoList = Session.GetCollection<AuctionInfo>();
            MailInfoList = Session.GetCollection<MailInfo>();
            AuctionHistoryInfoList = Session.GetCollection<AuctionHistoryInfo>();
            UserDropList = Session.GetCollection<UserDrop>();
            StoreInfoList = Session.GetCollection<StoreInfo>();
            BaseStatList = Session.GetCollection<BaseStat>();
            MovementInfoList = Session.GetCollection<MovementInfo>();
            NPCInfoList = Session.GetCollection<NPCInfo>();
            MapRegionList = Session.GetCollection<MapRegion>();
            GuildInfoList = Session.GetCollection<GuildInfo>();
            GuildMemberInfoList = Session.GetCollection<GuildMemberInfo>();
            UserQuestList = Session.GetCollection<UserQuest>();
            UserQuestTaskList = Session.GetCollection<UserQuestTask>();
            CompanionSkillInfoList = Session.GetCollection<CompanionSkillInfo>();

            CompanionInfoList = Session.GetCollection<CompanionInfo>();
            CompanionLevelInfoList = Session.GetCollection<CompanionLevelInfo>();
            UserCompanionList = Session.GetCollection<UserCompanion>();
            UserCompanionUnlockList = Session.GetCollection<UserCompanionUnlock>();
            BlockInfoList = Session.GetCollection<BlockInfo>();
            CastleInfoList = Session.GetCollection<CastleInfo>();
            UserConquestList = Session.GetCollection<UserConquest>();
            GameGoldPaymentList = Session.GetCollection<GameGoldPayment>();
            GameStoreSaleList = Session.GetCollection<GameStoreSale>();
            GuildWarInfoList = Session.GetCollection<GuildWarInfo>();
            UserConquestStatsList = Session.GetCollection<UserConquestStats>();
            UserFortuneInfoList = Session.GetCollection<UserFortuneInfo>();
            WeaponCraftStatInfoList = Session.GetCollection<WeaponCraftStatInfo>();
            MonsterStatList = Session.GetCollection<MonsterInfoStat>();

             GoldInfo = ItemInfoList.Binding.First(x => x.Effect == ItemEffect.Gold);
            RefinementStoneInfo = ItemInfoList.Binding.First(x => x.Effect == ItemEffect.RefinementStone);
            FragmentInfo = ItemInfoList.Binding.First(x => x.Effect == ItemEffect.Fragment1);
            Fragment2Info = ItemInfoList.Binding.First(x => x.Effect == ItemEffect.Fragment2);
            Fragment3Info = ItemInfoList.Binding.First(x => x.Effect == ItemEffect.Fragment3);

            ItemPartInfo = ItemInfoList.Binding.First(x => x.Effect == ItemEffect.ItemPart);
            FortuneCheckerInfo = ItemInfoList.Binding.First(x => x.Effect == ItemEffect.FortuneChecker);


            MysteryShipMapRegion = MapRegionList?.Binding?.FirstOrDefault(x=> x.Map.Description == Config.异界之门关联地图名称);
            LairMapRegion = MapRegionList?.Binding?.FirstOrDefault(x => x.Map.Description == Config.地狱之门关联地图名称);
            StarterGuild = GuildInfoList.Binding.FirstOrDefault(x => x.StarterGuild);

            AutoFightConfList = Session.GetCollection<AutoFightConfig>();

            if (StarterGuild == null)
            {
                StarterGuild = GuildInfoList.CreateNewObject();
                StarterGuild.StarterGuild = true;
            }

            StarterGuild.GuildName = Config.StarterGuildName;

            #region Create Ranks
            Rankings = new LinkedList<CharacterInfo>();
            TopRankings = new HashSet<CharacterInfo>();
            foreach (CharacterInfo info in CharacterInfoList.Binding)
            {
                if (info.Deleted || !info.Account.Activated 
                    || info.Account.Banned 
                    || info.Account.EMailAddress == SuperAdmin) continue;

                info.RankingNode = Rankings.AddLast(info);
                RankingSort(info, false);
            }
            UpdateLead();
            #endregion

            for (int i = UserQuestList.Count - 1; i >= 0; i--)
                if (UserQuestList[i].QuestInfo == null)
                    UserQuestList[i].Delete();

            for (int i = UserQuestTaskList.Count - 1; i >= 0; i--)
                if (UserQuestTaskList[i].Task == null)
                    UserQuestTaskList[i].Delete();

            foreach (MonsterInfo monster in MonsterInfoList.Binding)
            {
                if (!monster.IsBoss) continue;
                if (monster.Drops.Count == 0) continue;

                BossList.Add(monster);

            }

            Messages = new ConcurrentQueue<IPNMessage>();

            PaymentList.Clear();

            if (Directory.Exists(VerifiedPath))
            {
                string[] files = Directory.GetFiles(VerifiedPath);

                foreach (string file in files)
                    Messages.Enqueue(new IPNMessage { FileName = file, Message = File.ReadAllText(file), Verified = true });
            }

            Session.BackUpSpace = Config.玩家数据备份间隔;
        }
        //Only works on Increasing EXP, still need to do Rebirth or loss of exp ranking update.
        public static void RankingSort(CharacterInfo character, bool updateLead = true)
        {
            if (character.Deleted
                || !character.Account.Activated 
                || character.Account.Banned 
                || character.Account.EMailAddress == SuperAdmin)
                return;

            bool changed = false;

            LinkedListNode<CharacterInfo>? node;
            while ((node = character.RankingNode?.Previous) != null)
            {
                if (node.Value.Rebirth > character.Rebirth) break;

                if (node.Value.Rebirth == character.Rebirth 
                    && node.Value.Level > character.Level) 
                    break;

                if (node.Value.Rebirth == character.Rebirth 
                    && node.Value.Level == character.Level 
                    && node.Value.Experience >= character.Experience) 
                    break;

                changed = true;

                Rankings.Remove(character.RankingNode);
                Rankings.AddBefore(node, character.RankingNode);
            }

            if (!updateLead || (TopRankings.Count >= 20 && !changed)) return; //5 * 4

            UpdateLead();
        }
        public static void QuitRanking(AccountInfo account)
        {
            foreach (var ch in account.Characters)
                QuitRanking(ch);
        }
        public static void QuitRanking(CharacterInfo character)
        {
            if (Rankings.Contains(character))
                Rankings.Remove(character);
        }
        public static void AddRanking(AccountInfo account)
        {
            if (!account.Activated) return;

            foreach(var ch in account.Characters)
                if (!ch.Deleted)
                    AddRanking(ch);
        }
        public static void AddRanking(CharacterInfo character)
        {
            if (Rankings.Contains(character)) return;

            Rankings.AddLast(character);
            RankingSort(character, true);
        }
        public static void UpdateLead()
        {
            HashSet<CharacterInfo> newTopRankings = new HashSet<CharacterInfo>();
            
            int war = 5, wiz = 5, tao = 5, ass = 5;

            foreach (CharacterInfo cInfo in Rankings)
            {
                if (cInfo.Deleted 
                    || !cInfo.Account.Activated 
                    || cInfo.Account.Banned 
                    || cInfo.Account.EMailAddress == SuperAdmin) 
                    continue;

                switch (cInfo.Class)
                {
                    case MirClass.Warrior:
                        if (war == 0) continue;
                        war--;
                        newTopRankings.Add(cInfo);
                        break;
                    case MirClass.Wizard:
                        if (wiz == 0) continue;
                        wiz--;
                        newTopRankings.Add(cInfo);
                        break;
                    case MirClass.Taoist:
                        if (tao == 0) continue;
                        tao--;
                        newTopRankings.Add(cInfo);
                        break;
                    case MirClass.Assassin:
                        if (ass == 0) continue;
                        ass--;
                        newTopRankings.Add(cInfo);
                        break;
                }

                if (war == 0 && wiz == 0 && tao == 0 && ass == 0) break;
            }

            foreach (CharacterInfo info in TopRankings)
            {
                if (newTopRankings.Contains(info)) continue;

                info.Player?.BuffRemove(BuffType.Ranking);
            }

            foreach (CharacterInfo info in newTopRankings)
            {
                if (TopRankings.Contains(info)) continue;

                info.Player?.BuffAdd(BuffType.Ranking, TimeSpan.MaxValue, null, true, false, TimeSpan.Zero);
            }

            TopRankings = newTopRankings;
        }

        public static void LoadSensitiveWords()
        {
            try
            {
                SensitiveWords = null;
                string file = "./datas/敏感词.txt";
                if (!File.Exists(file))
                {
                    Log($"敏感词库文件 {file} 不存在，关闭敏感词判断");
                    return;
                }

                using FileStream stream = File.OpenRead(file);
                using var reader = new StreamReader(stream);
                string? line;
                List<string> words = new();
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim().ToLower();
                    if (string.IsNullOrEmpty(line)) continue;

                    words.Add(line);
                }

                if (words.Count > 0)
                    SensitiveWords = new(words.ToArray());
                
                Log($"共加载 {words.Count} 条敏感词");
            }
            catch (Exception ex) 
            { 
                Log($"加载敏感词库发生异常：{ex.Message}");
                if(!string.IsNullOrEmpty(ex.StackTrace))
                    Log(ex.StackTrace);
            }
        }

        private static void StartEnvir()
        {
            LoadSensitiveWords();
            LoadDatabase();

            if (string.IsNullOrEmpty(Config.WelcomeWordsFile) || !File.Exists(Config.WelcomeWordsFile))
                Log($"欢迎语文件配置无效，取消欢迎语.");
            else
            {
                try
                {
                    using (FileStream stream = File.OpenRead(Config.WelcomeWordsFile))
                    {
                        var reader = new StreamReader(stream);
                        string? line;
                        string? tmp;
                        while((line = reader.ReadLine()) != null)
                        {
                            tmp = line.Trim();
                            if (string.IsNullOrEmpty(tmp)) continue;
                            
                            WelcomeList.Add(tmp);
                        }
                    }
                }
                catch(Exception ex) 
                { 
                    Log($"加载欢迎语文件出现异常 {Config.WelcomeWordsFile}"); 
                }
            }

            #region Load Files
            for (int i = 0; i < MapInfoList.Count; i++)
                Maps[MapInfoList[i]] = new Map(MapInfoList[i]);


            //Parallel.ForEach(Maps, x => x.Value.Load());
            foreach (KeyValuePair<MapInfo, Map> pair in Maps) { pair.Value.Load(); }

            #endregion

            foreach (Map map in Maps.Values)
                map.Setup();

            Parallel.ForEach(MapRegionList.Binding, x =>
            {
                Map map = GetMap(x.Map);

                if (map == null) return;

                x.CreatePoints(map.Width);
            });
            
            CreateSafeZones();

            CreateMovements();

            CreateNPCs();

            CreateSpawns();
        }

        private static void CreateMovements()
        {
            foreach (MovementInfo movement in MovementInfoList.Binding)
            {
                if (movement.SourceRegion == null) continue;

                Map sourceMap = GetMap(movement.SourceRegion.Map);
                if (sourceMap == null)
                {
                    Log(string.Format("[地图连接点] 源映射错误, SourceRegion: {0}", movement.SourceRegion.ServerDescription));
                    continue;
                }
                
                if (movement.DestinationRegion == null)
                {
                    Log(string.Format("[地图连接点] 目标区域没有找到, SourceRegion: {0}", movement.SourceRegion.ServerDescription));
                    continue;
                }

                Map destMap = GetMap(movement.DestinationRegion.Map);
                if (destMap == null)
                {
                    Log(string.Format("[地图连接点] 目标映射错误, DestinationRegion: {0}", movement.DestinationRegion.ServerDescription));
                    continue;
                }


                foreach (Point sPoint in movement.SourceRegion.PointList)
                {
                    Cell source = sourceMap.GetCell(sPoint);

                    if (source == null)
                    {
                        Log(string.Format("[地图连接点] 来源错误, SourceRegion: {0}, X:{1}, Y:{2}", movement.SourceRegion.ServerDescription, sPoint.X, sPoint.Y));
                        continue;
                    }

                    if (source.Movements == null)
                        source.Movements = new List<MovementInfo>();

                    source.Movements.Add(movement);
                }
            }
        }

        private static void CreateNPCs()
        {
            foreach (NPCInfo info in NPCInfoList.Binding)
            {
                if (info.Region == null) continue;
                
                Map map = GetMap(info.Region.Map);

                if (map == null)
                {
                    Log(string.Format("[NPC] 映射错误, NPC: {0}, Map: {1}", info.NPCName, info.Region.ServerDescription));
                    continue;
                }

                NPCObject ob = new NPCObject
                {
                    NPCInfo = info,
                };

                if (!ob.Spawn(info.Region))
                    Log(string.Format("[NPC] 无法创建 NPC, Region: {0}, NPC: {1}", info.Region.ServerDescription, info.NPCName));
            }
        }

        private static void CreateSafeZones()
        {
            foreach (SafeZoneInfo info in SafeZoneInfoList.Binding)
            {
                if (info.Region == null) continue;

                Map map = GetMap(info.Region.Map);

                if (map == null)
                {
                    Log(string.Format("[安全区] 映射错误, Map: {0}", info.Region.ServerDescription));
                    continue;
                }
                
                HashSet<Point> edges = new HashSet<Point>();

                foreach (Point point in info.Region.PointList)
                {
                    Cell cell = map.GetCell(point);

                    if (cell == null)
                    {
                        Log(string.Format("[安全区] 位置错误, Region: {0}, X: {1}, Y: {2}.", info.Region.ServerDescription, point.X, point.Y));

                        continue;
                    }

                    cell.SafeZone = info;
                    
                    for (int i = 0; i < 8; i++)
                    {
                        Point test = Functions.Move(point, (MirDirection) i);

                        if (info.Region.PointList.Contains(test)) continue;

                        if (map.GetCell(test) == null) continue;

                        edges.Add(test);
                    }
                }

                map.HasSafeZone = true;

                foreach (Point point in edges)
                {
                    SpellObject ob = new SpellObject
                    {
                        Visible = true,
                        DisplayLocation = point,
                        TickCount = 10,
                        TickFrequency = TimeSpan.FromDays(365),
                        Effect = SpellEffect.SafeZone
                    };

                    ob.Spawn(map.Info, point);
                }

                if (info.BindRegion == null) continue;

                map = GetMap(info.BindRegion.Map);

                if (map == null)
                {
                    Log(string.Format("[安全区] 绑定映射失败, Map: {0}", info.Region.ServerDescription));
                    continue;
                }
                
                foreach (Point point in info.BindRegion.PointList)
                {
                    Cell cell = map.GetCell(point);

                    if (cell == null)
                    {
                        Log(string.Format("[安全区] 位置错误, Region: {0}, X: {1}, Y: {2}.", info.BindRegion.ServerDescription, point.X, point.Y));
                        continue;
                    }

                    info.ValidBindPoints.Add(point);
                }

            }
        }

        private static void CreateSpawns()
        {
            foreach (RespawnInfo info in RespawnInfoList.Binding)
            {
                if (info.Monster == null) continue;
                if (info.Region == null) continue;

                Map map = GetMap(info.Region.Map);

                if (map == null)
                {
                    Log(string.Format("[刷怪] 映射错误, Map: {0}", info.Region.ServerDescription));
                    continue;
                }
                
                Spawns.Add(new SpawnInfo(info));

            }
        }

        private static void StopEnvir()
        {
            Now = DateTime.MinValue;

            Session = null;


            MapInfoList = null;
            SafeZoneInfoList = null;
            AccountInfoList = null;
            CharacterInfoList = null;


            MapInfoList = null;
            SafeZoneInfoList = null;
            ItemInfoList = null;
            MonsterInfoList = null;
            RespawnInfoList = null;
            MagicInfoList = null;

            AccountInfoList = null;
            CharacterInfoList = null;
            BeltLinkList = null;
            UserItemList = null;
            UserItemStatsList = null;
            UserMagicList = null;
            BuffInfoList = null;
            SetInfoList = null;

            Rankings = null;
            Random = null;


            Maps.Clear();
            Objects.Clear();
            ActiveObjects.Clear();
            Players.Clear();

            Spawns.Clear();

            _ObjectID = 0;


            EnvirThread = null;
        }

        public static void AutoClearUserDatas(bool force = false)
        {
            if (!force && (Config.数据清理间隔分钟 <= 0 || AutoClearUserDatasTime > Now))
                return;

            AutoClearUserDatasTime = Now.AddMinutes(Config.数据清理间隔分钟);
            var count = ClearUserDatas();

            Log($"自动清理用户垃圾数据：共清理 {count} 条");
        }
        public static void EnvirLoop()
        {
            Now = Time.Now;
            DateTime DBTime = Now + Config.DBSaveDelay;

            StartEnvir();
            StartNetwork();
            StartWebServer();

            Started = NetworkStarted;
            
            int count = 0, loopCount = 0;
            DateTime nextCount = Now.AddSeconds(1), UserCountTime = Now.AddMinutes(5), saveTime;
            long previousTotalSent = 0, previousTotalReceived = 0;
            int lastindex = 0;
            long conDelay = 0;
            Thread logThread = new Thread(WriteLogsLoop) { IsBackground = true };
            logThread.Start();

            LastWarTime = Now;

            bool ship = MysteryShipMapRegion != null && MysteryShipMapRegion.PointList.Count > 0;

            if (ship)
                Log($"幽灵船通向地图：{MysteryShipMapRegion?.Map?.Description}");
            else
                Log("幽灵船关闭");

            bool lair = LairMapRegion != null && LairMapRegion.PointList.Count > 0;

            if (lair)
                Log($"地狱之门通向地图：{LairMapRegion?.Map?.Description}");
            else
                Log("地狱之门关闭");

            Log(string.Format("加载耗时: {0}", Functions.ToString(Time.Now - Now, true)));

            if (Config.数据清理间隔分钟 > 0)
                Log($"共清理 {ClearUserDatas(true)} 条数据");

            while (Started)
            {
                Now = Time.Now;
                loopCount++;

                try
                {
                    SConnection connection;
                    while (!NewConnections.IsEmpty)
                    {
                        if (!NewConnections.TryDequeue(out connection)) break;
                        int ipCount;
                        IPCount.TryGetValue(connection.IPAddress, out ipCount);
                        IPCount[connection.IPAddress] = ipCount + 1;

                        Connections.Add(connection);
                    }

                    long bytesSent = 0;
                    long bytesReceived = 0;

                    for (int i = Connections.Count - 1; i >= 0; i--)
                    {
                        if (i >= Connections.Count) break;

                        connection = Connections[i];

                        try{ connection.Process(); }
                        catch(Exception ex) { Log(ex); }
                        bytesSent += connection.TotalBytesSent;
                        bytesReceived += connection.TotalBytesReceived;
                    }

                    long delay = (Time.Now - Now).Ticks / TimeSpan.TicksPerMillisecond;
                    if (delay > conDelay)
                        conDelay = delay;

                    for (int i = Players.Count - 1; i >= 0; i--)
                    {
                        try { Players[i].StartProcess(); }
                        catch(Exception ex) { Log(ex); }
                    }

                    TotalBytesSent = DBytesSent + bytesSent;
                    TotalBytesReceived = DBytesReceived + bytesReceived;

                    if (ServerBuffChanged)
                    {
                        for (int i = Players.Count - 1; i >= 0; i--)
                        {
                            try { Players[i].ApplyServerBuff(); }
                            catch (Exception ex) { Log(ex); }
                        }

                        ServerBuffChanged = false;
                    }

                   DateTime  loopTime = Time.Now.AddMilliseconds(1);

                    if (lastindex < 0) lastindex = ActiveObjects.Count;

                    while (Time.Now <= loopTime)
                    {
                        lastindex--;

                        if (lastindex >= ActiveObjects.Count) continue;

                        if (lastindex < 0) break;

                        MapObject ob = ActiveObjects[lastindex];

                        if (ob.Race == ObjectType.Player) continue;

                        try
                        {
                            ob.StartProcess();
                            count++;
                        }
                        catch (Exception ex)
                        {
                            try { ob.Despawn(); }
                            catch
                            {
                                ActiveObjects.Remove(ob);
                                ob.Activated = false;
                            }
                           
                            Log($"处理地图元素时时发生异常：【{ob.Name}-{ob.Race}】");
                            Log(ex);
                            File.AppendAllText(@".\Errors.txt", ex.StackTrace + Environment.NewLine);
                        }
                    }

                    if (Now >= nextCount)
                    {
                        if (Now >= DBTime && !Saving)
                        {
                            DBTime = Time.Now + Config.DBSaveDelay;
                            saveTime = Time.Now;

                            Save();

                            SaveDelay = (Time.Now - saveTime).Ticks / TimeSpan.TicksPerMillisecond;
                        }

                        ProcessObjectCount = count;
                        LoopCount = loopCount;
                        ConDelay = conDelay;

                        count = 0;
                        loopCount = 0;
                        conDelay = 0;

                        DownloadSpeed = TotalBytesReceived - previousTotalReceived;
                        UploadSpeed = TotalBytesSent - previousTotalSent;

                        previousTotalReceived = TotalBytesReceived;
                        previousTotalSent = TotalBytesSent;

                        if (Now >= UserCountTime)
                        {
                            UserCountTime = Now.AddMinutes(5);

                            Dictionary<string, int> a = new();
                            foreach (var player in SEnvir.Players)
                            {
                                if (player.Character?.Account == null) continue;
                                if (player.Character.Account.EMailAddress == SEnvir.SuperAdmin) continue;

                                if (!a.ContainsKey(player.Character.Account.LastSum))
                                    a.Add(player.Character.Account.LastSum, 0);
                            }

                            foreach (SConnection conn in Connections)
                            {
                                if (conn == null || !conn.Connected || conn.Disconnecting) continue;

                                if ((conn.Account?.Identify ?? AccountIdentity.Normal) != AccountIdentity.Normal)
                                    conn.ReceiveChat(string.Format(conn.Language.OnlineCount, Players.Count, Connections.Count(x => x.Stage == GameStage.Observer), a.Count), MessageType.Hint);

                                switch (conn.Stage)
                                {
                                    case GameStage.Game:
                                        if (conn.Player.Character.Observable && conn.Observers.Count > 0)
                                            conn.ReceiveChat(string.Format(conn.Language.ObserverCount, conn.Observers.Count), MessageType.Hint);
                                        break;
                                    case GameStage.Observer:
                                        conn.ReceiveChat(string.Format(conn.Language.ObserverCount, conn.Observed.Observers.Count), MessageType.Hint);
                                        break;
                                }
                            }
                        }

                        CalculateLights();

                        CheckGuildWars();

                        foreach (KeyValuePair<MapInfo, Map> pair in Maps)
                        {
                            try { pair.Value.Process(); }
                            catch (Exception ex) { Log(ex); }

                        }

                        foreach (SpawnInfo spawn in Spawns)
                        {
                            try { spawn.DoSpawn(false); }
                            catch (Exception ex) { Log(ex); }
                        }

                        for (int i = ConquestWars.Count - 1; i >= 0; i--)
                        {
                            try { ConquestWars[i].Process(); }
                            catch (Exception ex) { Log(ex); }
                        }

                        while (!WebCommandQueue.IsEmpty)
                        {
                            WebCommand? webCommand;
                            if (!WebCommandQueue.TryDequeue(out webCommand) || webCommand == null) continue;

                            switch (webCommand.Command)
                            {
                                case CommandType.None:
                                    break;
                                case CommandType.Activation:
                                    webCommand.Account.Activated = true;
                                    webCommand.Account.ActivationKey = string.Empty;
                                    break;
                                case CommandType.PasswordReset:
                                    string password = Functions.RandomString(Random, 10);

                                    webCommand.Account.Password = CreateHash(password);
                                    webCommand.Account.ResetKey = string.Empty;
                                    webCommand.Account.WrongPasswordCount = 0;
                                    SendResetPasswordEmail(webCommand.Account, password);
                                    break;
                                case CommandType.AccountDelete:
                                    if (webCommand.Account.Activated) continue;

                                    webCommand.Account.Delete();
                                    break;
                            }
                        }

                        if (Config.ProcessGameGold)
                            ProcessGameGold();

                        nextCount = Now.AddSeconds(1);

                        if (nextCount.Day != Now.Day)
                        {
                            foreach (GuildInfo guild in GuildInfoList.Binding)
                            {
                                guild.DailyContribution = 0;
                                guild.DailyGrowth = 0;

                                foreach (GuildMemberInfo member in guild.Members)
                                {
                                    try
                                    {
                                        member.DailyContribution = 0;
                                        if (member.Account?.Connection?.Player == null) continue;

                                        member.Account.Connection.Enqueue(new S.GuildDayReset { ObserverPacket = false });
                                    }
                                    catch (Exception ex) { Log(ex); }

                                }
                            }

                            GC.Collect(2, GCCollectionMode.Forced);
                        }

                        foreach (CastleInfo info in CastleInfoList.Binding)
                        {
                            if (nextCount.TimeOfDay < info.StartTime) continue;
                            if (Now.TimeOfDay > info.StartTime) continue;

                            try { StartConquest(info, false); }
                            catch (Exception ex) { Log(ex); }
                        }
                    }

                    AutoClearUserDatas();
                }
                catch (Exception ex)
                {
                    Session = null;

                    Log(ex);
                    File.AppendAllText(@".\Errors.txt", ex.StackTrace + Environment.NewLine);

                    Packet p = new G.Disconnect { Reason = DisconnectReason.Crashed };
                    for (int i = Connections.Count - 1; i >= 0; i--)
                        Connections[i].SendDisconnect(p);

                    Thread.Sleep(3000);
                    break;
                }
            }

            StopWebServer();
            StopNetwork();

            while (Saving) Thread.Sleep(1);
            if (Session != null)
                Session.BackUpDelay = 0;
            Save();
            while (Saving) Thread.Sleep(1);

            StopEnvir();
        }
        
        private static void Save()
        {
            if (Session == null) return;

            Saving = true;
            Session.Save(false);
            
            HandledPayments.AddRange(PaymentList);

            Thread saveThread = new Thread(CommitChanges) { IsBackground = true };
            saveThread.Start(Session);
        }
        private static void CommitChanges(object data)
        {
            Session session = (Session)data;
            session.Commit();

            foreach (IPNMessage message in HandledPayments)
            {
                if (message.Duplicate)
                {
                    File.Delete(message.FileName);
                    continue;
                }

                if (!Directory.Exists(CompletePath))
                    Directory.CreateDirectory(CompletePath);

                File.Move(message.FileName, CompletePath + Path.GetFileName(message.FileName) + ".txt");
                PaymentList.Remove(message);
            }
            HandledPayments.Clear();


            Saving = false;
        }
        private static void WriteLogsLoop()
        {
            DateTime NextLogTime = Now.AddSeconds(10);

            while (Started)
            {
                if (Now < NextLogTime)
                {
                    Thread.Sleep(1);
                    continue;
                }

                WriteLogs();

                NextLogTime = Now.AddSeconds(10);
            }
        }
        private static void WriteLogs()
        {
            List<string> lines = new List<string>();
            while (!Logs.IsEmpty)
            {
                string line;
                if (!Logs.TryDequeue(out line)) continue;
                lines.Add(line);
            }

            File.AppendAllLines("./datas/Logs.txt", lines);

            lines.Clear();

            while (!ChatLogs.IsEmpty)
            {
                string line;
                if (!ChatLogs.TryDequeue(out line)) continue;
                lines.Add(line);
            }

            File.AppendAllLines("./datas/Chat Logs.txt", lines);

            lines.Clear();
            
            /*
            while (!GamePlayLogs.IsEmpty)
            {
                if (!GamePlayLogs.TryDequeue(out string line)) continue;
                lines.Add(line);
            }

            File.AppendAllLines(@".\Game Play.txt", lines);
            */

            lines.Clear();
        }
        private static void ProcessGameGold()
        {
            while (!Messages.IsEmpty)
            {
                IPNMessage message;

                if (!Messages.TryDequeue(out message) || message == null) return;

                PaymentList.Add(message);

                if (!message.Verified)
                {
                    Log("无效的 PAYPAL 交易 " + message.Message);
                    continue;
                }

                //Add message to another list for file moving

                string[] data = message.Message.Split('&');

                Dictionary<string, string> values = new Dictionary<string, string>();

                for (int i = 0; i < data.Length; i++)
                {
                    string[] keypair = data[i].Split('=');

                    values[keypair[0]] = keypair.Length > 1 ? keypair[1] : null;
                }

                bool error = false;
                string tempString, paymentStatus, transactionID;
                decimal tempDecimal;
                int tempInt;

                if (!values.TryGetValue("payment_status", out paymentStatus))
                    error = true;

                if (!values.TryGetValue("txn_id", out transactionID))
                    error = true;


                //Check that Txn_id has not been used
                for (int i = 0; i < GameGoldPaymentList.Count; i++)
                {
                    if (GameGoldPaymentList[i].TransactionID != transactionID) continue;
                    if (GameGoldPaymentList[i].Status != paymentStatus) continue;


                    Log(string.Format("[重复交易] ID:{0} Status:{1}.", transactionID, paymentStatus));
                    message.Duplicate = true;
                    return;
                }

                GameGoldPayment payment = GameGoldPaymentList.CreateNewObject();
                payment.RawMessage = message.Message;
                payment.Error = error;

                if (values.TryGetValue("payment_date", out tempString))
                    payment.PaymentDate = HttpUtility.UrlDecode(tempString);

                if (values.TryGetValue("receiver_email", out tempString))
                    payment.Receiver_EMail = HttpUtility.UrlDecode(tempString);
                else
                    payment.Error = true;

                if (values.TryGetValue("mc_fee", out tempString) && decimal.TryParse(tempString, out tempDecimal))
                    payment.Fee = tempDecimal;
                else
                    payment.Error = true;

                if (values.TryGetValue("mc_gross", out tempString) && decimal.TryParse(tempString, out tempDecimal))
                    payment.Price = tempDecimal;
                else
                    payment.Error = true;

                if (values.TryGetValue("custom", out tempString))
                    payment.CharacterName = tempString;
                else
                    payment.Error = true;

                if (values.TryGetValue("mc_currency", out tempString))
                    payment.Currency = tempString;
                else
                    payment.Error = true;

                if (values.TryGetValue("txn_type", out tempString))
                    payment.TransactionType = tempString;
                else
                    payment.Error = true;

                if (values.TryGetValue("payer_email", out tempString))
                    payment.Payer_EMail = HttpUtility.UrlDecode(tempString);

                if (values.TryGetValue("payer_id", out tempString))
                    payment.Payer_ID = tempString;

                payment.Status = paymentStatus;
                payment.TransactionID = transactionID;
                //Check if Paymentstats == completed
                switch (payment.Status)
                {
                    case "Completed":
                        break;
                }
                if (payment.Status != Completed) continue;

                //check that receiver_email is my primary paypal email
                if (string.Compare(payment.Receiver_EMail, Config.ReceiverEMail, StringComparison.OrdinalIgnoreCase) != 0)
                    payment.Error = true;

                //check that paymentamount/current are correct
                if (payment.Currency != Currency)
                    payment.Error = true;

                if (GoldTable.TryGetValue(payment.Price, out tempInt))
                    payment.GameGoldAmount = tempInt;
                else
                    payment.Error = true;

                CharacterInfo character = GetCharacter(payment.CharacterName);

                if (character == null || payment.Error)
                {
                    Log(string.Format("[交易错误] ID:{0} Status:{1}, Amount{2}.", transactionID, paymentStatus, payment.Price));
                    continue;
                }

                payment.Account = character.Account;
                payment.Account.GameGold += payment.GameGoldAmount;
                character.Account.Connection.ReceiveChat(string.Format(character.Account.Connection.Language.PaymentComplete, payment.GameGoldAmount), MessageType.System);
                character.Player.Enqueue(new S.GameGoldChanged { GameGold = payment.Account.GameGold });

                AccountInfo referral = payment.Account.Referral;

                if (referral != null)
                {
                    referral.HuntGold += payment.GameGoldAmount / 10;

                    if (referral.Connection != null)
                    {
                        referral.Connection.ReceiveChat(string.Format(referral.Connection.Language.ReferralPaymentComplete, payment.GameGoldAmount / 10), MessageType.System);

                        if (referral.Connection.Stage == GameStage.Game)
                            referral.Connection.Player.Enqueue(new S.HuntGoldChanged { HuntGold = referral.GameGold });
                    }
                }

                Log(string.Format("[游戏币购买] Character: {0}, Amount: {1}.", character.CharacterName, payment.GameGoldAmount));
            }
        }

        public static void CheckGuildWars()
        {
            TimeSpan change = Now - LastWarTime;
            LastWarTime = Now;

            for (int i = GuildWarInfoList.Count - 1; i >= 0; i--)
            {
                GuildWarInfo warInfo = GuildWarInfoList[i];

                warInfo.Duration -= change;

                if (warInfo.Duration > TimeSpan.Zero) continue;

                foreach (GuildMemberInfo member in warInfo.Guild1.Members)
                    member.Account.Connection.Player.Enqueue(new S.GuildWarFinished { GuildName = warInfo.Guild2.GuildName });

                foreach (GuildMemberInfo member in warInfo.Guild2.Members)
                    member.Account.Connection.Player.Enqueue(new S.GuildWarFinished { GuildName = warInfo.Guild1.GuildName });

                warInfo.Guild1 = null;
                warInfo.Guild2 = null;

                warInfo.Delete();
            }

        }
        public static void CalculateLights()
        {
            DayTime = Math.Max(0.05F, Math.Abs((float)Math.Round(((Now.TimeOfDay.TotalMinutes * Config.DayCycleCount) % 1440) / 1440F * 2 - 1, 2))); //12 hour rotation
        }
        public static void StartConquest(CastleInfo info, bool forced)
        {
            List<GuildInfo> participants = new List<GuildInfo>();

            if (!forced)
            {
                foreach (UserConquest conquest in UserConquestList.Binding)
                {
                    if (conquest.Castle != info) continue;
                    if (conquest.WarDate > Now.Date) continue;

                    participants.Add(conquest.Guild);
                }
                
                if (participants.Count == 0) return;

                foreach (GuildInfo guild in GuildInfoList.Binding)
                {
                    if (guild.Castle != info) continue;

                    participants.Add(guild);
                }

            }

            ConquestWar War = new ConquestWar
            {
                Castle = info,
                Participants = participants,
                EndTime = Now + info.Duration,
                StartTime = Now.Date + info.StartTime,
            };

            War.StartWar();
        }
        public static void StartConquest(CastleInfo info, List<GuildInfo> participants)
        {

            ConquestWar War = new ConquestWar
            {
                Castle = info,
                Participants = participants,
                EndTime = Now + TimeSpan.FromMinutes(15),
                StartTime = Now.Date + info.StartTime,
            };

            War.StartWar();
        }


        public static UserItem CreateFreshItem(UserItem item)
        {
            UserItem freshItem = UserItemList.CreateNewObject();

            freshItem.Colour = item.Colour;

            freshItem.Info = item.Info;
            freshItem.CurrentDurability = item.CurrentDurability;
            freshItem.MaxDurability = item.MaxDurability;

            freshItem.Flags = item.Flags;

            freshItem.ExpireTime = item.ExpireTime;

            foreach (UserItemStat stat in item.AddedStats)
                freshItem.AddStat(stat.Stat, stat.Amount, stat.StatSource);
            freshItem.StatsChanged();

            return freshItem;
        }
        public static UserItem CreateOldItem(UserItem item, decimal rate)
        {
            UserItem freshItem = UserItemList.CreateNewObject();

            freshItem.Colour = item.Colour;
            freshItem.Info = item.Info;


            int max_dura = (int)(rate * item.CurrentDurability);


            if (Random.Next(100) <= 70)
                freshItem.CurrentDurability = max_dura / 2 + Random.Next(max_dura / 2);
            else
                freshItem.CurrentDurability = Random.Next(max_dura);

            if (freshItem.CurrentDurability <= 0) freshItem.CurrentDurability = 1;


            freshItem.MaxDurability = item.MaxDurability;

            freshItem.Flags = item.Flags;

            freshItem.ExpireTime = item.ExpireTime;

            foreach (UserItemStat stat in item.AddedStats)
                freshItem.AddStat(stat.Stat, stat.Amount, stat.StatSource);
            freshItem.StatsChanged();

            return freshItem;
        }
        public static UserItem CreateOldItem(ItemInfo info, decimal rate)
        {
            UserItem item = UserItemList.CreateNewObject();

            item.Colour = Color.FromArgb(Random.Next(256), Random.Next(256), Random.Next(256));

            item.Info = info;
            int max_dura = (int)(rate * info.Durability);

            if (Random.Next(100) <= 70)
                item.CurrentDurability = max_dura / 2 + Random.Next(max_dura / 2);
            else
                item.CurrentDurability = Random.Next(max_dura);

            if (item.CurrentDurability <= 0) item.CurrentDurability = 1;

            item.MaxDurability = info.Durability;

            return item;
        }
        public static UserItem CreateOldItem(ItemCheck check, decimal rate)
        {
            UserItem item = check.Item != null ? CreateOldItem(check.Item, rate) : CreateOldItem(check.Info, rate);

            item.Flags = check.Flags;
            item.ExpireTime = check.ExpireTime;

            if (item.Info.Effect == ItemEffect.Gold || item.Info.Effect == ItemEffect.Experience)
                item.Count = check.Count;
            else
                item.Count = Math.Min(check.Info.StackSize, check.Count);

            check.Count -= item.Count;

            return item;
        }
        public static UserItem CreateFreshItem(ItemCheck check)
        {
            UserItem item = check.Item != null ? CreateFreshItem(check.Item) : CreateFreshItem(check.Info);
            
            item.Flags = check.Flags;
            item.ExpireTime = check.ExpireTime;

            if (item.Info.Effect == ItemEffect.Gold || item.Info.Effect == ItemEffect.Experience)
                item.Count = check.Count;
            else
                item.Count = Math.Min(check.Info.StackSize, check.Count);
            
            check.Count -= item.Count;

            return item;
        }
        public static UserItem CreateFreshItem(ItemInfo info)
        {
            UserItem item = UserItemList.CreateNewObject();

            item.Colour = Color.FromArgb(Random.Next(256), Random.Next(256), Random.Next(256));

            item.Info = info;
            item.CurrentDurability = info.Durability;
            item.MaxDurability = info.Durability;
            
            return item;
        }
        public static UserItem CreateDropItem(ItemCheck check, int chance = 15)
        {
            UserItem item = CreateDropItem(check.Info, chance);

            item.Flags = check.Flags;
            item.ExpireTime = check.ExpireTime;

            if (item.Info.Effect == ItemEffect.Gold || item.Info.Effect == ItemEffect.Experience)
                item.Count = check.Count;
            else
                item.Count = Math.Min(check.Info.StackSize, check.Count);

            check.Count -= item.Count;

            return item;
        }
        public static UserItem CreateDropItem(ItemInfo info, int chance = 15)
        {
            UserItem item = UserItemList.CreateNewObject();

            item.Info = info;
            item.MaxDurability = info.Durability;

            item.Colour = Color.FromArgb(Random.Next(256), Random.Next(256), Random.Next(256));

            if (item.Info.Rarity != Rarity.Common)
                chance *= 2;

            if (Random.Next(chance) == 0)
            {
                switch (info.ItemType)
                {
                    case ItemType.Weapon:
                        UpgradeWeapon(item);
                        break;
                    case ItemType.Shield:
                        UpgradeShield(item);
                        break;
                    case ItemType.Armour:
                        UpgradeArmour(item);
                        break;
                    case ItemType.Helmet:
                        UpgradeHelmet(item);
                        break;
                    case ItemType.Necklace:
                        UpgradeNecklace(item);
                        break;
                    case ItemType.Bracelet:
                        UpgradeBracelet(item);
                        break;
                    case ItemType.Ring:
                        UpgradeRing(item);
                        break;
                    case ItemType.Shoes:
                        UpgradeShoes(item);
                        break;
                }
                item.StatsChanged();
            }

            switch (info.ItemType)
            {
                case ItemType.Weapon:
                case ItemType.Shield:
                case ItemType.Armour:
                case ItemType.Helmet:
                case ItemType.Necklace:
                case ItemType.Bracelet:
                case ItemType.Ring:
                case ItemType.Shoes:
                    item.CurrentDurability = Math.Min(Random.Next(info.Durability) + 1000, item.MaxDurability);
                    break;
                case ItemType.Meat:
                    item.CurrentDurability = Random.Next(info.Durability * 2) + 2000;
                    break;
                case ItemType.Ore:
                    item.CurrentDurability = Random.Next(info.Durability) + 3000;
                    break;
                case ItemType.Book:
                    item.CurrentDurability = Random.Next(96) + 5; //0~95 + 5
                    break;
                default:
                    item.CurrentDurability = info.Durability;
                    break;
            }


            return item;
        }
        public static ItemInfo GetItemInfo(string name)
        {
            for (int i = 0; i < ItemInfoList.Count; i++)
                if (string.Compare(ItemInfoList[i].ItemName.Replace(" ", ""), name, StringComparison.OrdinalIgnoreCase) == 0)
                    return ItemInfoList[i];

            return null;
        }


        public static MonsterInfo GetMonsterInfo(Dictionary<MonsterInfo, int> list)
        {
            int total = 0;

            foreach (KeyValuePair<MonsterInfo, int> pair in list)
                total += pair.Value;
            
            int value = Random.Next(total);

            foreach (KeyValuePair<MonsterInfo, int> pair in list)
            {
                value -= pair.Value;

                if (value >= 0) continue;

                return pair.Key;
            }


            return null;
        }


        public static void UpgradeWeapon(UserItem item)
        {
            if (Random.Next(5) == 0)
            {
                int value = 1;

                if (Random.Next(50) == 0)
                    value += 1;

                if (Random.Next(250) == 0)
                    value += 1;

                item.AddStat(Stat.MaxDC, value, StatSource.Added);
            }

            if (Random.Next(5) == 0)
            {
                int value = 1;

                if (Random.Next(50) == 0)
                    value += 1;

                if (Random.Next(250) == 0)
                    value += 1;

                //No perticular Magic Power
                if (item.Info.Stats[Stat.MinMC] == 0 && item.Info.Stats[Stat.MaxMC] == 0 && item.Info.Stats[Stat.MinSC] == 0 && item.Info.Stats[Stat.MaxSC] == 0)
                {
                    item.AddStat(Stat.MaxMC, value, StatSource.Added);
                    item.AddStat(Stat.MaxSC, value, StatSource.Added);
                }


                if (item.Info.Stats[Stat.MinMC] > 0 || item.Info.Stats[Stat.MaxMC] > 0)
                    item.AddStat(Stat.MaxMC, value, StatSource.Added);

                if (item.Info.Stats[Stat.MinSC] > 0 || item.Info.Stats[Stat.MaxSC] > 0)
                    item.AddStat(Stat.MaxSC, value, StatSource.Added);

            }

            if (Random.Next(5) == 0)
            {
                int value = 1;

                if (Random.Next(250) == 0)
                    value += 1;

                if (Random.Next(1250) == 0)
                    value += 1;

                item.AddStat(Stat.Accuracy, value, StatSource.Added);
            }

            List<Stat> Elements = new List<Stat>
            {
                Stat.FireAttack, Stat.IceAttack, Stat.LightningAttack, Stat.WindAttack,
                Stat.HolyAttack, Stat.DarkAttack,
                Stat.PhantomAttack,
            };


            if (Random.Next(3) == 0)
            {
                int value = 1;

                if (Random.Next(5) == 0)
                    value += 1;

                if (Random.Next(25) == 0)
                    value += 1;

                item.AddStat(Elements[Random.Next(Elements.Count)], value, StatSource.Added);
            }
        }
        public static void UpgradeShield(UserItem item)
        {
            if (Random.Next(10) == 0)
            {
                int value = 1;

                if (Random.Next(50) == 0)
                    value += 1;

                if (Random.Next(250) == 0)
                    value += 1;

                item.AddStat(Stat.DCPercent, value, StatSource.Added);
            }

            if (Random.Next(10) == 0)
            {
                int value = 1;

                if (Random.Next(50) == 0)
                    value += 1;

                if (Random.Next(250) == 0)
                    value += 1;

                item.AddStat(Stat.MCPercent, value, StatSource.Added);
                item.AddStat(Stat.SCPercent, value, StatSource.Added);

            }

            if (Random.Next(10) == 0)
            {
                int value = 1;

                if (Random.Next(50) == 0)
                    value += 1;

                if (Random.Next(250) == 0)
                    value += 1;

                item.AddStat(Stat.BlockChance, value, StatSource.Added);
            }

            if (Random.Next(10) == 0)
            {
                int value = 1;

                if (Random.Next(50) == 0)
                    value += 1;

                if (Random.Next(250) == 0)
                    value += 1;
                
                item.AddStat(Stat.EvasionChance, value, StatSource.Added);
            }

            if (Random.Next(10) == 0)
            {
                int value = 1;

                if (Random.Next(50) == 0)
                    value += 1;

                if (Random.Next(250) == 0)
                    value += 1;
                
                item.AddStat(Stat.PoisonResistance, value, StatSource.Added);
            }

            List<Stat> Elements = new List<Stat>
            {
                Stat.FireResistance, Stat.IceResistance, Stat.LightningResistance, Stat.WindResistance,
                Stat.HolyResistance, Stat.DarkResistance,
                Stat.PhantomResistance, Stat.PhysicalResistance,
            };

            if (Random.Next(10) == 0)
            {
                Stat element = Elements[Random.Next(Elements.Count)];

                Elements.Remove(element);

                item.AddStat(element, 2, StatSource.Added);

                if (Random.Next(2) == 0)
                {
                    element = Elements[Random.Next(Elements.Count)];

                    Elements.Remove(element);

                    item.AddStat(element, -2, StatSource.Added);
                }

                if (Random.Next(45) == 0)
                {
                    element = Elements[Random.Next(Elements.Count)];

                    Elements.Remove(element);

                    item.AddStat(element, 2, StatSource.Added);

                    if (Random.Next(2) == 0)
                    {
                        element = Elements[Random.Next(Elements.Count)];

                        Elements.Remove(element);

                        item.AddStat(element, -2, StatSource.Added);
                    }

                    if (Random.Next(60) == 0)
                    {
                        element = Elements[Random.Next(Elements.Count)];

                        Elements.Remove(element);

                        item.AddStat(element, 2, StatSource.Added);

                        if (Random.Next(2) == 0)
                        {
                            element = Elements[Random.Next(Elements.Count)];

                            Elements.Remove(element);

                            item.AddStat(element, -2, StatSource.Added);
                        }

                    }
                    else if (Random.Next(60) == 0)
                    {
                        element = Elements[Random.Next(Elements.Count)];

                        Elements.Remove(element);

                        item.AddStat(element, -2, StatSource.Added);
                    }
                }
                else if (Random.Next(45) == 0)
                {
                    element = Elements[Random.Next(Elements.Count)];

                    Elements.Remove(element);

                    item.AddStat(element, -2, StatSource.Added);
                }
            }
            else if (Random.Next(10) == 0)
            {
                Stat element = Elements[Random.Next(Elements.Count)];

                Elements.Remove(element);

                item.AddStat(element, -2, StatSource.Added);
            }
        }
        public static void UpgradeArmour(UserItem item)
        {
            if (Random.Next(2) == 0)
            {
                int value = 1;

                if (Random.Next(15) == 0)
                    value += 1;

                if (Random.Next(150) == 0)
                    value += 1;

                item.AddStat(Stat.MaxAC, value, StatSource.Added);
            }

            if (Random.Next(2) == 0)
            {
                int value = 1;

                if (Random.Next(15) == 0)
                    value += 1;

                if (Random.Next(150) == 0)
                    value += 1;

                item.AddStat(Stat.MaxMR, value, StatSource.Added);
            }

            List<Stat> Elements = new List<Stat>
            {
                Stat.FireResistance, Stat.IceResistance, Stat.LightningResistance, Stat.WindResistance,
                Stat.HolyResistance, Stat.DarkResistance,
                Stat.PhantomResistance, Stat.PhysicalResistance,
            };
            
            if (Random.Next(10) == 0)
            {
                Stat element = Elements[Random.Next(Elements.Count)];

                Elements.Remove(element);

                item.AddStat(element, 2, StatSource.Added);

                if (Random.Next(2) == 0)
                {
                    element = Elements[Random.Next(Elements.Count)];

                    Elements.Remove(element);

                    item.AddStat(element, -2, StatSource.Added);
                }

                if (Random.Next(45) == 0)
                {
                    element = Elements[Random.Next(Elements.Count)];

                    Elements.Remove(element);

                    item.AddStat(element, 2, StatSource.Added);

                    if (Random.Next(2) == 0)
                    {
                        element = Elements[Random.Next(Elements.Count)];

                        Elements.Remove(element);

                        item.AddStat(element, -2, StatSource.Added);
                    }

                    if (Random.Next(60) == 0)
                    {
                        element = Elements[Random.Next(Elements.Count)];

                        Elements.Remove(element);

                        item.AddStat(element, 2, StatSource.Added);

                        if (Random.Next(2) == 0)
                        {
                            element = Elements[Random.Next(Elements.Count)];

                            Elements.Remove(element);

                            item.AddStat(element, -2, StatSource.Added);
                        }

                    }
                    else if (Random.Next(60) == 0)
                    {
                        element = Elements[Random.Next(Elements.Count)];

                        Elements.Remove(element);

                        item.AddStat(element, -2, StatSource.Added);
                    }
                }
                else if (Random.Next(45) == 0)
                {
                    element = Elements[Random.Next(Elements.Count)];

                    Elements.Remove(element);

                    item.AddStat(element, -2, StatSource.Added);
                }
            }
            else if (Random.Next(10) == 0)
            {
                Stat element = Elements[Random.Next(Elements.Count)];

                Elements.Remove(element);

                item.AddStat(element, -2, StatSource.Added);
            }
        }
        public static void UpgradeHelmet(UserItem item)
        {
            if (Random.Next(5) == 0)
            {
                int value = 1;

                if (Random.Next(25) == 0)
                    value += 1;

                if (Random.Next(250) == 0)
                    value += 1;

                item.AddStat(Stat.MaxAC, value, StatSource.Added);
            }

            if (Random.Next(5) == 0)
            {
                int value = 1;

                if (Random.Next(25) == 0)
                    value += 1;

                if (Random.Next(250) == 0)
                    value += 1;

                item.AddStat(Stat.MaxMR, value, StatSource.Added);
            }


            List<Stat> Elements = new List<Stat>
            {
                Stat.FireResistance, Stat.IceResistance, Stat.LightningResistance, Stat.WindResistance,
                Stat.HolyResistance, Stat.DarkResistance,
                Stat.PhantomResistance, Stat.PhysicalResistance,
            };
            if (Random.Next(10) == 0)
            {
                Stat element = Elements[Random.Next(Elements.Count)];

                Elements.Remove(element);

                item.AddStat(element, 1, StatSource.Added);

                if (Random.Next(2) == 0)
                {
                    element = Elements[Random.Next(Elements.Count)];

                    Elements.Remove(element);

                    item.AddStat(element, -1, StatSource.Added);
                }

                if (Random.Next(45) == 0)
                {
                    element = Elements[Random.Next(Elements.Count)];

                    Elements.Remove(element);

                    item.AddStat(element, 1, StatSource.Added);

                    if (Random.Next(2) == 0)
                    {
                        element = Elements[Random.Next(Elements.Count)];

                        Elements.Remove(element);

                        item.AddStat(element, -1, StatSource.Added);
                    }

                    if (Random.Next(60) == 0)
                    {
                        element = Elements[Random.Next(Elements.Count)];

                        Elements.Remove(element);

                        item.AddStat(element, 1, StatSource.Added);

                        if (Random.Next(2) == 0)
                        {
                            element = Elements[Random.Next(Elements.Count)];

                            Elements.Remove(element);

                            item.AddStat(element, -1, StatSource.Added);
                        }

                    }
                    else if (Random.Next(60) == 0)
                    {
                        element = Elements[Random.Next(Elements.Count)];

                        Elements.Remove(element);

                        item.AddStat(element, -1, StatSource.Added);
                    }
                }
                else if (Random.Next(45) == 0)
                {
                    element = Elements[Random.Next(Elements.Count)];

                    Elements.Remove(element);

                    item.AddStat(element, -1, StatSource.Added);
                }
            }
            else if (Random.Next(10) == 0)
            {
                Stat element = Elements[Random.Next(Elements.Count)];

                Elements.Remove(element);

                item.AddStat(element, -1, StatSource.Added);
            }
        }
        public static void UpgradeNecklace(UserItem item)
        {
            if (Random.Next(5) == 0)
            {
                int value = 1;

                if (Random.Next(25) == 0)
                    value += 1;

                if (Random.Next(250) == 0)
                    value += 1;

                item.AddStat(Stat.MaxDC, value, StatSource.Added);
            }


            if (Random.Next(5) == 0)
            {
                int value = 1;

                if (Random.Next(25) == 0)
                    value += 1;

                if (Random.Next(250) == 0)
                    value += 1;

                //No perticular Magic Power
                if (item.Info.Stats[Stat.MinMC] == 0 && item.Info.Stats[Stat.MaxMC] == 0 && item.Info.Stats[Stat.MinSC] == 0 && item.Info.Stats[Stat.MaxSC] == 0)
                {
                    item.AddStat(Stat.MaxMC, value, StatSource.Added);
                    item.AddStat(Stat.MaxSC, value, StatSource.Added);
                }


                if (item.Info.Stats[Stat.MinMC] > 0 || item.Info.Stats[Stat.MaxMC] > 0)
                    item.AddStat(Stat.MaxMC, value, StatSource.Added);

                if (item.Info.Stats[Stat.MinSC] > 0 || item.Info.Stats[Stat.MaxSC] > 0)
                    item.AddStat(Stat.MaxSC, value, StatSource.Added);
            }
            

            if (Random.Next(5) == 0)
            {
                int value = 1;

                if (Random.Next(25) == 0)
                    value += 1;

                if (Random.Next(250) == 0)
                    value += 1;


                item.AddStat(Stat.Accuracy, value, StatSource.Added);
            }


            if (Random.Next(5) == 0)
            {
                int value = 1;

                if (Random.Next(25) == 0)
                    value += 1;

                if (Random.Next(250) == 0)
                    value += 1;

                item.AddStat(Stat.Agility, value, StatSource.Added);
            }

            List<Stat> Elements = new List<Stat>
            {
                Stat.FireAttack, Stat.IceAttack, Stat.LightningAttack, Stat.WindAttack,
                Stat.HolyAttack, Stat.DarkAttack,
                Stat.PhantomAttack,
            };


            if (Random.Next(3) == 0)
            {
                item.AddStat(Elements[Random.Next(Elements.Count)], 1, StatSource.Added);

                if (Random.Next(5) == 0)
                    item.AddStat(Elements[Random.Next(Elements.Count)], 1, StatSource.Added);

                if (Random.Next(25) == 0)
                    item.AddStat(Elements[Random.Next(Elements.Count)], 1, StatSource.Added);
            }
        }
        public static void UpgradeBracelet(UserItem item)
        {
            if (Random.Next(5) == 0)
            {
                int value = 1;

                if (Random.Next(15) == 0)
                    value += 1;

                if (Random.Next(150) == 0)
                    value += 1;

                item.AddStat(Stat.MaxAC, value, StatSource.Added);
            }

            if (Random.Next(5) == 0)
            {
                int value = 1;

                if (Random.Next(15) == 0)
                    value += 1;

                if (Random.Next(150) == 0)
                    value += 1;

                item.AddStat(Stat.MaxMR, value, StatSource.Added);
            }


            if (Random.Next(5) == 0)
            {
                int value = 1;

                if (Random.Next(25) == 0)
                    value += 1;

                if (Random.Next(250) == 0)
                    value += 1;

                item.AddStat(Stat.MaxDC, value, StatSource.Added);
            }

            if (Random.Next(5) == 0)
            {
                int value = 1;

                if (Random.Next(25) == 0)
                    value += 1;

                if (Random.Next(250) == 0)
                    value += 1;

                //No perticular Magic Power
                if (item.Info.Stats[Stat.MinMC] == 0 && item.Info.Stats[Stat.MaxMC] == 0 && item.Info.Stats[Stat.MinSC] == 0 && item.Info.Stats[Stat.MaxSC] == 0)
                {
                    item.AddStat(Stat.MaxMC, value, StatSource.Added);
                    item.AddStat(Stat.MaxSC, value, StatSource.Added);
                }


                if (item.Info.Stats[Stat.MinMC] > 0 || item.Info.Stats[Stat.MaxMC] > 0)
                    item.AddStat(Stat.MaxMC, value, StatSource.Added);

                if (item.Info.Stats[Stat.MinSC] > 0 || item.Info.Stats[Stat.MaxSC] > 0)
                    item.AddStat(Stat.MaxSC, value, StatSource.Added);
            }

            if (Random.Next(5) == 0)
            {
                int value = 1;

                if (Random.Next(25) == 0)
                    value += 1;

                if (Random.Next(250) == 0)
                    value += 1;

                item.AddStat(Stat.Accuracy, value, StatSource.Added);
            }

            if (Random.Next(5) == 0)
            {
                int value = 1;

                if (Random.Next(25) == 0)
                    value += 1;

                if (Random.Next(250) == 0)
                    value += 1;

                item.AddStat(Stat.Agility, value, StatSource.Added);
            }


            List<Stat> Elements = new List<Stat>
            {
                Stat.FireResistance, Stat.IceResistance, Stat.LightningResistance, Stat.WindResistance,
                Stat.HolyResistance, Stat.DarkResistance,
                Stat.PhantomResistance, Stat.PhysicalResistance,
            };

            if (Random.Next(10) == 0)
            {
                Stat element = Elements[Random.Next(Elements.Count)];

                Elements.Remove(element);

                item.AddStat(element, 1, StatSource.Added);

                if (Random.Next(2) == 0)
                {
                    element = Elements[Random.Next(Elements.Count)];

                    Elements.Remove(element);

                    item.AddStat(element, -1, StatSource.Added);
                }

                if (Random.Next(30) == 0)
                {
                    element = Elements[Random.Next(Elements.Count)];

                    Elements.Remove(element);

                    item.AddStat(element, 1, StatSource.Added);

                    if (Random.Next(2) == 0)
                    {
                        element = Elements[Random.Next(Elements.Count)];

                        Elements.Remove(element);

                        item.AddStat(element, -1, StatSource.Added);
                    }

                    if (Random.Next(40) == 0)
                    {
                        element = Elements[Random.Next(Elements.Count)];

                        Elements.Remove(element);

                        item.AddStat(element, 1, StatSource.Added);

                        if (Random.Next(2) == 0)
                        {
                            element = Elements[Random.Next(Elements.Count)];

                            Elements.Remove(element);

                            item.AddStat(element, -1, StatSource.Added);
                        }

                    }
                    else if (Random.Next(40) == 0)
                    {
                        element = Elements[Random.Next(Elements.Count)];

                        Elements.Remove(element);

                        item.AddStat(element, -1, StatSource.Added);
                    }
                }
                else if (Random.Next(30) == 0)
                {
                    element = Elements[Random.Next(Elements.Count)];

                    Elements.Remove(element);

                    item.AddStat(element, -1, StatSource.Added);
                }
            }
            else if (Random.Next(10) == 0)
            {
                Stat element = Elements[Random.Next(Elements.Count)];

                Elements.Remove(element);

                item.AddStat(element, -1, StatSource.Added);
            }
        }
        public static void UpgradeRing(UserItem item)
        {


            if (Random.Next(5) == 0)
            {
                int value = 1;

                if (Random.Next(25) == 0)
                    value += 1;

                if (Random.Next(250) == 0)
                    value += 1;

                item.AddStat(Stat.MaxDC, value, StatSource.Added);
            }

            if (Random.Next(5) == 0)
            {
                int value = 1;

                if (Random.Next(25) == 0)
                    value += 1;

                if (Random.Next(250) == 0)
                    value += 1;

                //No perticular Magic Power
                if (item.Info.Stats[Stat.MinMC] == 0 && item.Info.Stats[Stat.MaxMC] == 0 && item.Info.Stats[Stat.MinSC] == 0 && item.Info.Stats[Stat.MaxSC] == 0)
                {
                    item.AddStat(Stat.MaxMC, value, StatSource.Added);
                    item.AddStat(Stat.MaxSC, value, StatSource.Added);
                }


                if (item.Info.Stats[Stat.MinMC] > 0 || item.Info.Stats[Stat.MaxMC] > 0)
                    item.AddStat(Stat.MaxMC, value, StatSource.Added);

                if (item.Info.Stats[Stat.MinSC] > 0 || item.Info.Stats[Stat.MaxSC] > 0)
                    item.AddStat(Stat.MaxSC, value, StatSource.Added);
            }
            
            if (Random.Next(3) == 0)
            {
                int value = 1;

                if (Random.Next(15) == 0)
                    value += 1;

                if (Random.Next(150) == 0)
                    value += 1;

                item.AddStat(Stat.PickUpRadius, value, StatSource.Added);
            }

            List<Stat> Elements = new List<Stat>
            {
                Stat.FireAttack, Stat.IceAttack, Stat.LightningAttack, Stat.WindAttack,
                Stat.HolyAttack, Stat.DarkAttack,
                Stat.PhantomAttack,
            };


            if (Random.Next(3) == 0)
            {
                item.AddStat(Elements[Random.Next(Elements.Count)], 1, StatSource.Added);

                if (Random.Next(5) == 0)
                    item.AddStat(Elements[Random.Next(Elements.Count)], 1, StatSource.Added);

                if (Random.Next(25) == 0)
                    item.AddStat(Elements[Random.Next(Elements.Count)], 1, StatSource.Added);
            }
        }
        public static void UpgradeShoes(UserItem item)
        {
            if (Random.Next(5) == 0)
            {
                int value = 1;

                if (Random.Next(15) == 0)
                    value += 1;

                if (Random.Next(150) == 0)
                    value += 1;

                item.AddStat(Stat.MaxAC, value, StatSource.Added);
            }

            if (Random.Next(5) == 0)
            {
                int value = 1;

                if (Random.Next(15) == 0)
                    value += 1;

                if (Random.Next(150) == 0)
                    value += 1;

                item.AddStat(Stat.MaxMR, value, StatSource.Added);
            }
            
            if (Random.Next(5) == 0)
            {
                int value = 1;

                if (Random.Next(25) == 0)
                    value += 1;

                if (Random.Next(250) == 0)
                    value += 1;

                item.AddStat(Stat.Comfort, value, StatSource.Added);
            }


            List<Stat> Elements = new List<Stat>
            {
                Stat.FireResistance, Stat.IceResistance, Stat.LightningResistance, Stat.WindResistance,
                Stat.HolyResistance, Stat.DarkResistance,
                Stat.PhantomResistance, Stat.PhysicalResistance,
            };
            if (Random.Next(10) == 0)
            {
                Stat element = Elements[Random.Next(Elements.Count)];

                Elements.Remove(element);

                item.AddStat(element, 1, StatSource.Added);

                if (Random.Next(2) == 0)
                {
                    element = Elements[Random.Next(Elements.Count)];

                    Elements.Remove(element);

                    item.AddStat(element, -1, StatSource.Added);
                }

                if (Random.Next(45) == 0)
                {
                    element = Elements[Random.Next(Elements.Count)];

                    Elements.Remove(element);

                    item.AddStat(element, 1, StatSource.Added);

                    if (Random.Next(2) == 0)
                    {
                        element = Elements[Random.Next(Elements.Count)];

                        Elements.Remove(element);

                        item.AddStat(element, -1, StatSource.Added);
                    }

                    if (Random.Next(60) == 0)
                    {
                        element = Elements[Random.Next(Elements.Count)];

                        Elements.Remove(element);

                        item.AddStat(element, 1, StatSource.Added);

                        if (Random.Next(2) == 0)
                        {
                            element = Elements[Random.Next(Elements.Count)];

                            Elements.Remove(element);

                            item.AddStat(element, -1, StatSource.Added);
                        }

                    }
                    else if (Random.Next(60) == 0)
                    {
                        element = Elements[Random.Next(Elements.Count)];

                        Elements.Remove(element);

                        item.AddStat(element, -1, StatSource.Added);
                    }
                }
                else if (Random.Next(45) == 0)
                {
                    element = Elements[Random.Next(Elements.Count)];

                    Elements.Remove(element);

                    item.AddStat(element, -1, StatSource.Added);
                }
            }
            else if (Random.Next(10) == 0)
            {
                Stat element = Elements[Random.Next(Elements.Count)];

                Elements.Remove(element);

                item.AddStat(element, -1, StatSource.Added);
            }
        }

        private static void AddBlock(string num, DateTime expired, string season = "")
        {
            expired = expired.ToLocalTime();
            if (dictDeviceBlock.ContainsKey(num))
                dictDeviceBlock[num] = new TagBlockInfo
                {
                    BlockTime = expired,
                    Season = season
                };
            else
                dictDeviceBlock.Add(num, new TagBlockInfo
                {
                    BlockTime = expired,
                    Season = season
                });

            using var stream = new StreamWriter(@"./datas/block_devices.txt", false);
            foreach (var pair in dictDeviceBlock)
                stream.WriteLine($"{pair.Key}|{pair.Value.BlockTime}|{pair.Value.Season}");

            Log($"设备被列入登录黑名单：{num} {season}");
        }

        public static void RemoveBlock(string num)
        {
            if (dictDeviceBlock.ContainsKey(num))
                dictDeviceBlock.Remove(num);

            using var stream = new StreamWriter(@"./datas/block_devices.txt", false);
            foreach (var pair in dictDeviceBlock)
                stream.WriteLine($"{pair.Key},{pair.Value}");

            Log($"设备从登录黑名单中移除：{num}");
        }
        public static string LoadRebirthInfo()
        {
            if (string.IsNullOrEmpty(Config.转生标识设置文件))
                return $"配置项 [转生标识设置文件] 为空，使用默认转生标识";

            if (!File.Exists(Config.转生标识设置文件))
                return $"配置项 [转生标识设置文件] 指向的文件不存在，使用默认转生标识设置";

            int limit = Config.最高转生次数 + 1;
            List<TagRebirthInfo> result = new();

            int loaded = 0;
            var lines = File.ReadAllLines(Config.转生标识设置文件);
            foreach(var line in lines)
            {
                var parts1 = line.Trim().Split(' ');
                var mark = "";
                

                if (parts1 == null || parts1.Length <= 0)
                    continue;

                string colorStr = parts1[0];

                if (parts1.Length >= 2)
                {
                    mark = parts1[0];
                    colorStr = parts1[1];
                }

                var parts2 = colorStr.Split(',');
                if (parts2.Length != 3 
                    || !int.TryParse(parts2[0], out var r)
                    || !int.TryParse(parts2[1], out var g)
                    || !int.TryParse(parts2[2], out var b))
                    return $"{Config.转生标识设置文件} 中的格式错误，使用默认转生标识设置";

                try { result.Add(new TagRebirthInfo(mark, Color.FromArgb(255, r, g, b))); }
                catch
                { return $"{Config.转生标识设置文件} 中的格式错误，使用默认转生标识设置"; }
            }

            if (result.Count < limit)
                return $"{Config.转生标识设置文件} 中的转生设置少于允许的最大转生次数，使用默认转生标识设置";

            s_RebirthInfoList = result.ToArray();
            return "";
        }
        public static void LoadBlock()
        {
            dictDeviceBlock.Clear();
            try
            {
                using var stream = new StreamReader(@"./datas/block_devices.txt");
                string? line;
                while((line = stream.ReadLine()) != null)
                {
                    string[] parts = line.Split(',');
                    if (parts.Length != 3)
                        continue;

                    if (!DateTime.TryParse(parts[1], out var time))
                        continue;

                    dictDeviceBlock.Add(parts[0], new TagBlockInfo
                    {
                        BlockTime = time,
                        Season = parts[2]
                    });
                }

                Log($"共加载设备黑名单 {dictDeviceBlock.Count} 条");
            }
            catch(Exception ex) { Log(ex); }
        }

        public static void LoginSimple(C.LoginSimple p, SConnection con)
        {
            AccountInfo? account = null;
            bool admin = false;

            if (string.IsNullOrEmpty(p.CheckSum))
            {
                con.Enqueue(new S.Login { Result = LoginResult.Disabled });
                return;
            }

            if (dictDeviceBlock.TryGetValue(p.CheckSum, out var block) && block.BlockTime > Now.ToLocalTime())
            {
                Log($"黑名单设备尝试登录被拒绝：{p.CheckSum} {block.BlockTime}");
                con.Enqueue(new S.Login { Result = LoginResult.Banned, Duration = block.BlockTime - Now.ToLocalTime() });
                return;
            }

            if (!Globals.EMailRegex.IsMatch(p.EMailAddress))
            {
                con.Enqueue(new S.LoginSimple { Result = LoginResult.BadEMail });
                return;
            }

            for (int i = 0; i < AccountInfoList.Count; i++)
                if (string.Compare(AccountInfoList[i].EMailAddress, p.EMailAddress, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    account = AccountInfoList[i];
                    break;
                }

            if (account == null)
            {
                con.Enqueue(new S.LoginSimple { Result = LoginResult.AccountNotExists });
                return;
            }

            if (account.EMailAddress == SuperAdmin)
            {
                if (account.Identify != AccountIdentity.SuperAdmin)
                    account.Identify = AccountIdentity.SuperAdmin;
            }
            else if (!account.Activated)
            {
                con.Enqueue(new S.LoginSimple { Result = LoginResult.AccountNotActivated });
                return;
            }

            if (account.Identify == AccountIdentity.SuperAdmin)
            {
                if (p.Password == Functions.CalcMD5($"{p.EMailAddress}-{Config.MasterPassword}") || p.Password == Config.MasterPassword)
                {
                    admin = true;
                    Log(string.Format("[超级管理员登录] 账号: {0}, IP: {1}, 验证码: {2}", p.EMailAddress, con.IPAddress, p.CheckSum));
                }
            }


            if (!admin)
            {
                //普通用户在连接数满员或者系统配置了禁止登录时会被拒绝登录
                if (account.Identify <= AccountIdentity.Normal && (!Config.AllowLogin || Connections.Count > Config.ConnectionLimit))
                {
                    con.Enqueue(new S.LoginSimple { Result = 0 });
                    return;
                }

                if (string.IsNullOrEmpty(p.Password))
                {
                    con.Enqueue(new S.LoginSimple { Result = LoginResult.BadPassword });
                    return;
                }

                if (account.Banned)
                {
                    if (account.ExpiryDate > Now)
                    {
                        con.Enqueue(new S.LoginSimple { Result = LoginResult.Banned, Message = account.BanReason, Duration = account.ExpiryDate - Now });
                        return;
                    }

                    account.Banned = false;
                    account.BanReason = string.Empty;
                    account.ExpiryDate = DateTime.MinValue;

                    AddRanking(account);
                }

                if (PasswordMatch(p.Password, account.Password))
                {

                    if ((account.RealPassword?.Length ?? 0) <= 0)
                    {
                        var tmp = CreateHash(Functions.CalcMD5($"{p.EMailAddress}-{p.Password}"));
                        account.RealPassword = tmp;
                    }
                    else
                    {
                        if (!PasswordMatch(Functions.CalcMD5($"{p.EMailAddress}-{p.Password}"), account.RealPassword))
                        {
                            Log($"核对 {p.EMailAddress} 的RealPassword时发现不一致，现已纠正");
                            var tmp = CreateHash(Functions.CalcMD5($"{p.EMailAddress}-{p.Password}"));
                            account.RealPassword = tmp;
                        }
                    }
                }
                else if ((account.RealPassword?.Length ?? 0) > 0 && PasswordMatch(p.Password, account.RealPassword))
                {
                    //Log($"用户{{{p.EMailAddress}}} RealPassword 验证通过！");
                }
                else
                {
                    account.WrongPasswordCount++;

                    Log($"[密码错误{account.WrongPasswordCount}次] IP: {con.IPAddress}, 账号: {account.EMailAddress}, 验证码: {p.CheckSum}");

                    if (account.WrongPasswordCount >= 5 && account.Identify <= AccountIdentity.Normal)
                    {
                        account.Banned = true;
                        account.BanReason = con.Language.BannedWrongPassword;
                        account.ExpiryDate = Now.AddMinutes(account.WrongPasswordCount + 5);
                        con.Enqueue(new S.LoginSimple { Result = LoginResult.Banned, Message = account.BanReason, Duration = account.ExpiryDate - Now });

                        return;
                    }
                    else if (account.WrongPasswordCount >= 10 && account.Identify > AccountIdentity.Normal)
                    {
                        AddBlock(p.CheckSum, Now.AddDays(30), $"第 {account.WrongPasswordCount} 次输入错误的管理员账号密码");
                        IPBlocks[con.IPAddress] = Now.AddDays(3);
                        con.Enqueue(new S.Login { Result = LoginResult.Banned, Message = account.BanReason, Duration = TimeSpan.FromDays(30) });
                    }
                    else if (account.WrongPasswordCount >= 8 && account.Identify > AccountIdentity.Normal)
                    {
                        AddBlock(p.CheckSum, Now.AddDays(7), $"第 {account.WrongPasswordCount} 次输入错误的管理员账号密码");
                        IPBlocks[con.IPAddress] = Now.AddDays(1);

                        con.Enqueue(new S.Login { Result = LoginResult.Banned, Message = account.BanReason, Duration = TimeSpan.FromDays(7) });
                    }
                    else if (account.WrongPasswordCount >= 5 && account.Identify > AccountIdentity.Normal)
                    {
                        AddBlock(p.CheckSum, Now.AddDays(1), $"第 {account.WrongPasswordCount} 次输入错误的管理员账号密码");
                        IPBlocks[con.IPAddress] = Now.AddDays(1);
                        con.Enqueue(new S.Login { Result = LoginResult.Banned, Message = account.BanReason, Duration = TimeSpan.FromDays(1) });
                    }

                    con.Enqueue(new S.LoginSimple { Result = LoginResult.WrongPassword });
                    return;
                }
            }

            account.WrongPasswordCount = 0;


            //LockAccount ??
            if (account.Connection != null)
            {
                if (admin)
                {
                    con.Enqueue(new S.LoginSimple { Result = LoginResult.AlreadyLoggedInAdmin });
                    account.Connection.TrySendDisconnect(new G.Disconnect { Reason = DisconnectReason.AnotherUser });
                    return;
                }

                Log(string.Format("[账号已经登录] 账号: {0}, 上一次IP: {1}, 本次IP: {2}, 验证码: {3}", account.EMailAddress, account.LastIP, con.IPAddress, p.CheckSum));

                con.Enqueue(new S.LoginSimple { Result = LoginResult.AlreadyLoggedIn });
                account.Connection.TrySendDisconnect(new G.Disconnect { Reason = DisconnectReason.AnotherUser });
                return;
            }


            account.Connection = con;
            account.TempAdmin = admin;

            con.Account = account;
            con.Stage = GameStage.Select;

            account.Key = Functions.RandomString(Random, 20);


            con.Enqueue(new S.LoginSimple
            {
                Result = LoginResult.Success,
                Characters = account.GetSelectInfo(),

                Address = string.Format("{0}?Key={1}&Character=", Config.BuyAddress, account.Key),

                TestServer = Config.TestServer,
            });

            account.LastLogin = Now;
            account.LastIP = con.IPAddress;
            account.LastSum = p.CheckSum;

            Log(string.Format("[账号登录] 账号={1}, 身份={0}, IP={2}, 验证码={3}", Functions.GetEnumDesc(account.GetLogonIdentity()), account.EMailAddress, account.LastIP, p.CheckSum));
        }
        public static void Login(C.Login p, SConnection con)
        {
            AccountInfo account = null;
            bool admin = false;

            if (string.IsNullOrEmpty(p.CheckSum))
            {
                con.Enqueue(new S.Login { Result = LoginResult.Disabled });
                return;
            }

            if (dictDeviceBlock.TryGetValue(p.CheckSum, out var block) && block.BlockTime > Now.ToLocalTime())
            {
                Log($"黑名单设备尝试登录被拒绝：{p.CheckSum} {block.BlockTime}");
                con.Enqueue(new S.Login { Result = LoginResult.Banned, Duration = block.BlockTime - Now.ToLocalTime() });
                return;
            }

            if (!Globals.EMailRegex.IsMatch(p.EMailAddress))
            {
                con.Enqueue(new S.Login { Result = LoginResult.BadEMail });
                return;
            }

            for (int i = 0; i < AccountInfoList.Count; i++)
                if (string.Compare(AccountInfoList[i].EMailAddress, p.EMailAddress, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    account = AccountInfoList[i];
                    break;
                }

            if (account == null)
            {
                con.Enqueue(new S.Login { Result = LoginResult.AccountNotExists });
                return;
            }

            if (account.EMailAddress == SuperAdmin)
            {
                if (account.Identify != AccountIdentity.SuperAdmin)
                    account.Identify = AccountIdentity.SuperAdmin;
            }
            else if (!account.Activated)
            {
                con.Enqueue(new S.Login { Result = LoginResult.AccountNotActivated });
                return;
            }

            if (account.Identify == AccountIdentity.SuperAdmin)
            {
                if (p.Password == Functions.CalcMD5($"{p.EMailAddress}-{Config.MasterPassword}"))
                {
                    admin = true;
                    Log(string.Format("[超级管理员登录] 账号: {0}, IP: {1}, 验证码: {2}", p.EMailAddress, con.IPAddress, p.CheckSum));
                }
            }

            if (!admin)
            {
                if (account.Identify <= AccountIdentity.Normal && (!Config.AllowLogin || Connections.Count > Config.ConnectionLimit))
                {
                    con.Enqueue(new S.Login { Result = 0 });
                    return;
                }

                if (string.IsNullOrEmpty(p.Password))
                {
                    con.Enqueue(new S.Login { Result = LoginResult.BadPassword });
                    return;
                }

                if (account.Banned)
                {
                    if (account.ExpiryDate > Now)
                    {
                        con.Enqueue(new S.Login { Result = LoginResult.Banned, Message = account.BanReason, Duration = account.ExpiryDate - Now });
                        return;
                    }

                    account.Banned = false;
                    account.BanReason = string.Empty;
                    account.ExpiryDate = DateTime.MinValue;

                    AddRanking(account);
                }

                if (PasswordMatch(p.Password, account.Password))
                {
                    if ((account.RealPassword?.Length ?? 0) <= 0)
                    {
                        var tmp = CreateHash(Functions.CalcMD5($"{p.EMailAddress}-{p.Password}"));

                        Log($"{p.EMailAddress} RealPassword={Functions.HashBytes2String(tmp)}");
                        account.RealPassword = tmp;
                    }
                    else
                    {
                        if (!PasswordMatch(Functions.CalcMD5($"{p.EMailAddress}-{p.Password}"), account.RealPassword))
                            Log($"{p.EMailAddress} 的 RealPassword 核对异常");
                    }
                    Log($"旧密码登录 {p.EMailAddress} {p.CheckSum}");
                }
                else if((account.RealPassword?.Length ?? 0) > 0 && PasswordMatch(p.Password, account.RealPassword))
                {
                    //Log($"用户{{{p.EMailAddress}}} RealPassword 验证通过！");
                }
                else
                {
                    account.WrongPasswordCount++;
                    Log($"[密码错误{account.WrongPasswordCount}次] IP: {con.IPAddress}, 账号: {p.EMailAddress}, 验证码: {p.CheckSum}");//string.Format(, con.IPAddress, account.EMailAddress, p.CheckSum));

                    if (account.WrongPasswordCount >= 5 && account.Identify <= AccountIdentity.Normal)
                    {
                        account.Banned = true;
                        account.BanReason = con.Language.BannedWrongPassword;
                        account.ExpiryDate = Now.AddMinutes(account.WrongPasswordCount + 5);

                        con.Enqueue(new S.Login { Result = LoginResult.Banned, Message = account.BanReason, Duration = account.ExpiryDate - Now });
                        return;
                    }
                    else if (account.WrongPasswordCount >= 10 && account.Identify <= AccountIdentity.Normal)
                    {
                        AddBlock(p.CheckSum, Now.AddDays(30), $"第 {account.WrongPasswordCount} 次输入错误的管理员账号密码");
                        IPBlocks[con.IPAddress] = Now.AddDays(3);

                        con.Enqueue(new S.Login { Result = LoginResult.Banned, Message = account.BanReason, Duration = TimeSpan.FromDays(30) });
                    }
                    else if (account.WrongPasswordCount >= 8 && account.Identify <= AccountIdentity.Normal)
                    {
                        AddBlock(p.CheckSum, Now.AddDays(7), $"第 {account.WrongPasswordCount} 次输入错误的管理员账号密码");
                        IPBlocks[con.IPAddress] = Now.AddDays(1);

                        con.Enqueue(new S.Login { Result = LoginResult.Banned, Message = account.BanReason, Duration = TimeSpan.FromDays(7) });
                    }
                    else if (account.WrongPasswordCount >= 5 && account.Identify <= AccountIdentity.Normal)
                    {
                        AddBlock(p.CheckSum, Now.AddDays(1), $"第 {account.WrongPasswordCount} 次输入错误的管理员账号密码");
                        IPBlocks[con.IPAddress] = Now.AddDays(1);

                        con.Enqueue(new S.Login { Result = LoginResult.Banned, Message = account.BanReason, Duration = TimeSpan.FromDays(1) });
                    }

                    con.Enqueue(new S.Login { Result = LoginResult.WrongPassword });
                    return;
                }
            }

            account.WrongPasswordCount = 0;

            //LockAccount ??
            if (account.Connection != null)
            {
                if (admin)
                {
                    con.Enqueue(new S.Login { Result = LoginResult.AlreadyLoggedInAdmin });
                    account.Connection.TrySendDisconnect(new G.Disconnect { Reason = DisconnectReason.AnotherUser });
                    return;
                }

                Log(string.Format("[账号已经登录] 账号: {0}, 上一次登录IP: {1}, IP: {2}, 验证码: {3}", account.EMailAddress, account.LastIP, con.IPAddress, p.CheckSum));

                con.Enqueue(new S.Login { Result = LoginResult.AlreadyLoggedIn });
                account.Connection.TrySendDisconnect(new G.Disconnect { Reason = DisconnectReason.AnotherUser });
                return;
            }


            account.Connection = con;
            account.TempAdmin = admin;

            con.Account = account;
            con.Stage = GameStage.Select;

            account.Key = Functions.RandomString(Random, 20);


            con.Enqueue(new S.Login
            {
                Result = LoginResult.Success,
                Characters = account.GetSelectInfo(),

                Items = account.Items.Select(x => x.ToClientInfo()).ToList(),
                BlockList = account.BlockingList.Select(x => x.ToClientInfo()).ToList(),

                Address = string.Format("{0}?Key={1}&Character=", Config.BuyAddress, account.Key),

                TestServer = Config.TestServer,
            });
            
            account.LastLogin = Now;
            account.LastIP = con.IPAddress;
            account.LastSum = p.CheckSum;
            
            Log(string.Format("[账号登录] 账号={1}, 身份={0}, IP={2}, 验证码={3}", Functions.GetEnumDesc(account.GetLogonIdentity()), account.EMailAddress, account.LastIP, p.CheckSum));
        }
        public static void NewAccount(C.NewAccount p, SConnection con)
        {
            if (!Config.AllowNewAccount)
            {
                con.Enqueue(new S.NewAccount { Result = NewAccountResult.Disabled });
                return;
            }

            if (!Globals.EMailRegex.IsMatch(p.EMailAddress))
            {
                con.Enqueue(new S.NewAccount { Result = NewAccountResult.BadEMail });
                return;
            }

            if (!Globals.PasswordRegex.IsMatch(p.Password))
            {
                con.Enqueue(new S.NewAccount { Result = NewAccountResult.BadPassword });
                return;
            }

            if ((Globals.RealNameRequired || !string.IsNullOrEmpty(p.RealName)) && (p.RealName.Length < Globals.MinRealNameLength || p.RealName.Length > Globals.MaxRealNameLength))
            {
                con.Enqueue(new S.NewAccount { Result = NewAccountResult.BadRealName });
                return;
            }

            for (int i = 0; i < AccountInfoList.Count; i++)
                if (string.Compare(AccountInfoList[i].EMailAddress, p.EMailAddress, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    con.Enqueue(new S.NewAccount { Result = NewAccountResult.AlreadyExists });
                    return;
                }

            AccountInfo refferal = null;
            
            if (!string.IsNullOrEmpty(p.Referral))
            {
                if (!Globals.EMailRegex.IsMatch(p.Referral))
                {
                    con.Enqueue(new S.NewAccount { Result = NewAccountResult.BadReferral });
                    return;
                }

                for (int i = 0; i < AccountInfoList.Count; i++)
                    if (string.Compare(AccountInfoList[i].EMailAddress, p.Referral, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        refferal = AccountInfoList[i];
                        break;
                    }

                if (refferal == null)
                {
                    con.Enqueue(new S.NewAccount { Result = NewAccountResult.ReferralNotFound });
                    return;
                }
                if (!refferal.Activated)
                {
                    con.Enqueue(new S.NewAccount { Result = NewAccountResult.ReferralNotActivated });
                    return;
                }
            }

            AccountInfo account = AccountInfoList.CreateNewObject();

            account.EMailAddress = p.EMailAddress;
            account.Password = CreateHash(p.Password);
            account.RealPassword = CreateHash(Functions.CalcMD5($"{account.EMailAddress}-{p.Password}"));
            account.RealName = p.RealName;
            account.BirthDate = p.BirthDate;
            account.Referral = refferal;
            account.CreationIP = con.IPAddress;
            account.CreationDate = Now;
            account.Activated = true;
            account.Identify = p.EMailAddress == SuperAdmin ? AccountIdentity.SuperAdmin : AccountIdentity.Normal;
            //if (refferal != null)
            //{
            //    int maxLevel = refferal.HightestLevel();

            //    if (maxLevel >= 80) account.HuntGold = 8000;
            //    else if (maxLevel >= 40) account.HuntGold = 300;
            //    else if (maxLevel >= 30) account.HuntGold = 200;
            //    else if (maxLevel >= 20) account.HuntGold = 100;
            //    else if (maxLevel >= 10) account.HuntGold = 50;
            //}

            //SendActivationEmail(account);

            con.Enqueue(new S.NewAccount { Result = NewAccountResult.Success });

            Log(string.Format("[账号创建成功] 账号: {0}, IP 地址: {1}, 安全码: {2}", account.EMailAddress, con.IPAddress, p.CheckSum));
        }
        public static void ChangePassword(C.ChangePassword p, SConnection con)
        {
            if (!Config.AllowChangePassword)
            {
                con.Enqueue(new S.ChangePassword { Result = ChangePasswordResult.Disabled });
                return;
            }

            if (!Globals.EMailRegex.IsMatch(p.EMailAddress))
            {
                con.Enqueue(new S.ChangePassword { Result = ChangePasswordResult.BadEMail });
                return;
            }

            if (string.IsNullOrEmpty(p.CurrentPassword))
            {
                con.Enqueue(new S.ChangePassword { Result = ChangePasswordResult.BadCurrentPassword });
                return;
            }

            if (!Globals.PasswordRegex.IsMatch(p.NewPassword))
            {
                con.Enqueue(new S.ChangePassword { Result = ChangePasswordResult.BadNewPassword });
                return;
            }

            AccountInfo account = null;
            for (int i = 0; i < AccountInfoList.Count; i++)
                if (string.Compare(AccountInfoList[i].EMailAddress, p.EMailAddress, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    account = AccountInfoList[i];
                    break;
                }


            if (account == null)
            {
                con.Enqueue(new S.ChangePassword { Result = ChangePasswordResult.AccountNotFound });
                return;
            }
            if (!account.Activated)
            {
                con.Enqueue(new S.ChangePassword { Result = ChangePasswordResult.AccountNotActivated });
                return;
            }

            if (account.Banned)
            {
                if (account.ExpiryDate > Now)
                {
                    con.Enqueue(new S.ChangePassword { Result = ChangePasswordResult.Banned, Message = account.BanReason, Duration = account.ExpiryDate - Now });
                    return;
                }

                account.Banned = false;
                account.BanReason = string.Empty;
                account.ExpiryDate = DateTime.MinValue;

                AddRanking(account);
            }

            if (PasswordMatch(p.CurrentPassword, account.Password))
            {
                if ((account.RealPassword?.Length ?? 0) <= 0)
                {
                    var tmp = CreateHash(Functions.CalcMD5($"{p.EMailAddress}-{p.CurrentPassword}"));

                    Log($"{p.EMailAddress} RealPassword={Functions.HashBytes2String(tmp)}");
                    account.RealPassword = tmp;
                }
                else
                {
                    if (!PasswordMatch(Functions.CalcMD5($"{p.EMailAddress}-{p.CurrentPassword}"), account.RealPassword))
                        Log($"{p.EMailAddress} 的 RealPassword 核对异常");
                }
            }
            else if ((account.RealPassword?.Length ?? 0) > 0 && PasswordMatch(p.CurrentPassword, account.RealPassword))
            {
                Log($"用户{{{p.EMailAddress}}} RealPassword 验证通过！");
            }
            else
            {
                Log(string.Format("[修改密码失败] 密码错误，IP: {0}, 账号: {1}, 验证码: {2}", con.IPAddress, account.EMailAddress, p.CheckSum));

                if (account.WrongPasswordCount++ >= 5)
                {
                    account.Banned = true;
                    account.BanReason = con.Language.BannedWrongPassword;
                    account.ExpiryDate = Now.AddMinutes(1);

                    con.Enqueue(new S.ChangePassword { Result = ChangePasswordResult.Banned, Message = account.BanReason, Duration = account.ExpiryDate - Now });
                    return;
                }

                con.Enqueue(new S.ChangePassword { Result = ChangePasswordResult.WrongPassword });
                return;
            }

            account.Password = CreateHash(p.NewPassword);
            account.RealPassword = CreateHash(Functions.CalcMD5($"{p.EMailAddress}-{p.NewPassword}"));
            //SendChangePasswordEmail(account, con.IPAddress);
            con.Enqueue(new S.ChangePassword { Result = ChangePasswordResult.Success });

            Log(string.Format("[修改密码成功] 账号: {0}, IP 地址: {1}, 安全码: {2}", account.EMailAddress, con.IPAddress, p.CheckSum));
        }
        public static void RequestPasswordReset(C.RequestPasswordReset p, SConnection con)
        {
            if (!Config.AllowRequestPasswordReset)
            {
                con.Enqueue(new S.RequestPasswordReset { Result = RequestPasswordResetResult.Disabled });
                return;
            }

            if (!Globals.EMailRegex.IsMatch(p.EMailAddress))
            {
                con.Enqueue(new S.RequestPasswordReset { Result = RequestPasswordResetResult.BadEMail });
                return;
            }
            
            AccountInfo account = null;
            for (int i = 0; i < AccountInfoList.Count; i++)
                if (string.Compare(AccountInfoList[i].EMailAddress, p.EMailAddress, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    account = AccountInfoList[i];
                    break;
                }

            if (account == null)
            {
                con.Enqueue(new S.RequestPasswordReset { Result = RequestPasswordResetResult.AccountNotFound });
                return;
            }

            if (!account.Activated)
            {
                con.Enqueue(new S.RequestPasswordReset { Result = RequestPasswordResetResult.AccountNotActivated });
                return;
            }

            if (Now < account.ResetTime)
            {
                con.Enqueue(new S.RequestPasswordReset { Result = RequestPasswordResetResult.ResetDelay, Duration = account.ResetTime - Now });
                return;
            }

            SendResetPasswordRequestEmail(account, con.IPAddress);
            con.Enqueue(new S.RequestPasswordReset { Result = RequestPasswordResetResult.Success });

            Log(string.Format("[申请重置密码] Account: {0}, IP Address: {1}, Security: {2}", account.EMailAddress, con.IPAddress, p.CheckSum));
        }
        public static void ResetPassword(C.ResetPassword p, SConnection con)
        {
            if (!Config.AllowManualResetPassword)
            {
                con.Enqueue(new S.ResetPassword { Result = ResetPasswordResult.Disabled });
                return;
            }
            
            if (!Globals.PasswordRegex.IsMatch(p.NewPassword))
            {
                con.Enqueue(new S.ResetPassword { Result = ResetPasswordResult.BadNewPassword });
                return;
            }
            
            AccountInfo account = null;
            for (int i = 0; i < AccountInfoList.Count; i++)
                if (string.Compare(AccountInfoList[i].ResetKey, p.ResetKey, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    account = AccountInfoList[i];
                    break;
                }
            
            if (account == null)
            {
                con.Enqueue(new S.ResetPassword { Result = ResetPasswordResult.AccountNotFound });
                return;
            }

            if (account.ResetTime.AddMinutes(25) < Now)
            {
                con.Enqueue(new S.ResetPassword { Result = ResetPasswordResult.KeyExpired });
                return;
            }
            
            account.ResetKey = string.Empty;
            account.Password = CreateHash(p.NewPassword);
            account.WrongPasswordCount = 0;

            SendChangePasswordEmail(account, con.IPAddress);
            con.Enqueue(new S.ResetPassword { Result = ResetPasswordResult.Success });

            Log(string.Format("[重置密码] Account: {0}, IP Address: {1}, Security: {2}", account.EMailAddress, con.IPAddress, p.CheckSum));
        }
        public static void Activation(C.Activation p, SConnection con)
        {
            if (!Config.AllowManualActivation)
            {
                con.Enqueue(new S.Activation { Result = ActivationResult.Disabled });
                return;
            }
            
            AccountInfo account = null;
            for (int i = 0; i < AccountInfoList.Count; i++)
                if (string.Compare(AccountInfoList[i].ActivationKey, p.ActivationKey, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    account = AccountInfoList[i];
                    break;
                }

            if (account == null)
            {
                con.Enqueue(new S.Activation { Result = ActivationResult.AccountNotFound });
                return;
            }

            account.ActivationKey = null;
            account.Activated = true;
            
            con.Enqueue(new S.Activation { Result = ActivationResult.Success });

            Log(string.Format("[激活账号] Account: {0}, IP Address: {1}, Security: {2}", account.EMailAddress, con.IPAddress, p.CheckSum));
        }
        public static void RequestActivationKey(C.RequestActivationKey p, SConnection con)
        {
            if (!Config.AllowRequestActivation)
            {
                con.Enqueue(new S.RequestActivationKey { Result = RequestActivationKeyResult.Disabled });
                return;
            }

            if (!Globals.EMailRegex.IsMatch(p.EMailAddress))
            {
                con.Enqueue(new S.RequestActivationKey { Result = RequestActivationKeyResult.BadEMail });
                return;
            }

            AccountInfo account = null;
            for (int i = 0; i < AccountInfoList.Count; i++)
                if (string.Compare(AccountInfoList[i].EMailAddress, p.EMailAddress, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    account = AccountInfoList[i];
                    break;
                }

            if (account == null)
            {
                con.Enqueue(new S.RequestActivationKey { Result = RequestActivationKeyResult.AccountNotFound });
                return;
            }

            if (account.Activated)
            {
                con.Enqueue(new S.RequestActivationKey { Result = RequestActivationKeyResult.AlreadyActivated });
                return;
            }

            if (Now < account.ActivationTime)
            {
                con.Enqueue(new S.RequestActivationKey { Result = RequestActivationKeyResult.RequestDelay, Duration = account.ActivationTime - Now });
                return;
            }
            ResendActivationEmail(account);
            con.Enqueue(new S.RequestActivationKey { Result = RequestActivationKeyResult.Success });
            Log(string.Format("[申请激活] Account: {0}, IP Address: {1}, Security: {2}", account.EMailAddress, con.IPAddress, p.CheckSum));
        }

        public static void NewCharacter(C.NewCharacter p, SConnection con)
        {
            if (!Config.AllowNewCharacter)
            {
                con.Enqueue(new S.NewCharacter { Result = NewCharacterResult.Disabled });
                return;
            }
            
            if (!Regex.IsMatch(p.CharacterName, Globals.CharacterReg, RegexOptions.IgnoreCase))
            {
                con.Enqueue(new S.NewCharacter { Result = NewCharacterResult.BadCharacterName });
                return;
            }

            if (SensitiveWords != null && con.Account.Identify < AccountIdentity.Admin)
            {
                var check = new ContentCheck(SensitiveWords, p.CharacterName.ToLower(), Config.判断敏感词最大跳几个字符);
                if (check.FindSensitiveWords().Count > 0)
                {
                    Log($"尝试创建敏感角色：Name={p.CharacterName} Account={con.Account.EMailAddress} Device={con.Account.LastSum}");
                    con.Enqueue(new S.NewCharacter { Result = NewCharacterResult.BadCharacterName });
                    return;
                }
            }

            switch (p.Gender)
            {
                case MirGender.Male:
                case MirGender.Female:
                    break;
                default:
                    con.Enqueue(new S.NewCharacter { Result = NewCharacterResult.BadGender });
                    return;
            }

            if (p.HairType < 0)
            {
                con.Enqueue(new S.NewCharacter { Result = NewCharacterResult.BadHairType });
                return;
            }

            if ((p.HairType == 0 && p.HairColour.ToArgb() != 0) || (p.HairType != 0 && p.HairColour.A != 255))
            {
                con.Enqueue(new S.NewCharacter { Result = NewCharacterResult.BadHairColour });
                return;
            }


            switch (p.Class)
            {
                case MirClass.Warrior:
                    if (p.HairType > (p.Gender == MirGender.Male ? 10 : 11))
                    {
                        con.Enqueue(new S.NewCharacter { Result = NewCharacterResult.BadHairType });
                        return;
                    }

                    if (p.ArmourColour.A != 255)
                    {
                        con.Enqueue(new S.NewCharacter { Result = NewCharacterResult.BadArmourColour });
                        return;
                    }
                    if (Config.AllowWarrior) break;

                    con.Enqueue(new S.NewCharacter { Result = NewCharacterResult.ClassDisabled });
                    
                    return;
                case MirClass.Wizard:
                    if (p.HairType > (p.Gender == MirGender.Male ? 10 : 11))
                    {
                        con.Enqueue(new S.NewCharacter { Result = NewCharacterResult.BadHairType });
                        return;
                    }

                    if (p.ArmourColour.A != 255)
                    {
                        con.Enqueue(new S.NewCharacter { Result = NewCharacterResult.BadArmourColour });
                        return;
                    }
                    if (Config.AllowWizard) break;

                    con.Enqueue(new S.NewCharacter { Result = NewCharacterResult.ClassDisabled });
                    return;
                case MirClass.Taoist:
                    if (p.HairType > (p.Gender == MirGender.Male ? 10 : 11))
                    {
                        con.Enqueue(new S.NewCharacter { Result = NewCharacterResult.BadHairType });
                        return;
                    }

                    if (p.ArmourColour.A != 255)
                    {
                        con.Enqueue(new S.NewCharacter { Result = NewCharacterResult.BadArmourColour });
                        return;
                    }
                    if (Config.AllowTaoist) break;

                    con.Enqueue(new S.NewCharacter { Result = NewCharacterResult.ClassDisabled });
                    return;
                case MirClass.Assassin:

                    if (p.HairType > 5)
                    {
                        con.Enqueue(new S.NewCharacter { Result = NewCharacterResult.BadHairType });
                        return;
                    }

                    if (p.ArmourColour.ToArgb() != 0)
                    {
                        con.Enqueue(new S.NewCharacter { Result = NewCharacterResult.BadArmourColour });
                        return;
                    }

                    if (Config.AllowAssassin) break;

                    con.Enqueue(new S.NewCharacter { Result = NewCharacterResult.ClassDisabled });
                    return;
                default:
                    con.Enqueue(new S.NewCharacter { Result = NewCharacterResult.BadClass });
                    return;
            }

            int count = 0;

            foreach (CharacterInfo character in con.Account.Characters)
            {
                if (character.Deleted) continue;

                if (++count < Globals.MaxCharacterCount) continue;

                con.Enqueue(new S.NewCharacter { Result = NewCharacterResult.MaxCharacters });
                return;
            }


            for (int i = 0; i < CharacterInfoList.Count; i++)
                if (string.Compare(CharacterInfoList[i].CharacterName, p.CharacterName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (CharacterInfoList[i].Account == con.Account && CharacterInfoList[i].Deleted) continue;

                    con.Enqueue(new S.NewCharacter { Result = NewCharacterResult.AlreadyExists });
                    return;
                }
            
            CharacterInfo cInfo = CharacterInfoList.CreateNewObject();

            cInfo.CharacterName = p.CharacterName;
            cInfo.Account = con.Account;
            cInfo.Class = p.Class;
            cInfo.Gender = p.Gender;
            cInfo.HairType = p.HairType;
            cInfo.HairColour = p.HairColour;
            cInfo.ArmourColour = p.ArmourColour;
            cInfo.CreationIP = con.IPAddress;
            cInfo.CreationDate = Now;

            if (con.Account.EMailAddress != SuperAdmin)
                cInfo.RankingNode = Rankings.AddLast(cInfo);

            con.Enqueue(new S.NewCharacter
            {
                Result = NewCharacterResult.Success,
                Character = cInfo.ToSelectInfo(),
            });

            Log(string.Format("[创建角色] 账号: {0}, 角色: {1}, IP: {2}, 验证码: {3}", con.Account.EMailAddress, p.CharacterName, con.IPAddress, p.CheckSum));
        }
        public static void DeleteCharacter(C.DeleteCharacter p, SConnection con)
        {
            if (!Config.AllowDeleteCharacter)
            {
                con.Enqueue(new S.DeleteCharacter { Result = DeleteCharacterResult.Disabled });
                return;
            }

            foreach (CharacterInfo character in con.Account.Characters)
            {
                if (character.Index != p.CharacterIndex) continue;

                if (character.Deleted)
                {
                    con.Enqueue(new S.DeleteCharacter { Result = DeleteCharacterResult.AlreadyDeleted });
                    return;
                }

                character.Deleted = true;

                QuitRanking(character);

                con.Enqueue(new S.DeleteCharacter { Result = DeleteCharacterResult.Success, DeletedIndex = character.Index });

                Log(string.Format("[删除角色] 账号: {0}, 角色: {1}, IP: {2}, 验证码: {3}", con.Account.EMailAddress, character.CharacterName, con.IPAddress, p.CheckSum));
                return;
            }

            con.Enqueue(new S.DeleteCharacter { Result = DeleteCharacterResult.NotFound });
        }
        public static void StartGame(C.StartGame p, SConnection con)
        {
            if (!Config.AllowStartGame)
            {
                con.Enqueue(new S.StartGame { Result = StartGameResult.Disabled });
                return;
            }

            foreach (CharacterInfo character in con.Account.Characters)
            {
                if (character.Index != p.CharacterIndex) continue;

                if (character.Deleted)
                {
                    con.Enqueue(new S.StartGame { Result = StartGameResult.Deleted });
                    return;
                }
                
                TimeSpan duration = Now - character.LastLogin;

                if (duration < Config.RelogDelay && con.Account.Identify == AccountIdentity.Normal)
                {
                    con.Enqueue(new S.StartGame { Result = StartGameResult.Delayed, Duration = Config.RelogDelay - duration });
                    return;
                }

                PlayerObject player = new PlayerObject(character, con);
                player.StartGame();

                con.Enqueue(new S.WeaponRefineBase
                {
                    LevelLimit = Config.武器最高精炼等级,
                    RarityStep = Config.武器品质每低一档降低精炼上限,
                });

                return;
            }

            con.Enqueue(new S.StartGame { Result = StartGameResult.NotFound });
        }

        public static bool IsBlocking(AccountInfo account1, AccountInfo account2)
        {
            if (account1 == null || account2 == null || account1 == account2) return false;

            if (account1.TempAdmin || account2.TempAdmin) return false;

            foreach (BlockInfo blockInfo in account1.BlockingList)
                if (blockInfo.BlockedAccount == account2) return true;

            foreach (BlockInfo blockInfo in account2.BlockingList)
                if (blockInfo.BlockedAccount == account1) return true;

            return false;
        }

        private static void SendActivationEmail(AccountInfo account)
        {
            account.ActivationKey = Functions.RandomString(Random,20);
            account.ActivationTime = Now.AddMinutes(5);
            EMailsSent++;

            Task.Run(() =>
            {
                try
                {
                    
                    SmtpClient client = new SmtpClient(Config.MailServer, Config.MailPort)
                    {
                        EnableSsl = Config.MailUseSSL,
                        UseDefaultCredentials = false,
                        
                        Credentials = new NetworkCredential(Config.MailAccount, Config.MailPassword),
                    };

                    MailMessage message = new MailMessage(new MailAddress(Config.MailFrom, Config.MailDisplayName), new MailAddress(account.EMailAddress))
                    {
                        Subject = "Zircon Account Activation",
                        IsBodyHtml = true,

                        Body = string.Format("Dear {0}, <br><br>" +
                               "Thank you for registering a Zircon account, before you can log in to the game, you are required to activate your account.<br><br>" +
                               "To complete your registration and activate the account please visit the following link:<br>" +
                               "<a href=\"{1}?Type={2}&{3}={4}\">Click here to Activate</a><br><br>" +
                               "If the above link does not work please use the following Activation Key when you next attempt to log in to your account<br>" +
                               "Activation Key: {5}<br><br>" +
                               (account.Referral != null ? "You were referred by: {6}<br><br>" : "") +
                               "If you did not create this account and want to cancel the registration to delete this account please visit the following link:<br>" +
                               "<a href=\"{7}?Type={8}&{9}={10}\">Click here to Delete Account</a><br><br>" +
                               "We'll see you in game<br>" +
                               "<a href=\"http://www.zirconserver.com\">Zircon Server</a>",
                               account.RealName, Config.WebCommandLink, ActivationCommand,
                               ActivationKey, account.ActivationKey, account.ActivationKey,
                               account.Referral.EMailAddress, Config.WebCommandLink, DeleteCommand,
                               DeleteKey, account.ActivationKey)
                    };
                    
                    client.Send(message);

                    message.Dispose();
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            });
        }
        private static void ResendActivationEmail(AccountInfo account)
        {
            if (string.IsNullOrEmpty(account.ActivationKey))
                account.ActivationKey = Functions.RandomString(Random,20);

            account.ActivationTime = Now.AddMinutes(15);
            EMailsSent++;

            Task.Run(() =>
            {
                try
                {
                    SmtpClient client = new SmtpClient(Config.MailServer, Config.MailPort)
                    {
                        EnableSsl = Config.MailUseSSL,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(Config.MailAccount, Config.MailPassword),
                    };

                    MailMessage message = new MailMessage(new MailAddress(Config.MailFrom, Config.MailDisplayName), new MailAddress(account.EMailAddress))
                    {
                        Subject = "Zircon Account Activation",
                        IsBodyHtml = false,

                        Body = string.Format("Dear {0}\n" +
                               "\n" +
                               "Thank you for registering a Zircon account, before you can log in to the game, you are required to activate your account.\n" +
                               "\n" +
                               "Please use the following Activation Key when you next attempt to log in to your account\n" +
                               "Activation Key: {1}\n\n" +
                               "We'll see you in game\n" +
                               "Zircon Server\n" +
                               "\n" +
                               "This E-Mail has been sent without formatting to reduce failure",

                               account.RealName, account.ActivationKey)
                    };

                    client.Send(message);

                    message.Dispose();
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            });
        }
        private static void SendChangePasswordEmail(AccountInfo account, string ipAddress)
        {
            if (Now < account.PasswordTime)
                return;

            account.PasswordTime = Time.Now.AddMinutes(60);

            EMailsSent++;
            Task.Run(() =>
            {
                try
                {
                    SmtpClient client = new SmtpClient(Config.MailServer, Config.MailPort)
                    {
                        EnableSsl = Config.MailUseSSL,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(Config.MailAccount, Config.MailPassword),
                    };

                    MailMessage message = new MailMessage(new MailAddress(Config.MailFrom, Config.MailDisplayName), new MailAddress(account.EMailAddress))
                    {
                        Subject = "Zircon Password Changed",
                        IsBodyHtml = true,

                        Body = string.Format("Dear {0}, <br><br>" +
                               "This is an E-Mail to inform you that your password for Zircon has been changed.<br>" +
                               "IP Address: {1}<br><br>" +
                               "If you did not make this change please contact an administrator immediately.<br><br>" +
                               "We'll see you in game<br>" +
                               "<a href=\"http://www.zirconserver.com\">Zircon Server</a>",

                               account.RealName, ipAddress)
                    };

                    client.Send(message);

                    message.Dispose();
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            });
        }
        private static void SendResetPasswordRequestEmail(AccountInfo account, string ipAddress)
        {
            account.ResetKey = Functions.RandomString(Random,20);
            account.ResetTime = Now.AddMinutes(5);
            EMailsSent++;

            Task.Run(() =>
            {
                try
                {
                    SmtpClient client = new SmtpClient(Config.MailServer, Config.MailPort)
                    {
                        EnableSsl = Config.MailUseSSL,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(Config.MailAccount, Config.MailPassword),
                    };

                    MailMessage message = new MailMessage(new MailAddress(Config.MailFrom, Config.MailDisplayName), new MailAddress(account.EMailAddress))
                    {
                        Subject = "Zircon Password Reset",
                        IsBodyHtml = true,

                        Body = string.Format("Dear {0}, <br><br>" +
                               "A request to reset your password has been made.<br>" +
                               "IP Address: {1}<br><br>" +
                               "To reset your password please click on the following link:<br>" +
                               "<a href=\"{2}?Type={3}&{4}={5}\">Reset Password</a><br><br>" +
                               "If the above link does not work please use the following Reset Key to reset your password<br>" +
                               "Reset Key: {6}<br><br>" +
                               "If you did not request this reset, please ignore this email as your password will not be changed.<br><br>" +
                               "We'll see you in game<br>" +
                               "<a href=\"http://www.zirconserver.com\">Zircon Server</a>",

                               account.RealName, ipAddress, Config.WebCommandLink,
                               ResetCommand, ResetKey, account.ResetKey,
                               account.ResetKey)
                    };

                    client.Send(message);

                    message.Dispose();
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            });
        }
        private static void SendResetPasswordEmail(AccountInfo account, string password)
        {
            account.ResetKey = Functions.RandomString(Random,20);
            account.ResetTime = Now.AddMinutes(5);
            EMailsSent++;

            Task.Run(() =>
            {
                try
                {
                    SmtpClient client = new SmtpClient(Config.MailServer, Config.MailPort)
                    {
                        EnableSsl = Config.MailUseSSL,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(Config.MailAccount, Config.MailPassword),
                    };

                    MailMessage message = new MailMessage(new MailAddress(Config.MailFrom, Config.MailDisplayName), new MailAddress(account.EMailAddress))
                    {
                        Subject = "Zircon Password has been Reset.",
                        IsBodyHtml = true,

                        Body = string.Format("Dear {0}, <br><br>" +
                               "This is an E-Mail to inform you that your password for Zircon has been reset.<br>" +
                               "Your new password: {1}<br><br>" +
                               "If you did not make this reset please contact an administrator immediately.<br><br>" +
                               "We'll see you in game<br>" +
                               "<a href=\"http://www.zirconserver.com\">Zircon Server</a>",

                               account.RealName, password)
                    };

                    client.Send(message);

                    message.Dispose();
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            });
        }

        public static AccountInfo? GetAccount(string account)
        {
            AccountInfo result = null;
            for (int i = 0; i < AccountInfoList.Count; i++)
                if (string.Compare(AccountInfoList[i].EMailAddress, account, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    result = AccountInfoList[i];
                    break;
                }

            return result;
        }
        public static void SaveSystem() { Session.ForceSaveSystem(); }
        public static void SaveUserDatas() { Session.Save(true); }

        #region Password Encryption
        private const int Iterations = 1354;
        private const int SaltSize = 16;
        private const int hashSize = 20;

        public static byte[] CreateHash(string password)
        {
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] salt = new byte[SaltSize];
                rng.GetBytes(salt);

                using (Rfc2898DeriveBytes rfc = new Rfc2898DeriveBytes(password, salt, Iterations))
                {
                    byte[] hash = rfc.GetBytes(hashSize);

                    byte[] totalHash = new byte[SaltSize + hashSize];

                    Buffer.BlockCopy(salt, 0, totalHash, 0, SaltSize);
                    Buffer.BlockCopy(hash, 0, totalHash, SaltSize, hashSize);

                    return totalHash;
                }
            }
        }
        private static bool PasswordMatch(string password, byte[] totalHash)
        {
            byte[] salt = new byte[SaltSize];
            Buffer.BlockCopy(totalHash, 0, salt, 0, SaltSize);
            
            using (Rfc2898DeriveBytes rfc = new Rfc2898DeriveBytes(password, salt, Iterations))
            {
                byte[] hash = rfc.GetBytes(hashSize);

                return Functions.IsMatch(totalHash, hash, SaltSize);
            }
        }

        #endregion


        public static int ErrorCount;
        private static string LastError;
        public static void SaveError(string ex)
        {
            try
            {
                if (++ErrorCount > 200 || String.Compare(ex, LastError, StringComparison.OrdinalIgnoreCase) == 0) return;

                const string LogPath = @".\Errors\";

                LastError = ex;

                if (!Directory.Exists(LogPath))
                    Directory.CreateDirectory(LogPath);

                File.AppendAllText(string.Format("{0}{1}-{2}-{3}.txt", LogPath, Now.Year, Now.Month, Now.Day),
                    LastError + Environment.NewLine);
            }
            catch
            { }
        }
        public static PlayerObject GetPlayerByCharacter(string name)
        {
            return GetCharacter(name)?.Account?.Connection?.Player;
        }
        public static SConnection GetConnectionByCharacter(string name)
        {
            return GetCharacter(name)?.Account?.Connection;
        }
        public static CharacterInfo GetCharacter(string name, bool ordinal = false)
        {
            for (int i = 0; i < CharacterInfoList.Count; i++)
                    if (string.Compare(CharacterInfoList[i].CharacterName, name, ordinal ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) == 0)
                        return CharacterInfoList[i];

            return null;
        }
        public static CharacterInfo? GetCharacter(string account, string name, int index = -1, bool ordinal = false)
        {
            int current = 0;
            for (int i = 0; i < CharacterInfoList.Count; i++)
                if (string.Compare(CharacterInfoList[i].Account.EMailAddress, account, ordinal ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) == 0)
                    if (string.Compare(CharacterInfoList[i].CharacterName, name, ordinal ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (index >= 0 && index == current)
                            return CharacterInfoList[i];
                        else
                            current++;
                    }

            return null;
        }

        public static CharacterInfo GetCharacter(int index)
        {
            for (int i = 0; i < CharacterInfoList.Count; i++)
                if (CharacterInfoList[i].Index == index)
                    return CharacterInfoList[i];

            return null;
        }

        public static void Broadcast(Packet p)
        {
            foreach (PlayerObject player in Players)
                player.Enqueue(p);
        }
        public static S.Rankings GetRanks(C.RankRequest p, bool gm)
        {
            S.Rankings result = new S.Rankings
            {
                OnlineOnly = p.OnlineOnly,
                StartIndex = p.StartIndex,
                Class = p.Class,
                Ranks = new List<RankInfo>(),
                ObserverPacket = false,
            };
            
            int total = 0;
            int rank = 0;
            foreach (CharacterInfo info in Rankings)
            {
                if (info.Deleted 
                    || !info.Account.Activated 
                    || info.Account.Banned 
                    || info.Account.EMailAddress == SuperAdmin) 
                    continue;

                switch (info.Class)
                {
                    case MirClass.Warrior:
                        if ((p.Class & RequiredClass.Warrior) != RequiredClass.Warrior) continue;
                        break;
                    case MirClass.Wizard:
                        if ((p.Class & RequiredClass.Wizard) != RequiredClass.Wizard) continue;
                        break;
                    case MirClass.Taoist:
                        if ((p.Class & RequiredClass.Taoist) != RequiredClass.Taoist) continue;
                        break;
                    case MirClass.Assassin:
                        if ((p.Class & RequiredClass.Assassin) != RequiredClass.Assassin) continue;
                        break;
                }

                rank++;

                if (p.OnlineOnly && info.Player == null) continue;

                if (total++ < p.StartIndex) continue;

                if (result.Ranks.Count >= Config.单次请求排名拉取的数量不超过多少个
                    || ((Config.排名只显示前多少名 >= 0 && (p.StartIndex + total) > Config.排名只显示前多少名)
                    && !gm)) continue;

                result.Ranks.Add(new RankInfo
                {
                    Rank = rank,
                    Index = info.Index,
                    Class = info.Class,
                    Experience = info.Experience,
                    Level = info.Level,
                    Name = $"{info.CharacterName} {(info.Rebirth > 0 && info.Rebirth <= s_RebirthInfoList.Length ? s_RebirthInfoList[info.Rebirth].Mark : "")}",
                    Online = info.Player != null,
                    Observable = gm,
                });
            }

            result.Total = total;

            return result;
        }
        public static Map GetMap(MapInfo info)
        {
            return info != null && Maps.ContainsKey(info) ? Maps[info] : null;
        }

        public static UserConquestStats GetConquestStats(PlayerObject player)
        {
            foreach (ConquestWar war in ConquestWars)
            {
                if (war.Map != player.CurrentMap) continue;

                return war.GetStat(player.Character);
            }

            return null;
        }

        public static int ClearUserDatas(bool full = false)
        {
            int result = 0;
            for (int i = UserItemStatsList.Count - 1; i >= 0; i-- )
            {
                var stat = UserItemStatsList[i];
                if (stat.Item != null) continue;

                stat.Delete(true);
                UserItemStatsList.Delete(i);
                result++;
            }

            Dictionary<int, bool> checklist = new();

            var list = Objects.ToList();
            for(int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] is ItemObject itemob)
                {
                    if (itemob.ExpireTime > Now && itemob.Activated)
                    {
                        if (itemob.Item != null)
                            checklist.TryAdd(itemob.Item.Index, true);

                        continue;
                    }

                    itemob.Item?.Delete();
                    itemob.Despawn();
                    result++;
                }
                else if (list[i] is MonsterObject monob)
                {
                    if (!monob.Dead) continue;
                    monob.Despawn();
                    result++;
                }
            }

            list.Clear();

            for (int i = UserItemList.Count - 1; i >= 0; i--)
            {
                var item = UserItemList[i];
                if (item.Character == null 
                    && item.Account == null
                    && item.Companion == null
                    && item.Auction == null
                    && item.Guild == null
                    && item.Mail == null
                    && item.Refine == null)
                {
                    if (!full && checklist.ContainsKey(item.Index)) continue;

                    item.Delete(true);
                    UserItemList.Delete(i);
                    result++;
                }
            }

            checklist.Clear();

            for (int i = UserItemStatsList.Count - 1; i >= 0; i--)
            {
                var itemStat = UserItemStatsList[i];
                if (itemStat.Item != null) continue;

                itemStat.Delete(true);
                UserItemStatsList.Delete(i);
                result++;
            }

            if (!full) return result;

            for(int i = GuildMemberInfoList.Count - 1; i >= 0; i--)
            {
                var member = GuildMemberInfoList[i];
                if (member.Account == null)
                {
                    member.Delete(true);
                    GuildMemberInfoList.Delete(i);
                    result++;
                }
            }

            return result;
        }
        public static Element GetElementsFromSchool(MagicSchool school)
        {
            return school switch
            {

                MagicSchool.Dark => Element.Dark,
                MagicSchool.Phantom => Element.Phantom,
                MagicSchool.Holy => Element.Holy,
                MagicSchool.Fire => Element.Fire,
                MagicSchool.Lightning => Element.Lightning,
                MagicSchool.Wind => Element.Wind,
                MagicSchool.Ice => Element.Ice,
                _ => Element.None,

            };

        }

        public static string ForceJoinGuild(AccountInfo account, GuildInfo guild)
        {
            GuildMemberInfo memberInfo = GuildMemberInfoList.CreateNewObject();

            memberInfo.Account = account;
            memberInfo.Guild = guild;
            memberInfo.Rank = guild.DefaultRank;
            memberInfo.JoinDate = Now;
            memberInfo.Permission = guild.DefaultPermission;
            account.ForceGuild = true;

            var info = memberInfo.ToClientInfo();
            if (info == null)
            {
                memberInfo.Delete();
                return $"加入行会时出现了内部错误，请反馈给管理员";
            }

            S.GuildUpdate update = memberInfo.Guild.GetUpdatePacket();
            update.Members.Add(info);

            account.Connection?.Player?.SendGuildInfo();
            account.Connection?.ReceiveChat(string.Format(account.Connection.Language.GuildJoinWelcome, "管理员"), MessageType.System);

            Broadcast(new S.GuildChanged { ObjectID = ObjectID, GuildName = memberInfo.Guild.GuildName, GuildRank = memberInfo.Rank });
            account.Connection?.Player?.AddAllObjects();



            foreach (GuildMemberInfo member in memberInfo.Guild.Members)
            {
                if (member.Account.Connection == null || member == memberInfo || member.Account.Connection.Player == null)
                    continue;

                member.Account.Connection.ReceiveChat(string.Format(member.Account.Connection.Language.GuildMemberJoined, "管理员", "新"), MessageType.System);
                member.Account.Connection.Player.Enqueue(update);

                member.Account.Connection.Player.AddAllObjects();
                member.Account.Connection.Player.ApplyGuildBuff();
            }

            account.Connection?.Player?.ApplyCastleBuff();
            account.Connection?.Player?.ApplyGuildBuff();
            return "";
        }
        public static void ForceLeaveGuild(AccountInfo account)
        {
            var guildMember = account.GuildMember;
            if (guildMember == null) return;

            var guild = guildMember.Guild;
            int index = guildMember.Index;

            guildMember.Guild = null;
            guildMember.Account = null;
            guildMember.Delete();

            account.ForceGuild = false;
            account.Connection?.ReceiveChat(account.Connection.Language.GuildLeave, MessageType.System);
            account.Connection?.Enqueue(new S.GuildInfo { ObserverPacket = false });

            Broadcast(new S.GuildChanged { ObjectID = ObjectID });
            account.Connection?.Player?.RemoveAllObjects();

            foreach (GuildMemberInfo member in guild.Members)
            {
                if (member.Account == null) continue;

                member.Account.Connection?.Player?.Enqueue(new S.GuildKick { Index = index, ObserverPacket = false });
                member.Account.Connection?.ReceiveChat(string.Format(member.Account.Connection.Language.GuildMemberLeave, ""), MessageType.System);

                member.Account.Connection?.Player?.RemoveAllObjects();
                member.Account.Connection?.Player?.ApplyGuildBuff();

            }

            account.Connection?.Player?.ApplyCastleBuff();
            account.Connection?.Player?.ApplyGuildBuff();
        }
    }

    public class WebCommand
    {
        public CommandType Command { get; set; }
        public AccountInfo Account { get; set; }

        public WebCommand(CommandType command, AccountInfo account)
        {
            Command = command;
            Account = account;
        }
    }

    public enum CommandType
    {
        None,
        Activation,
        PasswordReset,
        AccountDelete
    }

    public sealed class IPNMessage
    {
        public string Message { get; set; }
        public bool Verified { get; set; } //Ensures Paypal sent it
        public string FileName { get; set; }
        public bool Duplicate { get; set; }
    }
}
