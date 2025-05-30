﻿using System.Threading;
using System.Runtime;
using Library;
using Server.Envir;
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
SEnvir.StartServer();

while(!stop) Thread.Sleep(100);

//ConfigReader.Save();