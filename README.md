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

[�ٶ���������](���ӣ�https://pan.baidu.com/s/1RflU-PPn5BMoEPL8cOhp1g?pwd=9vqz 
��ȡ�룺9vqz)

�����˺ţ� **zrf@zrf.zrf��raphael@gm.gm** �������Ϊ  **123456** �� ��ֱ�ӵ�¼���档

����  **raphael@gm.gm**  Ϊ����Ա�˺ţ�����Ա����Ϊ  **654321** ��

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
## �ͻ���

��ȡ�ͻ���ȥ���￴ [ZirconLegend-Client](https://gitee.com/raphaelcheung/zircon-legend-client)
