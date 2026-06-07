using System.Threading;
using System.Runtime;
using Library;
using Server.Envir;
using Server.WebApi;
using System.Reflection;

ConfigReader.Load();

string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();



Console.WriteLine($"皓石传奇三 v{version}");
Console.WriteLine($"免费开源的传奇三，开源技术交流进QQ群 915941142");
Console.WriteLine($"客户端更新路径：{Config.ClientPath}");
Console.WriteLine($"地图文件路径：{Config.MapPath}");

if (Config.ConnectionLimit <= 0 || Config.ConnectionLimit >= 65534)
{
    Config.ConnectionLimit = 200;
    Console.WriteLine($"[最大连接数量限制] 配置了无效值，恢复默认值 {Config.ConnectionLimit}...");
}

Console.WriteLine($"最大连接数量限制：{Config.ConnectionLimit}");

if (Config.武器重置等待分钟 < 0)
{
    Config.武器重置等待分钟 = 24 * 60;
    Console.WriteLine($"[武器重置等待分钟] 配置了无效值，恢复默认值 {Config.武器重置等待分钟} ...");
}

if (Config.挖出的黑铁矿最小纯度 < 0 )
{
    Config.挖出的黑铁矿最小纯度 = 25;
    Console.WriteLine($"[挖出的黑铁矿最小纯度] 设置了无效值，恢复默认值 {Config.挖出的黑铁矿最小纯度} ...");
}

if (Config.挖出的黑铁矿最大纯度 < Config.挖出的黑铁矿最小纯度)
{
    Config.挖出的黑铁矿最大纯度 = Config.挖出的黑铁矿最小纯度;
    Console.WriteLine($"[挖出的黑铁矿最大纯度] 设置了无效值，恢复默认值 {Config.挖出的黑铁矿最大纯度}...");
}

if (Config.技能最高等级 < 0)
{
    Config.技能最高等级 = 3;
    Console.WriteLine($"[技能最高等级] 设置了无效值，恢复默认值 {Config.技能最高等级} ...");
}

if (Config.数据清理间隔分钟 < 0)
{
    Config.数据清理间隔分钟 = 0;
    Console.WriteLine($"[内存垃圾回收间隔多少分钟] 设置了无效值，恢复默认值 {Config.数据清理间隔分钟} ...");
}

if (Config.武器重置时每多少点属性保留一点 <= 0)
{
    Config.武器重置时每多少点属性保留一点 = 10;
    Console.WriteLine($"[武器重置时每多少点属性保留一点] 设置了无效值，恢复默认值 {Config.武器重置时每多少点属性保留一点} ...");
}

if (Config.武器重置冷却分钟 < 0)
{
    Config.武器重置冷却分钟 = 1440;
    Console.WriteLine($"[武器重置冷却分钟] 设置了无效值，恢复默认值 {Config.武器重置冷却分钟} ...");
}

if (Config.判断敏感词最大跳几个字符 < 0)
{
    Config.判断敏感词最大跳几个字符 = 2;
    Console.WriteLine($"[判断敏感词最大跳几个字符] 设置了无效值，恢复默认值 {Config.判断敏感词最大跳几个字符} ...");
}

if (Config.宠物不追击距离玩家多少格以外的敌人 <= 0)
{
    Config.宠物不追击距离玩家多少格以外的敌人 = 10;
    Console.WriteLine($"[宠物不追击距离玩家多少格以外的敌人] 设置了无效值，恢复默认值 {Config.宠物不追击距离玩家多少格以外的敌人} ...");
}

GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

bool stop = false;
bool running = true;

Task.Run(() =>
{
    stop = false;
    while (running)
    {
        while (SEnvir.DisplayLogs.TryDequeue(out string? log))
        {
            if (log != null) Console.WriteLine(log);
        }

        Thread.Sleep(100);
    }

    stop = true;
});

SEnvir.LoadBlock();
SEnvir.LoadClientHash();
var log = SEnvir.LoadRebirthInfo();
if (!string.IsNullOrEmpty(log)) SEnvir.Log(log);

// Start WebAPI server (must be before StartServer because EnvirLoop blocks)
WebApiStartup.Start();

SEnvir.StartServer();

// 【修复自重启死循环】当游戏主服务循环退出后，立即将 running 设为 false，以通知日志守护后台线程优雅退出
running = false;

// 增加 30 秒超时防死锁机制
int waitCount = 0;
while(!stop && waitCount < 300)
{
    Thread.Sleep(100);
    waitCount++;
}
if (!stop)
{
    Console.WriteLine("[警告] 后台日志线程未能在 30 秒内优雅退出，强制继续退出流程！");
}

// 【新增】系统安全自重启检测机制
// 如果 SEnvir.RequestRestart 被设置为 true，说明管理员在后台网页触发了安全重启指令
if (SEnvir.RequestRestart)
{
    try
    {
        // 1. 获取当前正在运行的 C# 服务器程序的完整绝对路径
        // 【修复】改用 Environment.ProcessPath 适配跨平台，而不是 MainModule.FileName
        string? exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            Console.WriteLine("[系统自重启] 无法获取当前进程路径，重启失败！");
        }
        else
        {
            // 2. 构造进程启动信息，并设定使用系统 Shell 独立运行，从而拉起一个干净的全新服务器实例
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = false // 【修复】关闭 ShellExecute 兼容 Linux/Docker 无界面环境
            };

            // 传递原有的命令行参数（跳过第 0 个参数，即执行程序本身）
            var cmdArgs = Environment.GetCommandLineArgs();
            for (int i = 1; i < cmdArgs.Length; i++)
            {
                startInfo.ArgumentList.Add(cmdArgs[i]);
            }
            
            // 3. 拉起新实例
            var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null)
            {
                Console.WriteLine("[系统自重启] 拉起新进程实例失败，Process.Start 返回 null。");
            }
            else
            {
                Console.WriteLine($"[系统自重启] 成功拉起新进程实例，执行路径: {exePath}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[系统自重启] 启动新进程时发生异常: {ex.Message}");
    }
}

//ConfigReader.Save();