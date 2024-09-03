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

GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

bool running = true;

Task t = new(ShowLog);
t.Start();
SEnvir.LoadClientHash();
SEnvir.StartServer();
t.Dispose();

void ShowLog()
{
    while(running)
    {
        while(SEnvir.DisplayLogs.TryDequeue(out string? log) && !string.IsNullOrEmpty(log))
        {
            Console.WriteLine(log);
        }

        Thread.Sleep(100);
    }
}

ConfigReader.Save();