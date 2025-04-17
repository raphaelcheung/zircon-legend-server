using System;
using Library;

namespace Server.Envir.Translations
{
    [ConfigPath(@".\datas\Translations\ChineseMessages.ini")]
    public class ChineseMessages : StringMessages
    {
        public ChineseMessages()
        {
            BannedWrongPassword = "登录密码错误次数太多。";
            PaymentComplete = "你成功支付 {0} 游戏金。";
            PaymentFailed = "你被扣除 {0} 游戏金。";
            ReferralPaymentComplete = "你的推荐人购买了游戏金，奖励你 {0} 狩猎金。";
            ReferralPaymentFailed = "你的推荐人购买游戏金失败，扣除你 {0} 狩猎金。";
            GameGoldLost = "扣除你 {0} 游戏金。";
            GameGoldRefund = "你的 {0} 游戏金被退款。";
            HuntGoldRefund = "你的 {0} 狩猎金被退款。";
            Welcome = "欢迎来到【皓石传奇三】，技术交流或想体验公益服进QQ群 915941142。";
            WelcomeObserver = "你现在是观察者玩家 {0}, 想停止观察的话，需要重新登录游戏。";
            ObserverChangeFail= "只有在安全区才能变更为观察模式。";
            OnlineCount = "在线玩家: {0}，在线设备：{2}，观察者玩家: {1}";
            ObserverCount = "你当前有 {0} 个观察者玩家。";
            CannotFindPlayer = "找不到玩家: {0}";
            AlreadyBlocked = "{0} 已经放入你的阻止名单。";
            BlockingWhisper = "你已禁止私聊。";
            PlayerBlockingWhisper = "玩家: {0} 已禁止私聊。";
            GlobalDelay = "你还需要等待 {0} 秒才能发公共消息。";
            GlobalLevel = "你的等级需要达到 33 才能公共喊话。";
            ShoutDelay = "你还需要等待 {0} 秒才能喊话。";
            ShoutLevel = "你的等级需要达到 2 才能使用喊话。";
            DiceRoll = "[ROLL] - {0} 摇了 {1} 个 {2} 面骰子.";
            TradingEnabled = "交易启用。";
            TradingDisabled = "交易禁用。";
            WhisperEnabled = "私聊启用。";
            WhisperDisabled = "私聊禁用。";
            GuildInviteEnabled= "已启用公会邀请。";
            GuildInviteDisabled= "已禁止公会邀请。";
            ObserverNotLoggedIn= "你需要先登录才能进行聊天。";
            Poisoned = "你中毒了。";
            MurderedBy= "你被 {0} 杀害了。";
            Curse = "你杀害了 {0}, 霉运将围绕着你...";
            Murdered = "你杀害了 {0}.";
            Protected= "你受到了自卫法的保护。";
            Killed = "你被 {0} 在正当防卫中杀死了。";
            Died = "你在战斗中死亡。";
            GroupRecallEnabled= "队伍召唤已启用。";
            GroupRecallDisabled= "队伍召唤已禁用。";
            NeedLevel = "你的等级要达到 {0} 才能继续。";
            NeedMaxLevel= "你的等级要低于 {0} 才能继续。";
            NeedItem = "你需要一个 '{0}' 才能继续。";
            NeedMonster= "这条路被封闭...";
            ConquestStarted= "{0} 攻城战开始。";
            ConquestFinished= "{0} 攻城战结束。";
            ConquestCapture= "{0} 已攻陷 {1}.";
            ConquestOwner= "{0} 是 {1} 的城主.";
            ConquestLost= "{0} 已经失去 {1}.";
            BossSpawn= "邪恶潜伏在 {0}.";
            HarvestRare= "有宝藏埋藏在 {0} 深处.";
            NetherGateOpen= "通往异界的大门已经开启, {0}, {1}";
            NetherGateClosed= "通往异界的大门已经关闭";
            HarvestNothing= "什么都没发现。";
            HarvestCarry= "负重已满";
            HarvestOwner= "你附近没有尸体。";
            LairGateOpen= "地狱之门已经打开, {0}, {1}";
            LairGateClosed= "地狱之门已经关闭";
            Expired = "你的 {0} 已经过期。";
            CannotTownTeleport= "无法在此地图上进行回城传送。";
            CannotRandomTeleport= "无法在此地图上进行随机传送。";
            ConnotResetCompanionSkill= "要使用 {0} 请输入 '@宠物技能{1}'";
            LearnBookFailed= "秘籍《{0}》修炼失败，没有包含你缺少的页面。";
            LearnBookSuccess= "恭喜，你成功修炼技能 {0}。";
            LearnBook4Failed= "你的技能 {0} 经验有所增加。";
            LearnBook4Success= "恭喜，你成功将技能 {0} 修炼到了 {1} 级。";
            StorageSafeZone = "只有在安全区才能使用仓库。";
            GuildStoragePermission= "你没有权限拿去公会的仓库物品。";
            GuildStorageSafeZone= "只有在安全区才能使用公会仓库。";
            CompanionNoRoom= "你的同伴无法带更多东西。";
            StorageLimit= "你不能扩展更多仓库空间了。";
            MarryAlreadyMarried= "你已经结过婚。";
            MarryNeedLevel= "你的等级需要达到22级才能结婚。";
            MarryNeedGold= "结婚需要支付 500,000 金币，你的金币数量不够。";
            MarryNotFacing= "你需要面朝对方才能继续。";
            MarryTargetAlreadyMarried= "{0} 已经结过婚。";
            MarryTargetHasProposal= "{0} 已经被求过婚。";
            MarryTargetNeedLevel = "{0} 需要达到22级才能结婚。";
            MarryTargetNeedGold = "{0} 金币不足导致无法结婚。";
            MarryDead= "你无法跟死亡的人结婚。";
            MarryComplete= "恭喜，你已经完成与 {0} 的结婚登记。";
            MarryDivorce= "你与 {0} 离婚了。";
            MarryDivorced= "{0} 与你离婚了。";
            MarryTeleportDead= "你已经死亡，无法传送到伴侣身边。";
            MarryTeleportPK= "你已经红名，无法传送到伴侣身边。";
            MarryTeleportDelay= "你需要等待 {0} 才能传送到伴侣身边。";
            MarryTeleportOffline= "你伴侣已经离线，因此你无法传送到对方身边。";
            MarryTeleportPartnerDead= "你伴侣已经死亡，因此你无法传送到对方身边。";
            MarryTeleportMap = "你伴侣所在的地图无法传送。";
            MarryTeleportMapEscape = "你所在地图无法使用夫妻传送。";
            CompanionAppearanceAlready = "{0} 已成功捕获。";
            CompanionNeedTicket = "你需要一张同伴票用以解锁新的外观。";
            CompanionSkillEnabled= "同伴技能等级 {0} 激活。";
            CompanionSkillDisabled = "同伴技能等级 {0} 禁用.";
            CompanionAppearanceLocked= "你没有捕获 {0} 的机会。";
            CompanionNeedGold = "你无法驾驭这个伙伴。";
            CompanionBadName= "你给伙伴取的新名字不符合要求。";
            CompanionRetrieveFailed= "能取回 {0} 是因为它有 {1}.";
            QuestSelectReward= "你必须选择奖励。";
            QuestNeedSpace = "无法完成任务，因为你的背包已经满了。";
            MailSafeZone= "无法从邮件获取物品，你必须在安全区。";
            MailNeedSpace= "无法从邮件获取物品，你的背包已满。";
            MailHasItems= "无法删除邮件因为里面有物品没取出。";
            MailNotFound = "{0} 不存在。";
            MailSelfMail= "你不能给自己发邮件。";
            MailMailCost= "你的金钱不够支付邮件费用。";
            MailSendSafeZone= "你必须呆在安全区才能用邮件寄出你背包里的物品。";
            ConsignSafeZone= "你必须呆在安全区才能寄存背包里的物品。";
            ConsignLimit= "你已经达到允许寄存数量的上限。";
            ConsignGuildFundsGuild= "你不能使用公会资金在市场上交易，因为你不是公会成员。";
            ConsignGuildFundsPermission= "你不能使用公会资金在市场上交易，因为你没有权限。";
            ConsignGuildFundsCost= "你的公会买不起这些东西。";
            ConsignGuildFundsUsed= "{0} 使用 {1:#,##0} 帮会金币采购了 {2} x{3} 用于 {4}.";
            ConsignCost= "你无法支付购买这些物品的费用。";
            ConsignComplete= "物品注册成功。";
            ConsignAlreadySold = "物品已被售出。";
            ConsignNotEnough = "你要购买的数量超出可出售的数量。";
            ConsignBuyOwnItem= "你不能购买自己的物品。";
            ConsignBuyGuildFundsGuild= "你不能使用公会金在商人处购买，因为你不是公会成员。";
            ConsignBuyGuildFundsPermission= "你不能使用公会金在市场上购买，因为你没有权限。";
            ConsignBuyGuildFundsCost= "你的公会无力支付购买这些物品的费用。";
            ConsignBuyGuildFundsUsed= "{0} 使用 {1:#,##0} 帮会金币采购了 {2} x{3}.";
            ConsignBuyCost= "你买不起这么多物品。";
            StoreNotAvailable= "购买失败，该物品当前不可购买。";
            StoreNeedSpace= "你背包空间不足，无法购买物品。";
            StoreCost= "你买不起这些物品。";
            GuildNeedHorn= "创建公会失败，你没有沃玛号角。";
            GuildNeedGold= "创建公会失败，你没有足够的金币。";
            GuildBadName= "创建公会失败，公会名称不符合要求。";
            GuildNameTaken= "创建公会失败，公会名称已被使用。";
            GuildNoticePermission= "你没有权限修改公会通告。";
            GuildEditMemberPermission = "你没有权限修改公会消息。";
            GuildMemberLength= "修改公会头衔失败，头衔名称太长。";
            GuildMemberNotFound= "找不到公会成员。";
            GuildKickPermission= "你没有权限开除公会成员。";
            GuildKickSelf= "你不能把自己从公会中开除。";
            GuildMemberKicked= "{0} 被 {1} 从公会中开除。";
            GuildKicked= "你被 {0} 从公会中开除了。";
            GuildManagePermission= "你没有管理公会的权限。";
            GuildMemberLimit= "公会人数已经达到上限。";
            GuildMemberCost= "公会没有足够的资源来提升人员限额。";
            GuildStorageLimit= "公会仓库已经达到上限。";
            GuildStorageCost= "公会没有足够资金来提升仓库上限。";
            GuildInvitePermission= "你没有权限邀请新成员。";
            GuildInviteGuild= "玩家: {0}, 已经加入其它公会。";
            GuildInviteInvited= "玩家: {0}, 已经被邀请加入其它公会。";
            GuildInviteNotAllowed= "玩家: {0}, 没有接受加入公会的邀请。";
            GuildInvitedNotAllowed= "{0} 想邀请你加入公会 {1}, 但是你没有接受邀请。@AllowGuild";
            GuildInviteRoom = "你的公会人数已经达到上限。";
            GuildNoGuild= "你还没有加入公会。";
            GuildWarPermission= "你没有权限开启公会战。";
            GuildNotFoundGuild= "找不到公会 {0}.";
            GuildWarOwnGuild= "你无法对自己的公会开战。";
            GuildAlreadyWar= "你们正在与公会 {0} 开战中。";
            GuildWarCost= "你的公会支付不起开战所需费用。";
            GuildWarFunds= "{0} 支出 {1:#,##0} 公会金向 {2} 开战。";
            GuildConquestCastle = "你已经拥有城堡，无法提交攻城请求。";
            GuildConquestExists = "你已经提交了攻城请求。";
            GuildConquestBadCastle= "无效的城堡。";
            GuildConquestProgress = "在攻城期间无法提交请求。";
            GuildConquestNeedItem= "你需要 {0} 才能申请对 {1} 的攻城战。";
            GuildConquestSuccess= "有个公会对你们城堡提交宣战请求。";
            GuildConquestDate = "你的公会已经对 {0} 宣战。";
            GuildJoinGuild= "你已经加入了公会。";
            GuildJoinTime= "你还需要等待 {0} 才能加入其它公会。";
            GuildJoinNoGuild= "玩家: {0}, 已退出公会。";
            GuildJoinPermission= "玩家: {0}, 没有权限把你添加进公会。";
            GuildJoinNoRoom= "{0} 的团队人数已经达到上限。";
            GuildJoinWelcome = "欢迎加入公会: {0}.";
            GuildMemberJoined= "{0} 已经邀请 {1} 加入公会。";
            GuildLeaveFailed= "离开公会失败，在没有帮主期间成员不能私自离开公会。";
            GuildLeave= "你已经离开公会。";
            GuildMemberLeave= "{0} 已经离开了公会。";
            GuildWarDeath= "来自 {1} 的 {0} 被来自公会 {3} 的 {2} 杀死。";
            GroupNoGroup= "你还没加入队伍。";
            GroupNotLeader = "你不是队伍的领导。";
            GroupMemberNotFound= "你的队伍找不到该成员: {0}。";
            GroupAlreadyGrouped = "玩家: {0}, 已经加入了其它队伍。";
            GroupAlreadyInvited = "玩家: {0}, 已被邀请加入其它队伍。";
            GroupInviteNotAllowed= "玩家: {0}, 没有接受组队邀请。";
            GroupSelf= "你不能和自己组队。";
            GroupMemberLimit= "{0} 的队伍已经达到人数上限。";
            GroupRecallDelay= "你还需等待 {0} 才能召唤组员。";
            GroupRecallMap= "这个地图不能使用组员召唤";
            GroupRecallNotAllowed = "你没有接受组员召唤。";
            GroupRecallMemberNotAllowed= "{0} 没有接受组员召唤。";
            GroupRecallFromMap= "你在这个地图无法接受组员召唤。";
            GroupRecallMemberFromMap= "{0} 无法从这个地图接受组员召唤。";
            TradeAlreadyTrading = "你已经在和其他人做交易。";
            TradeAlreadyHaveRequest = "你已经提交了交易请求。";
            TradeNeedFace= "你要面向对方才能发起交易。";
            TradeTargetNotAllowed = "{0} 拒绝了你的交易请求。";
            TradeTargetAlreadyTrading= "{0} 准备好和你交易。";
            TradeTargetAlreadyHaveRequest = "{0} 已经有交易请求了。";
            TradeNotAllowed= "{0} 想跟你进行交易，但你还未同意。@AllowTrade";
            TradeTargetDead = "你不能跟已经死亡的人交易。";
            TradeRequested = "你给 {0} 发送了交易请求...";
            TradeWaiting= "等待搭档接受你的交易请求...";
            TradePartnerWaiting= "你的搭档在等待你接受TA的交易请求...";
            TradeNoGold= "你没有足够的金币进行交易....";
            TradePartnerNoGold = "你的搭档没有足够金币进行交易。";
            TradeTooMuchGold= "你无法持有更多金币。";
            TradePartnerTooMuchGold= "你的搭档无法持有更多的金币...";
            TradeFailedItemsChanged= "你的物品发生了变化，交易失败。";
            TradeFailedPartnerItemsChanged= "{0}的物品发生了变化，交易失败。";
            TradeNotEnoughSpace = "你拿不下这么多物品，请扩充背包容量后再尝试。";
            TradeComplete = "交易完成..";
            NPCFundsGuild = "你不能使用公会金从商人那里购买东西，因为你不是公会成员。";
            NPCFundsPermission= "你不能使用公会金从商人那里购买东西，因为你没有权限。";
            NPCFundsCost= "无法购买该物品，你还需要 {0:#,##0} 金币。";
            NPCCost= "无法购买物品，你还需要 {0:#,##0} 金币.";
            NPCNoRoom= "你拿不动这么多物品。";
            NPCFundsBuy= "{0} 使用 {1:#,##0} 公会金购买了 {2} x{3}.";
            NPCSellWorthless= "无法出售该物品。";
            NPCSellTooMuchGold= "无法出售物品，你携带的金币过多。";
            NPCSellResult = "你出售 {0} 个物品，获得 {1:#,##0} 金币。";
            FragmentCost= "无法分解这些物品, 你还需要 {0:#,##0} 金币。";
            FragmentSpace= "无法分解这些物品，你背包空间不足。";
            FragmentResult= "你分解了 {0} 个物品，花费 {1:#,##0}.";
            AccessoryLevelCost= "你无法支付物品升级的费用。";
            AccessoryLeveled = "恭喜，你的 {0} 已经升级，可以准备提升属性。";
            RepairFail= "你无法修复 {0}.";
            RepairFailRepaired = "你无法修复 {0}, 因为已经修复过了。";
            RepairFailLocation= "你不能再这里修复 {0} 。";
            RepairFailCooldown= "你还需等待 {1} 才能特殊修复 {0} 。";
            NPCRepairGuild = "你不能使用公会金来进行修复，因为你不是公会成员。";
            NPCRepairPermission= "你不能使用公会金来进行修复，因为你没有权限。";
            NPCRepairGuildCost= "无法修复物品，你的公会金还缺少 {0:#,##0} .";
            NPCRepairCost = "无法修复物品，你还需要 {0:#,##0} 金币。";
            NPCRepairResult= "你修理了 {0} 个物品，花费 {1:#,##0} 金币。";
            NPCRepairSpecialResult= "你特殊修理了 {0} 个物品，花费了 {1:#,##0} 金币。";
            NPCRepairGuildResult= "{0} 使用了 {1:#,##0} 公会金，修复了 {2} 个物品。";
            NPCRefinementGold= "你没有足够金币。";
            NPCRefinementStoneFailedRoom = "创建精炼石失败，无法获得此物品。";
            NPCRefinementStoneFailed = "合成精炼石失败。";
            NPCRefineNotReady= "还没准备好..";
            NPCRefineNoRoom= "你背包没有足够空间。";
            NPCRefineSuccess= "恭喜, 精炼成功了。";
            NPCRefineFailed= "遗憾，精炼失败了。";
            NPCMasterRefineGold= "你没有足够的金币来进行大师精炼, 需要花费: {0:#,##0}.";
            NPCMasterRefineChance= "你的成功率为: {0}%";
            ChargeExpire = "{0} 的能量已从你的武器上消失。";
            ChargeFail= "{0} 充能失败..";
            CloakCombat = "你不能在战斗中披上斗篷。";
            DashFailed= "你的能量无法移动面前的东西。";
            WraithLevel= "{0} 级别太高，你的束缚无法生效。";
            AbyssLevel= "{0} 级别太高，你的深渊苦海无法生效。";
            SkillEffort= "Using {0} on this map takes more effort than normal, You cannot use items for a {1}.";
            SkillBadMap= "你无法在这个地图使用 {0}。";
            HorseDead= "死亡状态下无法骑马。";
            HorseOwner= "你还没有自己的马匹。";
            HorseMap = "这个地图不能骑马。";
        }

