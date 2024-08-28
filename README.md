# 皓石传奇三  Zircon Mir3

本开源项目仅供学习游戏技术，禁止商用以及非法用途。

## 简介

### 完整的传奇三游戏

- 含了四个职业：战士、法师、道士、刺客<br/>
<img src="Images/biqi.jpg" title="比奇城内截图"><br/>
<img src="Images/fashi.jpg" title="法师截图"><br/>
<img src="Images/cike.jpg" title="刺客截图"><br/>
	
- 技能丰富，平均每个职业有 38 个技能<br/>
<img src="Images/lianyue.jpg" title="莲月剑法截图"><br/>


- 地图和道具及其丰富，玩到 100级没压力；

- 技能正常修炼到 3级以后，还可通过打出高等级技能书一直升到 6级；

- 武器和首饰均可精炼，品质高的装备精炼上限也更高；

- 法师招宠与道士的宠物最高可升至暗金等级，各项属性翻倍，非常实用；

- 刺杀剑术破防之余，技能等级越高，刺杀剑术的攻速越快，爽之又爽；

### 支持多平台部署

服务端支持在 Linux、Windows、Docker 平台上部署。

<img src="Images/docker.jpg" title="Docker 运行截图">
<br/>
	
### 便捷传送

每个传送石都可以方便地传送到任意地图。<br/>
<img src="Images/chuansong.jpg" title="Docker 运行截图">
<br/>

## 服务器部署

### 下载运营数据

包含了地图数据比较大，压缩之后仍然有近 800Mb，因此只能放在网盘中。

【[百度网盘 2024-8-15](https://pan.baidu.com/s/1OMkb834cOtxF8KIrlJMKRQ?pwd=h1bv)】

内置账号： **zrf@zrf.zrf、raphael@gm.gm** ，密码均为  **123456** ， 可直接登录游玩。

其中  **raphael@gm.gm**  为管理员账号，包含了三个管理员角色 **raphael01、raphael02、raphael03** 管理员密码为  **654321** 。

去版本 [发布页面](https://gitee.com/raphaelcheung/zircon-legend-server/releases) 下载最新的服务器配置文件` Server.ini `，根据需要修改服务器 IP 以及端口。

### 部署游戏服务

- #### 推荐用 docker-composer 部署。

注意要将上一步下载来的运营数据解压后映射到容器的` /zircon/datas `目录。

配置文件` Server.ini `同样映射到容器的目录` /zircon/datas `下。

```
services:
	zircon:
		container_name: zircon
		image: raphzhang/zirconlegend:latest
		networks:
			1panel-network:
				ipv4_address: 172.18.0.82
		ports:
			- 192.168.0.3:17000:7000
		restart: unless-stopped
		user: "0:0"
		volumes:
			- ./datas:/zircon/datas
version: "3"
networks:
	1panel-network:
		external: true
```

成功运行后能看到类似下面的输出就表示运行成功：

```
皓石传奇三 v0.1.0.0
免费开源的传奇三，有疑问请联系开源志愿者：QQ50181976
版本文件路径：./datas/Legend.exe
地图文件路径：./datas/Map/
[Sunday, 11 August 2024 03:30:51]: Network Started.
[Sunday, 11 August 2024 03:30:51]: Web Server Started.
[Sunday, 11 August 2024 03:30:51]: Loading Time: 2 Seconds
```

- #### Windows 10 部署

新建一个英文名服务器根目录，从 [发布页面](https://gitee.com/raphaelcheung/zircon-legend-server/releases) 下载运行包解压到根目录下。

根目录下建立一个子目录` datas `。

把前面下载的运营数据解压连同配置文件` Server.ini `放入子目录` datas `中。

用管理员权限运行根目录的` Server.exe `。

### GM 管理

登录的时候，账号那里填写管理员账号，密码要使用管理员密码进行登录，才会具备 GM 权限。


以下是 GM 管理员命令：

```
@TAKECASTLE [城堡指数]
@FORCEENDWAR [城堡指数] 参数1  结束攻城
@FORCEWAR [城堡指数]    参数1   开始
@CLEARBELT
@MAP [地图名]
@GLOBALBAN [角色名] [*持续时间]
@CHATBAN [角色名] [*持续时间]    禁止聊天
@REFUNDHUNTGOLD [角色] [数量]   狩猎金币
@REFUNDGAMEGOLD  [角色] [数量]    奖励游戏币
@TAKEGAMEGOLD  [角色] [数量]      移除游戏币
@REMOVEGAMEGOLD [角色] [数量]  因为支付失败扣除游戏币
@GIVEGAMEGOLD  [角色] [数量]   成功购买 游戏币
@REBOOT   重启
@GCCOLLECT   gc收集
@MAKE [物品名称] [数量]
@SETCOMPANIONVALUE [Level] [Stat] [Value]
@GIVESKILLS [角色名]   给与全部技能
@GOTO [角色名]
@LEVEL [角色名|  Level] [*Level]  调整自身等级
@ITEMBOT [角色名]
@GOLDBOT [角色名]
@GAMEMASTER    
@OBSERVER      退出 进入隐身模式
@RECALL [角色名]    召唤到身边
@LEAVEGUILD
@ALLOWGUILD
@BLOCKWHISPER
@ALLOWTRADE
@ENABLELEVEL3
@ENABLELEVEL5
@ENABLELEVEL7
@ENABLELEVEL10
@ENABLELEVEL11
@ENABLELEVEL13
@ENABLELEVEL15
@EXTRACTORLOCK
@ROLL [Amount]
```

## 客户端

获取客户端去这里看 【[ZirconLegend-Client](https://gitee.com/raphaelcheung/zircon-legend-client)】

## 代码编译

开发环境依赖：

- Microsoft Visual Studio Community 2022

- .Net 8.0

安装这些后拉取全库代码。

项目包含了子模块，拉取的时候要选中` Recursive `。

这样才能把子模块一并拉取下来。

项目的编译依赖都已预设好，直接编译即可