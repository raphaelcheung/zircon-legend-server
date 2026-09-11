# Webhook 通知使用指南
---

## 一、功能简介

Webhook 通知功能可以在重要的运营事件发生时，由游戏服务器主动向你配置的接收地址发起一次 HTTP POST 请求（JSON 格式），让你第一时间掌握服务器动态。

**支持的事件：**

| 事件 | 事件类型标识 | 触发条件 |
|------|-------------|---------|
| 定时在线人数通知 | `online_count_periodic` | 每隔 N 分钟（可配置）自动上报一次当前在线人数 |
| 在线超过阈值通知 | `online_threshold_high` | 在线人数上升突破高阈值时触发一次 |
| 在线低于阈值通知 | `online_threshold_low` | 在线人数跌破低阈值时触发一次 |
| 后台登录成功通知 | `admin_login_success` | 有人成功登录 Web 管理后台时触发 |
| 后台登录失败通知 | `admin_login_fail` | 有人尝试登录后台失败时触发（含失败原因） |

**特点：**
- 全局只有一个接收 URL 和一个总开关，不做按事件的细分控制，配置简单。
- 默认关闭、URL 默认为空——升级后不会产生任何外送行为，需要主动开启。
- 通知发送为异步执行（5 秒超时，失败不重试），绝不影响游戏主循环和后台登录的响应速度。
- 通知内容不含任何密码、密码哈希等敏感凭据。

---

## 二、快速上手（三步完成）

### 第 1 步：准备一个接收地址

你需要一个能接收 HTTP POST 请求（Content-Type: `application/json`）的 URL 作为事件接收端，例如：

- 自建的 HTTP 服务（最灵活，可自由处理 JSON 报文）
- 一层轻量转发服务：接收本服务器的 JSON 报文，转换为钉钉/企业微信/飞书/Telegram 机器人的消息格式后转发（注意：各类 IM 机器人对报文格式有固定要求，本功能推送的是自定义 JSON，**不能直接**填机器人地址，需经转发转换）

> 接收端返回 HTTP 2xx 状态码即视为发送成功；返回其他状态码或 5 秒内无响应，本次发送即告失败（只记日志，不重试）。

### 第 2 步：填写配置

有两种方式，任选其一：

**方式 A：直接编辑配置文件**（需重启服务器生效）

打开 `./datas/Server.ini`，在文件末尾追加或修改 `[WebHook]` 节：

```ini
[WebHook]
WebHookEnabled=True
WebHookUrl=http://你的接收地址/webhook
WebHookIntervalMinutes=5
WebHookHighThreshold=0
WebHookLowThreshold=0
```

保存后重启服务器。

**方式 B：通过管理后台热更新**（立即生效，无需重启）

登录 Web 管理后台（需超级管理员账号），调用"修改运行时配置"接口逐项更新：

```bash
# 开启总开关
curl -X PUT http://服务器地址:7080/api/config/runtime \
  -H "Authorization: Bearer <你的Token>" \
  -H "Content-Type: application/json" \
  -d '{"key":"webhookenabled","value":"true"}'

# 设置接收 URL
curl -X PUT http://服务器地址:7080/api/config/runtime \
  -H "Authorization: Bearer <你的Token>" \
  -H "Content-Type: application/json" \
  -d '{"key":"webhookurl","value":"http://你的接收地址/webhook"}'
```

### 第 3 步：验证

配置完成后观察服务器控制台日志（也可通过后台"系统日志"接口查看），搜索 `[WebHook]` 关键字：

```
[WebHook] 发送成功 类型=online_count_periodic 状态=200
```

看到"发送成功"即表示整条链路已打通。

---

## 三、配置项详解

| 配置键 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `WebHookEnabled` | 开关 | `False` | **总开关**。关闭时所有事件一律不外送 |
| `WebHookUrl` | 文本 | 空 | 事件接收地址，须为 `http://` 或 `https://` 开头的合法 URL |
| `WebHookIntervalMinutes` | 整数 | `5` | 定时在线人数通知的间隔（分钟），最小 1 分钟，配置 0 或负数时按 1 分钟生效 |
| `WebHookHighThreshold` | 整数 | `0` | 在线人数高阈值。`0` 或负数表示不启用"超过阈值"通知 |
| `WebHookLowThreshold` | 整数 | `0` | 在线人数低阈值。`0` 或负数表示不启用"低于阈值"通知 |

**后台热更新对应的 key（大小写不敏感）：** `webhookenabled` / `webhookurl` / `webhookintervalminutes` / `webhookhighthreshold` / `webhooklowthreshold`

所有配置修改**立即生效**（方式 B 无需重启；方式 A 重启后读取）。修改后台配置需要超级管理员权限。

---

## 四、事件报文说明

所有通知均为 HTTP POST，请求头 `Content-Type: application/json`，请求体为 JSON。`timestamp` 为 UTC 时间戳。

### 1. 定时在线人数通知（`online_count_periodic`）

服务器启动后，经过**第一个完整间隔**才发出首次通知（不会在启动瞬间立即发送），之后每隔 `WebHookIntervalMinutes` 分钟发送一次。

```json
{
  "eventType": "online_count_periodic",
  "timestamp": "2026-09-11T08:30:00Z",
  "onlineCount": 42
}
```

### 2. 在线超过阈值通知（`online_threshold_high`）

在线人数**由不高于高阈值上升至超过高阈值**时触发一次（`direction` 为 `"up"`）。人数在阈值之上继续波动不会重复触发；只有人数回落到阈值及以下后再次突破，才会再次通知。