        public override string BannedWrongPassword { get; set; }


        public override string PaymentComplete { get; set; }
        public override string PaymentFailed { get; set; }
        public override string ReferralPaymentComplete { get; set; }
        public override string ReferralPaymentFailed { get; set; }
        public override string GameGoldLost { get; set; }
        public override string GameGoldRefund { get; set; }
        public override string HuntGoldRefund { get; set; }


        public override string Welcome { get; set; }
        public override string WelcomeObserver { get; set; }
        public override string ObserverChangeFail { get; set; } 
        public override string OnlineCount { get; set; }
        public override string ObserverCount { get; set; }
        public override string CannotFindPlayer { get; set; }
        public override string AlreadyBlocked { get; set; } 
        public override string BlockingWhisper { get; set; }
        public override string PlayerBlockingWhisper { get; set; }
        public override string GlobalDelay { get; set; }
        public override string GlobalLevel { get; set; } 
        public override string ShoutDelay { get; set; }
        public override string ShoutLevel { get; set; }
        public override string DiceRoll { get; set; } 
        public override string TradingEnabled { get; set; }
        public override string TradingDisabled { get; set; }
        public override string WhisperEnabled { get; set; }
        public override string WhisperDisabled { get; set; }
        public override string GuildInviteEnabled { get; set; } 
        public override string GuildInviteDisabled { get; set; } 
        public override string ObserverNotLoggedIn { get; set; } 
        public override string Poisoned { get; set; }
        public override string MurderedBy { get; set; } 
        public override string Curse { get; set; }
        public override string Murdered { get; set; }
        public override string Protected { get; set; } 
        public override string Killed { get; set; }
        public override string Died { get; set; }
        public override string GroupRecallEnabled { get; set; } 
        public override string GroupRecallDisabled { get; set; } 


