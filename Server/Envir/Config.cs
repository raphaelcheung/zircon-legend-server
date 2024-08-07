using System;
using System.IO;
using System.Security.Cryptography;
using Library;

namespace Server.Envir
{
    [ConfigPath(@"./datas/Server.ini")]
    public class Config
    {
        public Config()
        {
            IPAddress = "127.0.0.1";
            Port = 7000;
            TimeOut = TimeSpan.FromSeconds(20);
            PingDelay = TimeSpan.FromSeconds(2);
            UserCountPort = 3000;
            MaxPacket = 50;
            PacketBanTime = TimeSpan.FromMinutes(5);
            CheckVersion = true;
            VersionPath = "./Zircon.exe";
            DBSaveDelay = TimeSpan.FromMinutes(5);
            MapPath = "./Map/";
            MasterPassword = "REDACTED";
            ReleaseDate = new DateTime(2017, 12, 22, 18, 00, 00, DateTimeKind.Utc);
            TestServer = false;
            StarterGuildName = "Starter Guild";
            EasterEventEnd = new DateTime(2018, 04, 09, 00, 00, 00, DateTimeKind.Utc);
            HalloweenEventEnd = new DateTime(2018, 11, 07, 00, 00, 00, DateTimeKind.Utc);
            ChristmasEventEnd = new DateTime(2019, 01, 03, 00, 00, 00, DateTimeKind.Utc);
            AllowLogin = true;
            AllowNewAccount = true;
            AllowChangePassword = true;
            AllowRequestPasswordReset = true;
            AllowWebResetPassword = true;
            AllowManualResetPassword = true;
            AllowDeleteAccount = true;
            AllowManualActivation = true;
            AllowWebActivation = true;
            AllowRequestActivation = true;
            AllowNewCharacter = true;
            AllowDeleteCharacter = true;
            RelogDelay = TimeSpan.FromSeconds(10);
            AllowWarrior = true;
            AllowWizard = true;
            AllowTaoist = true;
            AllowAssassin = false;
            MailServer = "smtp.gmail.com";
            MailPort = 587;
            MailUseSSL = true;
            MailAccount = @"admin@zirconserver.com";
            MailPassword = @"REDACTED";
            MailFrom = "admin@zirconserver.com";
            MailDisplayName = "Admin";
            WebPrefix = @"http://*:80/Command/";
            WebCommandLink = @"https://www.zirconserver.com/Command";
            ActivationSuccessLink = @"https://www.zirconserver.com/activation-successful/";
            ActivationFailLink = @"https://www.zirconserver.com/activation-unsuccessful/";
            ResetSuccessLink = @"https://www.zirconserver.com/password-reset-successful/";
            ResetFailLink = @"https://www.zirconserver.com/password-reset-unsuccessful/";
            DeleteSuccessLink = @"https://www.zirconserver.com/account-deletetion-successful/";
            DeleteFailLink = @"https://www.zirconserver.com/account-deletetion-unsuccessful/";
            BuyPrefix = @"http://*:80/BuyGameGold/";
            BuyAddress = @"http://145.239.204.13/BuyGameGold";
            IPNPrefix = @"http://*:80/IPN/";
            ReceiverEMail = @"REDACTED";
            MaxViewRange = 18;
            ProcessGameGold = true;
            AllowBuyGammeGold = true;
            ShoutDelay = TimeSpan.FromSeconds(10);
            GlobalDelay = TimeSpan.FromSeconds(60);
            MaxLevel = 10;
            DayCycleCount = 3;
            SkillExp = 3;
            AllowObservation = true;
            BrownDuration = TimeSpan.FromSeconds(60);
            PKPointTickRate = TimeSpan.FromSeconds(60);
            PKPointRate = 50;
            RedPoint = 200;
            PvPCurseDuration = TimeSpan.FromMinutes(60);
            PvPCurseRate = 4;
            AutoReviveDelay = TimeSpan.FromMinutes(10);
            DeadDuration = TimeSpan.FromMinutes(1);
            HarvestDuration = TimeSpan.FromMinutes(5);
            MysteryShipRegionIndex = 0;
            LairRegionIndex = 0;
            DropDuration = TimeSpan.FromMinutes(60);
            DropDistance = 5;
            DropLayers = 5;
            TorchRate = 10;
            SpecialRepairDelay = TimeSpan.FromHours(8);
            MaxLuck = 7;
            MaxCurse = -10;
            CurseRate = 20;
            LuckRate = 10;
            MaxStrength = 5;
            StrengthAddRate = 10;
            StrengthLossRate = 20;
            ExperienceRate  = 0;
            DropRate = 0;
            GoldRate = 0;
            SkillRate = 0;
            CompanionRate = 0;
        }

        [ConfigSection("Network")]
        public static string IPAddress { get; set; }
        public static ushort Port { get; set; } 
        public static TimeSpan TimeOut { get; set; }
        public static TimeSpan PingDelay { get; set; }
        public static ushort UserCountPort { get; set; } 
        public static int MaxPacket { get; set; } 
        public static TimeSpan PacketBanTime { get; set; } 


