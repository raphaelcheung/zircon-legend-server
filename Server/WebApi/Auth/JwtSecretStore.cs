using Library;
using Server.Envir;
using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Server.WebApi.Auth
{
    /// <summary>
    /// 解析 WebAPI 的 JWT 签名密钥。
    ///
    /// 背景(上游安全报告 issue #21):旧版本把签名密钥硬编码为
    /// "ZirconLegendServer2024SecretKey32",而 Server.ini 缺少 [WebApi] 段时
    /// ConfigReader 会沿用源码里的默认值。由于该密钥是公开的,任何能访问 WebAPI
    /// 端口的人都可以自行签发一个 identity=SuperAdmin 的令牌。
    ///
    /// 本类保证:
    /// 1. 任何情况下都不会使用旧的公开默认密钥;
    /// 2. 未显式配置(或仍为旧默认值)时,自动生成加密随机密钥并写入 Server.ini;
    /// 3. 写入失败时退回内存临时密钥,而不是退回公开密钥。
    /// </summary>
    public static class JwtSecretStore
    {
        /// <summary>旧版本硬编码的公开密钥,视为已泄漏,启动时必须轮换。</summary>
        public const string LegacyDefaultSecret = "ZirconLegendServer2024SecretKey32";

        /// <summary>HS256 要求密钥至少 256 bit(32 字节),这里生成 48 字节留出余量。</summary>
        private const int SecretByteLength = 48;

        private const int MinimumSecretBytes = 32;
        private const string SectionName = "WebApi";
        private const string SecretKeyName = "WebApiJwtSecret";
        private const string DefaultConfigPath = @"./datas/Server.ini";

        /// <summary>
        /// 返回一个可用的强随机签名密钥。
        /// 若配置中已提供合规密钥则直接返回,否则生成随机密钥并尽力持久化。
        /// </summary>
        public static string Resolve()
        {
            string configured = (Config.WebApiJwtSecret ?? string.Empty).Trim();

            string reason;

            if (configured.Length == 0)
            {
                reason = "未配置 JWT 签名密钥";
            }
            else if (IsLegacyDefault(configured))
            {
                reason = "检测到已公开的默认 JWT 签名密钥";
            }
            else if (Encoding.UTF8.GetByteCount(configured) < MinimumSecretBytes)
            {
                reason = $"配置的 JWT 签名密钥不足 {MinimumSecretBytes} 字节,HS256 无法使用";
            }
            else
            {
                return configured;
            }

            string secret = GenerateSecret();
            string path = GetConfigPath();

            if (IniSecretStore.TryWrite(path, SectionName, SecretKeyName, secret, out string error))
            {
                SEnvir.Log($"WebAPI: {reason},已生成随机 JWT 签名密钥并写入 {path} 的 [{SectionName}] 段。");
            }
            else
            {
                SEnvir.Log($"WebAPI: {reason};随机密钥写入 {path} 失败({error})。本次运行将使用内存临时密钥,重启后所有登录会话失效。请手动在该文件的 [{SectionName}] 段设置 {SecretKeyName}。");
            }

            return secret;
        }

        private static bool IsLegacyDefault(string value)
        {
            return string.Equals(value.Trim(), LegacyDefaultSecret, StringComparison.Ordinal);
        }

        private static string GenerateSecret()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(SecretByteLength));
        }

        private static string GetConfigPath()
        {
            ConfigPath? attribute = typeof(Config).GetCustomAttribute<ConfigPath>();

            string? path = attribute?.Path;

            return string.IsNullOrWhiteSpace(path) ? DefaultConfigPath : path!;
        }
    }
}
