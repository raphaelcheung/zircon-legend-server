## 管理员身份说明

- GM密码 配置在服务器的文件` ./datas/Server.ini `中；

- 使用 GM账号 + GM密码 登录为 **正式管理员**，登录后默认打开了 GM状态；

- 使用 GM账号 + 账号密码 登录为 **临时管理员**，登陆后默认关闭GM状态；

- 正式管理员和临时管理员都可以用 @GM 切换GM状态，GM状态下怪物不会攻击自己；

## 仅正式管理员可以使用的命令
```
@TAKECASTLE [城堡指数]
@FORCEENDWAR [城堡指数] 参数1       #结束攻城
@FORCEWAR [城堡指数]    参数1            #开始攻城
@GLOBALBAN [角色名] [*持续时间]
@CHATBAN [角色名] [*持续时间]             #禁止聊天
@REFUNDHUNTGOLD [角色] [数量]        #退还狩猎币
@REFUNDGAMEGOLD  [角色] [数量]       #退还游戏币
@TAKEGAMEGOLD  [角色] [数量]            #给予游戏币
@REMOVEGAMEGOLD [角色] [数量]       #扣除游戏币
@GIVEGAMEGOLD  [角色] [数量]              #给与游戏币
@REBOOT   重启
@GCCOLLECT   gc收集
@SETCOMPANIONVALUE [Level] [Stat] [Value]
@ITEMBOT [角色名]
@GOLDBOT [角色名]
@OBSERVER                         #切换观察者模式
@ROLL [Amount]
@ADMIN 账号 true/false      #在线设置账号的管理员权限（临时管理员），这里设置的权限也会永久保存
@修改密码 账号 新密码        #修改指定账号的密码
@禁止登录 账号 封禁秒数    #在指定时间内禁止账号登录，秒数为0表示解禁
@恢复误删 角色名                #恢复误删角色
@重载更新                            #重新加载客户端更新目录下的文件
@屏蔽物品掉落 物品名 true/false   #设置为true后，该物品不会从任何怪物身上掉落
@保存数据库                        #将内存中的数据库保存到文件，比如屏蔽物品掉落的改动、地图怪物倍率的改动等
@怪物倍率                            #显示当前地图怪物的倍率数据
@怪物倍率 生命倍率 最大生命倍率 伤害倍率 最大伤害倍率 经验倍率 最大经验倍率 掉落倍率 最大掉落倍率 金币倍率 最大金币倍率   #修改当前地图的怪物倍率数据
@清理怪物                            #清除当前地图上所有的怪物，过一会地图会自动刷新新的怪物
@开启怪物攻城                     #会全服发出当前GM所在地图的攻城通知，当前地图的卫士会全部隐藏
@结束怪物攻城                     #全服发出怪物攻城结束的通知，当前地图的卫士正常出现
@怪物攻城 怪物名 数量 范围   #在GM所在位置的指定范围内刷新相应数量的怪物，BOSS则只能在怪物攻城状态下才能刷
@内存统计                             #将当前的用户数据统计出来并写入到` ./datas/block_devices.txt `中以便分析
@怪物数值 [mr=?,?] [ac=?,?] [hp=?] [gc=?,?]  #设置怪物的属性，一次可以设多个属性，可以只设置一个属性，设置后只影响新刷出来的怪物
@死亡经验优化			#切换转生死亡经验优化配置开关，true 是死亡掉落一半经验，幸运儿直升一级；false 是死亡全掉，掉落经验全部加给幸运儿
@清理已删角色 最近登录天数  #清理最近登录超过天数的已删角色
@怪物数值 HP=最小值,最大值 AC=最小值,最大值 MR=最小值,最大值 DC=最小值,最大值   #调整怪物的基础数值
@道具呼唤怪物时长 分钟   #用道具召唤出来的怪物存活超出时间后会被自动清理
@调整怪物种族 怪物名称 CanTame/Undead true/false  #设置怪物种族，CanTame为生物，Undead是不死，两者都false是恶魔
```

## 临时管理员开GM状态时才能用的（正式管理员随时可用）
```
@MAKE 物品名称 [数量]	#正式管理员创建的物品可以赠送，可以交易；临时管理员不可赠送和交易
@GIVESKILLS [角色名]         #给与全部技能，正式管理员可以对任何人使用，临时管理员只能对自己使用
@LEVEL [角色名]   等级        #调整指定角色等级，正式管理员可以对任何人使用，临时管理员只能对自己使用
!@   #发布红色系统消息
@!  #发布白底蓝字的通告
```

## 所有管理员不用开GM状态也能使用的命令
```
@MAP 地图名		#飞到该地图随机的位置
@GOTO 角色名		#飞到指定角色身边
@RECALL 角色名		#召唤到身边
@GM    				#切换GM状态
@在线统计                  	#查看在线人数，在线设备数
@在线角色            		#列出所有在线的角色名称
@监控在线			#打开后，任何角色上下线自己都会收到消息
@角色关联 [角色名]	#如果没写角色名，会将所有在线角色根据设备号分类输出；如果写了角色名则只列出该角色相同设备的角色
@找怪物 怪物名                    #寻找一只不在可视范围内的怪物，并传送到怪物旁边
```
