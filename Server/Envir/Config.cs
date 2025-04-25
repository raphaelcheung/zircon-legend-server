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
            DBSaveDelay = TimeSpan.FromMinutes(5);
            MapPath = "./Map/";
            MasterPassword = "REDACTED";
            ReleaseDate = new DateTime(2017, 12, 22, 18, 00, 00, DateTimeKind.Utc);
            TestServer = false;
            StarterGuildName = "新人行会";
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
            技能初级阶段基础经验 = 3;
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
            DropDuration = TimeSpan.FromMinutes(5);
            DropDistance = 5;
            DropLayers = 5;
            TorchRate = 10;
            SpecialRepairDelay = TimeSpan.FromHours(4);
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
            技能低等级经验倍率 = 0;
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
        public static bool UseProxy { get; set; } = false;
        public static int ConnectionLimit { get; set; } = 200;


        [ConfigSection("System")]
        public static TimeSpan DBSaveDelay { get; set; } 
        public static string MapPath { get; set; } 
        public static string MasterPassword { get; set; }
        public static string ClientPath { get; set; } = "";
        public static DateTime ReleaseDate { get; set; } 
        public static bool TestServer { get; set; } 
        public static string StarterGuildName { get; set; } 
        public static DateTime EasterEventEnd { get; set; } 
        public static DateTime HalloweenEventEnd { get; set; } 
        public static DateTime ChristmasEventEnd { get; set; }
        public static int UpgradeChunkSize { get; set; } = 512 * 1014;//默认 512KB
        public static string WelcomeWordsFile { get; set; } = "";
        public static int 挖出的黑铁矿最小纯度 { get; set; } = 25;
        public static int 挖出的黑铁矿最大纯度 { get; set; } = 45;
        public static int 排名只显示前多少名 { get; set; } = -1;
        public static TimeSpan 玩家数据备份间隔 { get; set; } = TimeSpan.FromMinutes(30);
        public static int 单次请求排名拉取的数量不超过多少个 { get; set; } = 20;
        public static string 地狱之门关联地图名称 { get; set; } = "赤龙城入口";
        public static string 异界之门关联地图名称 { get; set; } = "神舰入口";
        public static int 数据清理间隔分钟 { get; set; } = 60 * 10;
        public static byte 判断敏感词最大跳几个字符 { get; set; } = 2;

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
        public static int 技能初级阶段基础经验 { get; set; } = 3;
        public static bool AllowObservation { get; set; } 
        public static TimeSpan BrownDuration { get; set; } 
        public static int PKPointRate { get; set; }
        public static TimeSpan PKPointTickRate { get; set; }
        public static int RedPoint { get; set; }
        public static TimeSpan PvPCurseDuration { get; set; } 
        public static int PvPCurseRate { get; set; } 
        public static TimeSpan AutoReviveDelay { get; set; }
        public static int 最高转生次数 { get; set; } = 0;
        public static int 转生基础等级 { get; set; } = 86;
        public static int 技能最高等级 { get; set; } = 6;
        public static string 转生标识设置文件 { get; set; } = "";

        [ConfigSection("Monsters")]
        public static TimeSpan DeadDuration { get; set; }
        public static TimeSpan HarvestDuration { get; set; }
        public static int MysteryShipRegionIndex { get; set; }
        public static int 不掉落低于本价格的普通药水 { get; set; } = 0;
        public static bool DropNothingTypeCommonItem { get; set; } = true;
        public static int DropLowestEquipmentsExcludeWeapon { get; set; } = 0;
        public static int DropLowestWeapon { get; set; } = 0;
        public static string SummonMonsterGrowUpFile { get; set; } = "";
        public static uint 道具呼唤的怪物存活分钟 { get; set; } = 30;
        public static uint 宠物不追击距离玩家多少格以外的敌人 { get; set; } = 10;

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
        public static bool MonsterDropGroupShare { get; set; } = false;
        public static bool CanSeeOthersDropped { get; set; } = false;
        public static int MonsterDropProtectionDuration { get; set; } = 0;
        public static int 武器最高精炼等级 { get; set; } = 20;
        public static int 武器品质每低一档降低精炼上限 { get; set; } = 3;
        public static int 武器重置等待分钟 { get; set; } = 60 * 24;
        public static int 武器精炼最大几率基数 { get; set; } = 90;
        public static int 武器精炼几率基数 { get; set; } = 60;
        public static int 武器重置冷却分钟 { get; set; } = 1440;
        public static bool 武器重置保留五分之一属性 { get; set; } = false;
        public static int 武器重置时每多少点属性保留一点 { get; set; } = 10;

        [ConfigSection("Rates")]
        public static int ExperienceRate { get; set; }
        public static int DropRate { get; set; }
        public static int GoldRate { get; set; }
        public static int 技能低等级经验倍率 { get; set; } = 1;
        public static int CompanionRate { get; set; }
        public static int Boss掉落倍率 { get; set; } = 0;
        public static int 技能高等级经验倍率 { get; set; } = 100;

    }
}