        [ConfigSection("System")]
        public static bool CheckVersion { get; set; } 
        public static string VersionPath { get; set; } 
        public static TimeSpan DBSaveDelay { get; set; } 
        public static string MapPath { get; set; } 
        public static byte[] ClientHash;
        public static string MasterPassword { get; set; } 
        public static string ClientPath { get; set; }
        public static DateTime ReleaseDate { get; set; } 
        public static bool TestServer { get; set; } 
        public static string StarterGuildName { get; set; } 
        public static DateTime EasterEventEnd { get; set; } 
        public static DateTime HalloweenEventEnd { get; set; } 
        public static DateTime ChristmasEventEnd { get; set; } 


        [ConfigSection("Control")]
        public static bool AllowLogin { get; set; } 
        public static bool AllowNewAccount { get; set; } 
        public static bool AllowChangePassword { get; set; } 

        public static bool AllowRequestPasswordReset { get; set; } 
        public static bool AllowWebResetPassword { get; set; } 
        public static bool AllowManualResetPassword { get; set; }

        public static bool AllowDeleteAccount { get; set; } 

        public static bool AllowManualActivation { get; set; } 
        public static bool AllowWebActivation { get; set; } 
        public static bool AllowRequestActivation { get; set; } 

        public static bool AllowNewCharacter { get; set; } 
        public static bool AllowDeleteCharacter { get; set; } 
        public static bool AllowStartGame { get; set; }
        public static TimeSpan RelogDelay { get; set; } 
        public static bool AllowWarrior { get; set; } 
        public static bool AllowWizard { get; set; } 
        public static bool AllowTaoist { get; set; } 
        public static bool AllowAssassin { get; set; }

        [ConfigSection("Mail")]
        public static string MailServer { get; set; }
        public static int MailPort { get; set; } 
        public static bool MailUseSSL { get; set; } 
        public static string MailAccount { get; set; } 
        public static string MailPassword { get; set; } 
        public static string MailFrom { get; set; } 
        public static string MailDisplayName { get; set; } 

        [ConfigSection("WebServer")]
        public static string WebPrefix { get; set; } 
        public static string WebCommandLink { get; set; } 

        public static string ActivationSuccessLink { get; set; } 
        public static string ActivationFailLink { get; set; } 
        public static string ResetSuccessLink { get; set; } 
        public static string ResetFailLink { get; set; } 
        public static string DeleteSuccessLink { get; set; } 
        public static string DeleteFailLink { get; set; } 

        public static string BuyPrefix { get; set; } 
        public static string BuyAddress { get; set; } 
        public static string IPNPrefix { get; set; } 
        public static string ReceiverEMail { get; set; } 
        public static bool ProcessGameGold { get; set; } 
        public static bool AllowBuyGammeGold { get; set; } 


        [ConfigSection("Players")]
        public static int MaxViewRange { get; set; } 
        public static TimeSpan ShoutDelay { get; set; } 
        public static TimeSpan GlobalDelay { get; set; } 
        public static int MaxLevel { get; set; } 
        public static int DayCycleCount { get; set; }
        public static int SkillExp { get; set; }
        public static bool AllowObservation { get; set; } 
        public static TimeSpan BrownDuration { get; set; } 
        public static int PKPointRate { get; set; }
        public static TimeSpan PKPointTickRate { get; set; }
        public static int RedPoint { get; set; }
        public static TimeSpan PvPCurseDuration { get; set; } 
        public static int PvPCurseRate { get; set; } 
        public static TimeSpan AutoReviveDelay { get; set; } 


        [ConfigSection("Monsters")]
        public static TimeSpan DeadDuration { get; set; }
        public static TimeSpan HarvestDuration { get; set; }
        public static int MysteryShipRegionIndex { get; set; }
        public static int LairRegionIndex { get; set; }

        [ConfigSection("Items")]
        public static TimeSpan DropDuration { get; set; }
        public static int DropDistance { get; set; }
        public static int DropLayers { get; set; }
        public static int TorchRate { get; set; }
        public static TimeSpan SpecialRepairDelay { get; set; }
        public static int MaxLuck { get; set; } 
        public static int MaxCurse { get; set; } 
        public static int CurseRate { get; set; } 
        public static int LuckRate { get; set; }
        public static int MaxStrength { get; set; }
        public static int StrengthAddRate { get; set; }
        public static int StrengthLossRate { get; set; }

        [ConfigSection("Rates")]
        public static int ExperienceRate { get; set; }
        public static int DropRate { get; set; }
        public static int GoldRate { get; set; }
        public static int SkillRate { get; set; }
        public static int CompanionRate { get; set; } 


        public static void LoadVersion()
        {
            try
            {
                if (File.Exists(VersionPath))
                    using (FileStream stream = File.OpenRead(VersionPath))
                    using (MD5 md5 = MD5.Create())
                        ClientHash = md5.ComputeHash(stream);
                else ClientHash = null;
            }
            catch (Exception ex)
            {
                SEnvir.Log(ex.ToString());
            }
        }
    }
}
