# �ʯ������

<br/>
�����Ĵ�������Ϸ���������ĸ�ְҵ��սʿ����ʦ����ʿ���̿ͣ��� 146 �����ܣ�ƽ��ÿ��ְҵ�� 38 �����ܡ�
<br/>
<img src="Images/biqi.jpg" title="������ڽ�ͼ"><br/>
<img src="Images/fashi.jpg" title="��ʦ��ͼ"><br/>
<img src="Images/lianyue.jpg" title="���½�����ͼ"><br/>
<img src="Images/cike.jpg" title="�̿ͽ�ͼ"><br/>
<br/>
�����֧�� Linux��Windows��Docker ����<br/>
<img src="Images/docker.jpg" title="Docker ���н�ͼ">
<br/>
ÿ������ʯ�����Է���ش��͵������ͼ��<br/>
<img src="Images/chuansong.jpg" title="Docker ���н�ͼ">
<br/>

## ����������

### ������Ӫ����

�����˵�ͼ���ݱȽϴ�ѹ��֮����Ȼ�н� 800MB�����ֻ�ܷ��������С�

[�ٶ����̷��� ��ȡ�룺9vqz](���ӣ�https://pan.baidu.com/s/1RflU-PPn5BMoEPL8cOhp1g?pwd=9vqz)

�����˺ţ� **zrf@zrf.zrf��raphael@gm.gm** �������Ϊ  **123456** �� ��ֱ�ӵ�¼���档

����  **raphael@gm.gm**  Ϊ����Ա�˺ţ���������������Ա��ɫ **raphael01��raphael02��raphael03** ����Ա����Ϊ  **654321** ��


### ������Ϸ����

�Ƽ��� docker-composer ����

ע��Ҫ����һ������������Ӫ���ݽ�ѹ��ӳ�䵽������ /zircon/datas Ŀ¼��

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

 **�ɹ����к��ܿ������������** 

```
�ʯ������ v0.1.0.0
��ѿ�Դ�Ĵ�����������������ϵ��Դ־Ը�ߣ�QQ50181976
�汾�ļ�·����./datas/Legend.exe
��ͼ�ļ�·����./datas/Map/
[Sunday, 11 August 2024 03:30:51]: Network Started.
[Sunday, 11 August 2024 03:30:51]: Web Server Started.
[Sunday, 11 August 2024 03:30:51]: Loading Time: 2 Seconds
```

### GM ����

��¼��ʱ���˺�������д��Ϸ��ɫ���֣�ע�⣬����**��ɫ����**��Ȼ������ʹ�ù���Ա������е�¼���Ż�߱� GM Ȩ�ޡ�

������ GM ����Ա���

```
@TAKECASTLE [�Ǳ�ָ��]
@FORCEENDWAR [�Ǳ�ָ��] ����1  ��������
@FORCEWAR [�Ǳ�ָ��]    ����1   ��ʼ
@CLEARBELT
@MAP [��ͼ��]
@GLOBALBAN [��ɫ��] [*����ʱ��]
@CHATBAN [��ɫ��] [*����ʱ��]    ��ֹ����
@REFUNDHUNTGOLD [��ɫ] [����]   ���Խ��
@REFUNDGAMEGOLD  [��ɫ] [����]    ������Ϸ��
@TAKEGAMEGOLD  [��ɫ] [����]      �Ƴ���Ϸ��
@REMOVEGAMEGOLD [��ɫ] [����]  ��Ϊ֧��ʧ�ܿ۳���Ϸ��
@GIVEGAMEGOLD  [��ɫ] [����]   �ɹ����� ��Ϸ��
@REBOOT   ����
@GCCOLLECT   gc�ռ�
@MAKE [��Ʒ����] [����]
@SETCOMPANIONVALUE [Level] [Stat] [Value]
@GIVESKILLS [��ɫ��]   ����ȫ������
@GOTO [��ɫ��]
@LEVEL [��ɫ��|  Level] [*Level]  ��������ȼ�
@ITEMBOT [��ɫ��]
@GOLDBOT [��ɫ��]
@GAMEMASTER    
@OBSERVER      �˳� ��������ģʽ
@RECALL [��ɫ��]    �ٻ������
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

## �ͻ���

��ȡ�ͻ���ȥ���￴ [ZirconLegend-Client](https://gitee.com/raphaelcheung/zircon-legend-client)

## �������

��������������

- Microsoft Visual Studio Community 2022

- .Net 8.0

��װ��Щ����ȡȫ����룬Ҫע�������Ŀ��������ģ�飬��ȡ��ʱ��Ҫѡ�� `Recursive`��

�������ܰ���ģ��һ����ȡ��������Ŀ�ı�����������Ԥ��ã�ֱ�ӱ��뼴��