        public override string NeedLevel { get; set; }
        public override string NeedMaxLevel { get; set; } 
        public override string NeedItem { get; set; }
        public override string NeedMonster { get; set; } 


        public override string ConquestStarted { get; set; } 
        public override string ConquestFinished { get; set; } 
        public override string ConquestCapture { get; set; } 
        public override string ConquestOwner { get; set; } 
        public override string ConquestLost { get; set; } 


        public override string BossSpawn { get; set; } 
        public override string HarvestRare { get; set; } 
        public override string NetherGateOpen { get; set; } 
        public override string NetherGateClosed { get; set; } 
        public override string HarvestNothing { get; set; } 
        public override string HarvestCarry { get; set; } 
        public override string HarvestOwner { get; set; } 
        public override string LairGateOpen { get; set; } 
        public override string LairGateClosed { get; set; } 


        public override string Expired { get; set; }
        public override string CannotTownTeleport { get; set; } 
        public override string CannotRandomTeleport { get; set; } 
        public override string ConnotResetCompanionSkill { get; set; } 
        public override string LearnBookFailed { get; set; } 
        public override string LearnBookSuccess { get; set; } 
        public override string LearnBook4Failed { get; set; } 
        public override string LearnBook4Success { get; set; } 
        public override string StorageSafeZone { get; set; }
        public override string GuildStoragePermission { get; set; } 
        public override string GuildStorageSafeZone { get; set; } 
        public override string CompanionNoRoom { get; set; } 
        public override string StorageLimit { get; set; } 


