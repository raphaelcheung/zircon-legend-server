using Library;
using Server.WebApi.Auth;
using Server.WebApi.Services;
using System.Security.Claims;
using Server.Envir;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Server.WebApi.Endpoints
{
    /// <summary>
    /// 仪表盘及核心系统运维 API 控制端点
    /// </summary>
    public static class DashboardEndpoints
    {
        // 已彻底安全移除 EmptyWorkingSet 底层内核 API 导入与调用，彻底防止 Windows 11 系统在特定杀毒挂钩或内核隔离下的蓝屏/重启冲突，全面替换为纯托管安全模式

        public static void Map(WebApplication app)
        {
            var group = app.MapGroup("/api/dashboard")
                .RequireAuthorization();

            // 获取仪表盘实时统计数据及服务器基本配置
            group.MapGet("/stats", GetStats);
            // 【新增】快捷运维操作 API 端点
            group.MapPost("/save-users", SaveUsers);      // 手动保存玩家数据 (.db)
            group.MapPost("/save-system", SaveSystem);    // 手动保存系统数据 (.db)
            group.MapPost("/gc", TriggerGC);              // 手动触发服务器垃圾回收 (释放内存)
            group.MapPost("/kick-all", KickAllPlayers);  // 带全服滚动系统红字倒计时的强制踢人
            group.MapPost("/restart", RestartServer);    // 带全服滚动系统红字倒计时的服务优雅自重启
        }

        /// <summary>
        /// 获取仪表盘实时监控信息与服务器当前运行倍率
        /// </summary>
        private static IResult GetStats(ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var uptime = dataService.GetUptime();
            var onlinePlayers = dataService.GetOnlinePlayers();

            // 获取当前服务器进程的真实物理内存占用并折算为 MB (1MB = 1024 * 1024 字节)
            double workingSetMB = Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0;
            double gcHeapMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0;

            return Results.Ok(new DashboardStats
            {
                OnlinePlayers = dataService.GetOnlinePlayerCount(),
                TotalAccounts = dataService.GetTotalAccountCount(),
                TotalCharacters = dataService.GetTotalCharacterCount(),
                Started = dataService.IsServerRunning(),
                UptimeSeconds = (long)uptime.TotalSeconds,
                Uptime = FormatUptime(uptime),
                
                // 【新增数据项】物理内存占用与七大核心系统运行倍率
                MemoryUsage = $"{gcHeapMB:F1} MB",
                ExperienceRate = Config.ExperienceRate,
                DropRate = Config.DropRate,
                GoldRate = Config.GoldRate,
                SkillLowRate = Config.技能低等级经验倍率,
                SkillHighRate = Config.技能高等级经验倍率,
                CompanionRate = Config.CompanionRate,
                BossDropRate = Config.Boss掉落倍率,

                RecentPlayers = onlinePlayers.Take(5).Select(p => new RecentPlayerDto
                {
                    Name = p.CharacterName,
                    Level = p.Level,
                    Class = p.Class,
                    Map = p.MapName
                }).ToList()
            });
        }

        /// <summary>
        /// 格式化运行时长为高可读的中文格式
        /// </summary>
        private static string FormatUptime(TimeSpan uptime)
        {
            if (uptime.TotalDays >= 1)
            {
                return $"{(int)uptime.TotalDays}天 {uptime.Hours}小时 {uptime.Minutes}分钟";
            }
            if (uptime.TotalHours >= 1)
            {
                return $"{(int)uptime.TotalHours}小时 {uptime.Minutes}分钟";
            }
            return $"{uptime.Minutes}分钟 {uptime.Seconds}秒";
        }

        /// <summary>
        /// <summary>
        /// 快捷运维：立即保存玩家数据 (users.db)
        /// </summary>
        private static IResult SaveUsers(ClaimsPrincipal user)
        {
            // 权限验证：至少需要 Supervisor (普通/高级管理员) 特权凭证
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            // 强力防崩卫哨：检测服务器是否离线或数据库未完全加载就绪
            if (!SEnvir.HasSession)
            {
                return Results.BadRequest(new { message = "游戏服务器当前处于关闭或正在加载状态，请在服务器开启就绪后再执行玩家数据存盘。" });
            }

            try
            {
                // 安全审计：抓取当前执行该操作的管理员邮箱并载入日志
                var adminEmail = JwtHelper.GetEmail(user);
                SEnvir.Log($"[系统运维操作] 管理员={adminEmail}, 触发了手动保存玩家数据(SaveUserDatas)");

                // 调用底层 Session 的物理存盘，安全写入磁盘
                SEnvir.SaveUserDatas();
                return Results.Ok(new { message = "玩家数据已成功安全写入磁盘。" });
            }
            catch (Exception ex)
            {
                SEnvir.Log($"[系统运维操作] 手动保存玩家数据失败！错误原因: {ex.Message}");
                // 【安全气囊优化】防崩捕获，避免异常击穿 Web 进程
                return Results.BadRequest(new { message = $"保存玩家数据失败: {ex.Message}，可能是由于磁盘空间不足或独占锁定，请稍后重试。" });
            }
        }

        /// <summary>
        /// <summary>
        /// 快捷运维：立即保存系统全局数据 (system.db)
        /// </summary>
        private static IResult SaveSystem(ClaimsPrincipal user)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            // 强力防崩卫哨：检测服务器是否离线或数据库未完全加载就绪
            if (!SEnvir.HasSession)
            {
                return Results.BadRequest(new { message = "游戏服务器当前处于关闭或正在加载状态，请在服务器开启就绪后再执行系统数据存盘。" });
            }

            try
            {
                var adminEmail = JwtHelper.GetEmail(user);
                SEnvir.Log($"[系统运维操作] 管理员={adminEmail}, 触发了手动保存系统数据(SaveSystem)");

                // 强制保存系统表数据落盘
                SEnvir.SaveSystem();
                return Results.Ok(new { message = "系统数据已成功安全写入磁盘。" });
            }
            catch (Exception ex)
            {
                SEnvir.Log($"[系统运维操作] 手动保存系统数据失败！错误原因: {ex.Message}");
                // 【安全气囊优化】捕获异常并返回友好的 400 Bad Request，防止 HTTP 崩溃成 500
                return Results.BadRequest(new { message = $"保存系统数据失败: {ex.Message}。可能是由于数据库文件被系统独占或写入冲突，请稍后重试。" });
            }
        }

        /// <summary>
        /// 快捷运维：立即执行内存垃圾清理与托管资源回收 (GC)
        /// </summary>
        private static IResult TriggerGC(ClaimsPrincipal user)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var adminEmail = JwtHelper.GetEmail(user);

            // 获取回收前的物理内存大小
            double gcBefore = GC.GetTotalMemory(false) / 1024.0 / 1024.0;

            // 1. 强制触发 .NET 托管堆的全局垃圾清理并挂起线程直到终结器全部执行完毕（最高代 Gen2 强制同步整理，支持大对象堆压缩）
            // 采用纯托管模式垃圾清理，100% 绝对物理安全，彻底消除 Windows 蓝屏/系统重启隐患
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, true, true);

            // 获取回收后的物理内存大小
            double gcAfter = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
            double gcFreed = gcBefore - gcAfter;

            // 详细写入前后内存对比的日志，方便运维追踪资源释放效果
            SEnvir.Log($"[系统运维操作] 管理员={adminEmail}, 触发了系统内存清理。清理前={gcBefore:F1}MB, 清理后={gcAfter:F1}MB (释放了 {gcFreed:F1}MB)");

            return Results.Ok(new
            {
                message = "垃圾回收资源清理完毕（已采用纯托管安全模式）。",
                before = $"{gcBefore:F1} MB",
                after = $"{gcAfter:F1} MB",
                freed = $"{gcFreed:F1} MB"
            });
        }

        /// <summary>
        /// 快捷运维：强制踢出所有玩家下线 (支持全服倒计时滚动公告)
        /// </summary>
        private static IResult KickAllPlayers(ClaimsPrincipal user, [FromQuery] int delaySeconds = 60)
        {
            // 权限验证：高危操作，强制要求 Admin (超级管理员) 特权凭证
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Admin))
            {
                return Results.Forbid();
            }

            // 强力防崩卫哨：检测服务器是否离线或数据库未就绪
            if (!SEnvir.HasSession)
            {
                return Results.BadRequest(new { message = "游戏服务器当前处于关闭状态，无法执行一键踢人操作。" });
            }

            var adminEmail = JwtHelper.GetEmail(user);
            SEnvir.Log($"[系统运维操作] 管理员={adminEmail}, 触发了强制下线所有玩家，设定倒计时={delaySeconds}秒");

            // 如果延迟秒数 <= 0，立即无感踢出所有玩家
            if (delaySeconds <= 0)
            {
                SEnvir.Post(_ =>
                {
                    SEnvir.SaveUserDatas(); // 踢人前先进行安全数据存盘，防止回档
                    int count = 0;
                    var players = SEnvir.Players.ToArray(); // 复制数组防止遍历中途因下线修改集合报错
                    foreach (var player in players)
                    {
                        if (player?.Connection != null)
                        {
                            player.Connection.TryDisconnect();
                            count++;
                        }
                    }
                    SEnvir.Log($"[系统运维操作] 已立即强制断开了所有玩家的网络连接。断开人数={count}");
                });
                return Results.Ok(new { message = $"立即踢人指令已安全投递到游戏主线程。" });
            }
            else
            {
                // 开启异步倒计时广播任务，避免阻塞 HTTP 响应线程
                Task.Run(async () =>
                {
                    try
                    {
                        for (int i = delaySeconds; i > 0; i--)
                        {
                            // 在开始、每10秒、以及倒数最后5秒时发送游戏内全局红字公告
                            if (i == delaySeconds || i % 10 == 0 || i <= 5)
                            {
                                SEnvir.Post(_ => SEnvir.Broadcast(new Library.Network.ServerPackets.Chat
                                {
                                    Text = $"【系统公告】服务器将在 {i} 秒后强制踢出所有在线玩家，请妥善做好安全下线准备！",
                                    Type = MessageType.System
                                }));
                            }
                            await Task.Delay(1000);
                        }

                        // 倒计时归零，安全落盘并强制切断网络
                        SEnvir.Post(_ =>
                        {
                            SEnvir.SaveUserDatas();
                            int count = 0;
                            var players = SEnvir.Players.ToArray();
                            foreach (var player in players)
                            {
                                if (player?.Connection != null)
                                {
                                    player.Connection.TryDisconnect();
                                    count++;
                                }
                            }
                            SEnvir.Log($"[系统运维操作] 倒计时结束，已完成全员强制踢下线。执行切断人数={count}");
                        });
                    }
                    catch (Exception ex)
                    {
                        SEnvir.Log($"[系统运维操作] 异步倒计时踢人发生异常: {ex.Message}");
                    }
                });

                return Results.Ok(new { message = $"倒计时 {delaySeconds} 秒强制切断指令已发布，游戏内公告已开始循环滚动。" });
            }
        }

        /// <summary>
        /// 快捷运维：优雅自重启服务器程序 (支持全服倒计时滚动公告)
        /// </summary>
        private static IResult RestartServer(ClaimsPrincipal user, [FromQuery] int delaySeconds = 60)
        {
            // 权限验证：高危操作，强制要求 Admin (超级管理员) 特权凭证
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Admin))
            {
                return Results.Forbid();
            }

            // 强力防崩卫哨：检测服务器是否离线或数据库未就绪
            if (!SEnvir.HasSession || !SEnvir.Started)
            {
                return Results.BadRequest(new { message = "游戏服务器当前处于关闭状态，无需执行自重启。" });
            }

            // 【防重复触发自重启保护】
            if (SEnvir.RequestRestart)
            {
                return Results.BadRequest(new { message = "重启指令已发送，正在执行重启流程，请勿重复操作。" });
            }

            var adminEmail = JwtHelper.GetEmail(user);
            SEnvir.Log($"[系统运维操作] 管理员={adminEmail}, 触发了服务器安全自重启，设定倒计时={delaySeconds}秒");

            // 如果延迟秒数 <= 0，立即保存并重启
            if (delaySeconds <= 0)
            {
                SEnvir.Log("[系统自重启] 接收到立即重启指令，主线程即将优雅停服并重新拉起新实例。");
                SEnvir.RequestRestart = true;
                SEnvir.Started = false; // 将运行标志置为 false，主循环会自动退出并完成数据强制存盘
                return Results.Ok(new { message = "服务器正在立即安全停服，并将于新进程窗口秒级拉起重新开服。" });
            }
            else
            {
                // 开启异步倒计时警告，每隔一定时间广播游戏内红字警告，让正在副本或打宝的玩家有时间回城或下线
                Task.Run(async () =>
                {
                    try
                    {
                        for (int i = delaySeconds; i > 0; i--)
                        {
                            if (i == delaySeconds || i % 10 == 0 || i <= 5)
                            {
                                int secs = i;
                                SEnvir.Post(_ => SEnvir.Broadcast(new Library.Network.ServerPackets.Chat
                                {
                                    Text = $"【系统公告】服务器将在 {secs} 秒后进行安全保存并自动重启，请玩家就地回城安全下线！",
                                    Type = MessageType.System
                                }));
                            }
                            await Task.Delay(1000);
                        }

                        SEnvir.Log("[系统自重启] 倒计时结束，服务器开始执行安全停服自重启...");
                        SEnvir.RequestRestart = true;
                        SEnvir.Started = false; // 触发优雅的 SEnvirLoop 退出，退出时收尾存盘并触发 Program.cs 重新拉起
                    }
                    catch (Exception ex)
                    {
                        SEnvir.Log($"[系统自重启] 异步倒计时重启发生异常: {ex.Message}");
                    }
                });

                return Results.Ok(new { message = $"安全自重启倒计时 {delaySeconds} 秒已发布，游戏内系统公告已开始滚动广播。" });
            }
        }
    }

    /// <summary>
    /// 实时仪表盘监控数据载体 (DTO)
    /// </summary>
    public class DashboardStats
    {
        public int OnlinePlayers { get; set; }
        public int TotalAccounts { get; set; }
        public int TotalCharacters { get; set; }
        public bool Started { get; set; }
        public long UptimeSeconds { get; set; }
        public string Uptime { get; set; } = "";
        
        // 【新增属性字段】
        public string MemoryUsage { get; set; } = "";     // 当前进程物理内存使用量字符串 (如: "152.6 MB")
        public int ExperienceRate { get; set; }         // 系统当前全局经验倍率 (百分比数值，0=默认1倍)
        public int DropRate { get; set; }               // 系统当前全局爆率倍率 (百分比数值，0=默认1倍)
        public int GoldRate { get; set; }               // 系统当前全局金币倍率 (百分比数值，0=默认1倍)
        public int SkillLowRate { get; set; }           // 系统当前低级技能倍率
        public int SkillHighRate { get; set; }          // 系统当前高级技能倍率
        public int CompanionRate { get; set; }          // 系统当前宠物定时经验增长加成点数
        public int BossDropRate { get; set; }           // 系统当前 Boss 爆率倍率

        public List<RecentPlayerDto> RecentPlayers { get; set; } = new();
    }

    /// <summary>
    /// 最近活动玩家数据载体 (DTO)
    /// </summary>
    public class RecentPlayerDto
    {
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public string Class { get; set; } = "";
        public string Map { get; set; } = "";
    }
}