```json
{
  "eventType": "online_threshold_high",
  "timestamp": "2026-09-11T08:35:00Z",
  "currentCount": 45,
  "threshold": 40,
  "direction": "up"
}
```

### 3. 在线低于阈值通知（`online_threshold_low`）

在线人数**由不低于低阈值下降至低于低阈值**时触发一次（`direction` 为 `"down"`）。防抖规则与超过阈值通知相同（对称）。

```json
{
  "eventType": "online_threshold_low",
  "timestamp": "2026-09-11T12:00:00Z",
  "currentCount": 3,
  "threshold": 5,
  "direction": "down"
}
```

> 阈值判定与定时人数通知共用同一个采样节奏（每 N 分钟采样一次），不额外增加采样频率。服务器重启后防抖状态重置——若重启时人数本就高于高阈值，首次采样即会触发一次通知，属预期行为。

### 4. 后台登录成功通知（`admin_login_success`）

```json
{
  "eventType": "admin_login_success",
  "timestamp": "2026-09-11T09:00:00Z",
  "email": "admin@example.com",
  "ip": "203.0.113.10"
}
```

### 5. 后台登录失败通知（`admin_login_fail`）

`failReason` 为离散的失败原因，取以下四种之一：

| failReason 值 | 含义 |
|---------------|------|
| `account_not_found` | 账号不存在 |
| `password_error` | 密码错误 |
| `insufficient_permission` | 账号存在且密码正确，但权限不足 |
| `account_banned` | 账号已被封禁 |

```json
{
  "eventType": "admin_login_fail",
  "timestamp": "2026-09-11T09:01:23Z",
  "email": "hacker@example.com",
  "ip": "198.51.100.7",
  "failReason": "password_error"
}
```

> 登录失败通知可用于发现撞库或暴力破解尝试。通知中只含账号邮箱与来源 IP，**绝不含密码明文或哈希**。

---

## 五、行为规则与注意事项

1. **总开关优先**：`WebHookEnabled=False` 时，任何事件都不发送，无论其他配置如何。
2. **URL 校验**：开关开启但 URL 为空时，事件触发会记录 `[WebHook] URL 未配置` 日志；URL 非法（非 http/https）时记录 `[WebHook] URL 格式非法`。两种情况均不会发起请求。
3. **防抖机制**：阈值通知只在真正穿越阈值的瞬间发送一次，避免人数在阈值附近抖动产生通知风暴。
4. **发送保障**：单次发送 5 秒超时；失败（网络错误/超时/非 2xx）不自动重试，仅写日志。接收方故障不会拖垮游戏服务器。
5. **无事件专属开关**：所有事件共用一个总开关和一个 URL，无法只开启某类事件——这是有意设计，保持配置简单。
6. **与游戏内广播互不影响**：现有的游戏内在线人数广播（向管理员发聊天提示）不受本功能影响，二者独立运行。

---

## 六、常见问题

**Q1：配置了但收不到通知？**

按以下清单排查：
1. `WebHookEnabled` 是否为 `True`？
2. `WebHookUrl` 是否已填写且以 `http://` 或 `https://` 开头？
3. 控制台/系统日志中搜索 `[WebHook]`：
   - 无任何记录 → 事件未触发（如定时通知需等第一个完整间隔；阈值通知需真正穿越阈值）
   - `URL 未配置` / `URL 格式非法` → 检查 URL 配置
   - `发送超时` / `发送失败` → 接收端不可达或返回了非 2xx，排查接收端
   - `发送成功` → 接收端已收到，问题在接收端之后环节
4. 阈值通知：确认阈值 > 0，且人数是**跨越**阈值（如高阈值 40，人数从 39 变 41 才触发；从 45 变 44 不触发）。

**Q2：可以直接填钉钉/飞书机器人的地址吗？**

不建议直接填写。IM 机器人要求特定的报文格式（如钉钉的 `msgtype`/`content` 结构），本功能推送的是自定义 JSON。请自建一个轻量转发服务做格式转换后再调用机器人。

**Q3：通知内容安全吗？会不会泄露密码？**

不会。通知体只包含事件类型、时间戳、在线人数/阈值/登录邮箱/来源 IP 等运营信息，代码层面严格禁止携带密码、密码哈希、JWT 密钥等凭据。

**Q4：如何临时停用通知？**

把 `WebHookEnabled` 置为 `False`（后台热更新或改 INI 重启均可），立即停止所有外送。

**Q5：修改间隔/阈值后什么时候生效？**

立即生效。定时器按新间隔计算下一个采样点；阈值在下一次采样（即下个定时周期）时按新值判定。

**Q6：发送失败会丢事件吗？**

会。失败不重试，事件不落盘、不排队。这是为了在接收方故障期间不产生请求堆积。可通过日志 `[WebHook]` 记录追溯失败事实。

---

## 附：日志速查

| 日志内容 | 含义 |
|---------|------|
| `[WebHook] 发送成功 类型=xx 状态=200` | 发送成功 |
| `[WebHook] 发送失败 类型=xx 状态=500` | 接收端返回非 2xx |
| `[WebHook] 发送超时 类型=xx` | 接收端 5 秒内未响应 |
| `[WebHook] 发送异常 类型=xx err=...` | 网络错误等异常 |
| `[WebHook] URL 未配置` | 开关开启但 URL 为空 |
| `[WebHook] URL 格式非法` | URL 不是合法的 http/https 地址 |