        public override string MarryAlreadyMarried { get; set; } 
        public override string MarryNeedLevel { get; set; } 
        public override string MarryNeedGold { get; set; } 
        public override string MarryNotFacing { get; set; } 
        public override string MarryTargetAlreadyMarried { get; set; } 
        public override string MarryTargetHasProposal { get; set; } 
        public override string MarryTargetNeedLevel { get; set; }
        public override string MarryTargetNeedGold { get; set; }
        public override string MarryDead { get; set; } 
        public override string MarryComplete { get; set; } 
        public override string MarryDivorce { get; set; } 
        public override string MarryDivorced { get; set; } 
        public override string MarryTeleportDead { get; set; } 
        public override string MarryTeleportPK { get; set; } 
        public override string MarryTeleportDelay { get; set; } 
        public override string MarryTeleportOffline { get; set; } 
        public override string MarryTeleportPartnerDead { get; set; } 
        public override string MarryTeleportMap { get; set; }
        public override string MarryTeleportMapEscape { get; set; }


        public override string CompanionAppearanceAlready { get; set; }
        public override string CompanionNeedTicket { get; set; }
        public override string CompanionSkillEnabled { get; set; } 
        public override string CompanionSkillDisabled { get; set; }
        public override string CompanionAppearanceLocked { get; set; } 
        public override string CompanionNeedGold { get; set; }
        public override string CompanionBadName { get; set; } 
        public override string CompanionRetrieveFailed { get; set; } 
        public override string QuestSelectReward { get; set; } 
        public override string QuestNeedSpace { get; set; }


