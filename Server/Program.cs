using System.Threading;
using System.Runtime;
using Library;
using Server.Envir;
using System.Reflection;

ConfigReader.Load();

string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();



Console.WriteLine($"皓石传奇三 v{version}");
Console.WriteLine($"免费开源的传奇三，技术交流或想体验公益服进QQ群 915941142");
Console.WriteLine($"客户端更新路径：{Config.ClientPath}");
Console.WriteLine($"地图文件路径：{Config.MapPath}");

if (Config.ConnectionLimit <= 0 || Config.ConnectionLimit >= 65534)
{
    Console.WriteLine($"[最大连接数量限制] 配置了无效值 {Config.ConnectionLimit}，采用默认值...");
    Config.ConnectionLimit = 200;
}

Console.WriteLine($"最大连接数量限制：{Config.ConnectionLimit}");

if (Config.武器重置等待分钟 < 0)
{
    Console.WriteLine($"[武器重置等待分钟] 配置了无效值 {Config.武器重置等待分钟}，采用默认值...");
    Config.武器重置等待分钟 = 24 * 60;
}

if (Config.挖出的黑铁矿最小纯度 < 0 )
{
    Console.WriteLine($"[挖出的黑铁矿最小纯度] 设置了无效值：{Config.挖出的黑铁矿最小纯度}，采用默认值...");
    Config.挖出的黑铁矿最小纯度 = 25;
}

if (Config.挖出的黑铁矿最大纯度 < Config.挖出的黑铁矿最小纯度)
{
    Console.WriteLine($"[挖出的黑铁矿最大纯度] 设置了无效值：{Config.挖出的黑铁矿最大纯度}，采用默认值...");
    Config.挖出的黑铁矿最大纯度 = Config.挖出的黑铁矿最小纯度;
}

if (Config.技能最高等级 < 0)
{
    Console.WriteLine($"[技能最高等级] 设置了无效值：{Config.技能最高等级}，采用默认值...");
    Config.技能最高等级 = 1;
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

SEnvir.LoadClientHash();
SEnvir.StartServer();

while(!stop) Thread.Sleep(100);

//ConfigReader.Save();