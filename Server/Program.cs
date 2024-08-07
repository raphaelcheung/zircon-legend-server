using System.Threading;
using System.Runtime;
using Library;
using Server.Envir;
using System.Reflection;

ConfigReader.Load();
Config.LoadVersion();

string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

Console.WriteLine($"皓石传奇三 v{version}");
Console.WriteLine($"免费开源的传奇三，有疑问请联系开源志愿者：QQ50181976");
Console.WriteLine($"版本文件路径：{Config.VersionPath}");
Console.WriteLine($"地图文件路径：{Config.MapPath}");


GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

bool running = true;

Task t = new(ShowLog);
t.Start();
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