        public override string MailSafeZone { get; set; } 
        public override string MailNeedSpace { get; set; } 
        public override string MailHasItems { get; set; } 
        public override string MailNotFound { get; set; }
        public override string MailSelfMail { get; set; } 
        public override string MailMailCost { get; set; } 
        public override string MailSendSafeZone { get; set; } 


        public override string ConsignSafeZone { get; set; } 
        public override string ConsignLimit { get; set; } 
        public override string ConsignGuildFundsGuild { get; set; } 
        public override string ConsignGuildFundsPermission { get; set; } 
        public override string ConsignGuildFundsCost { get; set; } 
        public override string ConsignGuildFundsUsed { get; set; } 
        public override string ConsignCost { get; set; } 
        public override string ConsignComplete { get; set; } 
        public override string ConsignAlreadySold { get; set; }
        public override string ConsignNotEnough { get; set; }
        public override string ConsignBuyOwnItem { get; set; } 
        public override string ConsignBuyGuildFundsGuild { get; set; } 
        public override string ConsignBuyGuildFundsPermission { get; set; } 
        public override string ConsignBuyGuildFundsCost { get; set; } 
        public override string ConsignBuyGuildFundsUsed { get; set; } 
        public override string ConsignBuyCost { get; set; } 


        public override string StoreNotAvailable { get; set; } 
        public override string StoreNeedSpace { get; set; } 
        public override string StoreCost { get; set; } 


        public override string GuildNeedHorn { get; set; } 
        public override string GuildNeedGold { get; set; } 
        public override string GuildBadName { get; set; } 
        public override string GuildNameTaken { get; set; } 
        public override string GuildNoticePermission { get; set; } 
        public override string GuildEditMemberPermission { get; set; }
        public override string GuildMemberLength { get; set; } 
        public override string GuildMemberNotFound { get; set; } 
        public override string GuildKickPermission { get; set; } 
        public override string GuildKickSelf { get; set; } 
        public override string GuildMemberKicked { get; set; } 
        public override string GuildKicked { get; set; } 
        public override string GuildManagePermission { get; set; } 
        public override string GuildMemberLimit { get; set; } 
        public override string GuildMemberCost { get; set; } 
        public override string GuildStorageLimit { get; set; } 
        public override string GuildStorageCost { get; set; } 
        public override string GuildInvitePermission { get; set; } 
        public override string GuildInviteGuild { get; set; } 
        public override string GuildInviteInvited { get; set; } 
        public override string GuildInviteNotAllowed { get; set; } 
        public override string GuildInvitedNotAllowed { get; set; } 
        public override string GuildInviteRoom { get; set; }
        public override string GuildNoGuild { get; set; } 
        public override string GuildWarPermission { get; set; } 
        public override string GuildNotFoundGuild { get; set; } 
        public override string GuildWarOwnGuild { get; set; } 
        public override string GuildAlreadyWar { get; set; } 
        public override string GuildWarCost { get; set; } 
        public override string GuildWarFunds { get; set; } 
        public override string GuildConquestCastle { get; set; }
        public override string GuildConquestExists { get; set; }
        public override string GuildConquestBadCastle { get; set; } 
        public override string GuildConquestProgress { get; set; }
        public override string GuildConquestNeedItem { get; set; } 
        public override string GuildConquestSuccess { get; set; } 
        public override string GuildConquestDate { get; set; }
        public override string GuildJoinGuild { get; set; } 
        public override string GuildJoinTime { get; set; } 
        public override string GuildJoinNoGuild { get; set; } 
        public override string GuildJoinPermission { get; set; } 
        public override string GuildJoinNoRoom { get; set; } 
        public override string GuildJoinWelcome { get; set; }
        public override string GuildMemberJoined { get; set; } 
        public override string GuildLeaveFailed { get; set; } 
        public override string GuildLeave { get; set; } 
        public override string GuildMemberLeave { get; set; } 
        public override string GuildWarDeath { get; set; } 


        public override string GroupNoGroup { get; set; } 
        public override string GroupNotLeader { get; set; }
        public override string GroupMemberNotFound { get; set; } 
        public override string GroupAlreadyGrouped { get; set; }
        public override string GroupAlreadyInvited { get; set; }
        public override string GroupInviteNotAllowed { get; set; } 
        public override string GroupSelf { get; set; } 
        public override string GroupMemberLimit { get; set; } 
        public override string GroupRecallDelay { get; set; } 
        public override string GroupRecallMap { get; set; } 
        public override string GroupRecallNotAllowed { get; set; }
        public override string GroupRecallMemberNotAllowed { get; set; } 
        public override string GroupRecallFromMap { get; set; } 
        public override string GroupRecallMemberFromMap { get; set; } 


        public override string TradeAlreadyTrading { get; set; }
        public override string TradeAlreadyHaveRequest { get; set; }
        public override string TradeNeedFace { get; set; } 
        public override string TradeTargetNotAllowed { get; set; }
        public override string TradeTargetAlreadyTrading { get; set; } 
        public override string TradeTargetAlreadyHaveRequest { get; set; }
        public override string TradeNotAllowed { get; set; } 
        public override string TradeTargetDead { get; set; }
        public override string TradeRequested { get; set; }
        public override string TradeWaiting { get; set; } 
        public override string TradePartnerWaiting { get; set; } 
        public override string TradeNoGold { get; set; } 
        public override string TradePartnerNoGold { get; set; }
        public override string TradeTooMuchGold { get; set; } 
        public override string TradePartnerTooMuchGold { get; set; } 
        public override string TradeFailedItemsChanged { get; set; } 
        public override string TradeFailedPartnerItemsChanged { get; set; } 
        public override string TradeNotEnoughSpace { get; set; }
        public override string TradeComplete { get; set; }


        public override string NPCFundsGuild { get; set; }
        public override string NPCFundsPermission { get; set; } 
        public override string NPCFundsCost { get; set; } 
        public override string NPCCost { get; set; } 
        public override string NPCNoRoom { get; set; } 
        public override string NPCFundsBuy { get; set; } 
        public override string NPCSellWorthless { get; set; } 
        public override string NPCSellTooMuchGold { get; set; } 
        public override string NPCSellResult { get; set; }
        public override string FragmentCost { get; set; } 
        public override string FragmentSpace { get; set; } 
        public override string FragmentResult { get; set; } 
        public override string AccessoryLevelCost { get; set; } 
        public override string AccessoryLeveled { get; set; }
        public override string RepairFail { get; set; } 
        public override string RepairFailRepaired { get; set; }
        public override string RepairFailLocation { get; set; } 
        public override string RepairFailCooldown { get; set; } 
        public override string NPCRepairGuild { get; set; }
        public override string NPCRepairPermission { get; set; } 
        public override string NPCRepairGuildCost { get; set; } 
        public override string NPCRepairCost { get; set; }
        public override string NPCRepairResult { get; set; } 
        public override string NPCRepairSpecialResult { get; set; } 
        public override string NPCRepairGuildResult { get; set; } 
        public override string NPCRefinementGold { get; set; } 
        public override string NPCRefinementStoneFailedRoom { get; set; }
        public override string NPCRefinementStoneFailed { get; set; }
        public override string NPCRefineNotReady { get; set; } 
        public override string NPCRefineNoRoom { get; set; } 
        public override string NPCRefineSuccess { get; set; } 
        public override string NPCRefineFailed { get; set; } 
        public override string NPCMasterRefineGold { get; set; } 
        public override string NPCMasterRefineChance { get; set; } 


        public override string ChargeExpire { get; set; }
        public override string ChargeFail { get; set; } 
        public override string CloakCombat { get; set; }
        public override string DashFailed { get; set; } 
        public override string WraithLevel { get; set; } 
        public override string AbyssLevel { get; set; } 
        public override string SkillEffort { get; set; } 
        public override string SkillBadMap { get; set; } 


        public override string HorseDead { get; set; } 
        public override string HorseOwner { get; set; } 
        public override string HorseMap { get; set; } 
    }
}
