# �ʯ������- ������ Zircon Mir3 Server

����Դ��Ŀ����ѧϰ��Ϸ��������ֹ�����Լ��Ƿ���;��

**����Ŀ��վΪ [Gitee-��ʯ������](https://gitee.com/raphaelcheung/zircon-legend-server.git)������ƽ̨��Ϊ����
�汾����ֻ������վ�ϣ�**

����Ŀ�Ӽ��� 2019 ������������ Zircon �汾��չ�������޸����ϰٸ�Bug��
Ϊ���Ͳ���ɱ����������������˿�ƽ̨��docker�İ汾��
����ԭ�������������ҵ��� **DevExpress** ���Ҳ�֧�ֿ�ƽ̨��
�����ͬ����һͬ�����ȥ�������ݿⱣ����ԭ�湤�߼��ݿɱ༭��
��LOMCN�� 2025 �귢�����°湤�������ع������ݿ⣬�޷����ݣ�ֻ�ܺ��ϰ汾���߼��ݣ�

���ñ���Ŀ�����а����ı����� **�����** ��ף�����귢��ƣ�

���������Լ����鹫������ ��[QQȺ��915941142](https://qm.qq.com/q/JeoJOJ4z4e)��

![ɨ���Ⱥ](Images/QQ%E7%BE%A4.jpg)

## ��Ϸ���

### �����Ĵ�������Ϸ

- �����ĸ�ְҵ��սʿ����ʦ����ʿ���̿�<br/>
<img src="Images/biqi.jpg" title="������ڽ�ͼ"><br/>
<img src="Images/fashi.jpg" title="��ʦ��ͼ"><br/>
<img src="Images/cike.jpg" title="�̿ͽ�ͼ"><br/>
	
- ���ܷḻ��ƽ��ÿ��ְҵ�� 38 ������<br/>
<img src="Images/lianyue.jpg" title="���½�����ͼ"><br/>


- ��ͼ�͵��߼���ḻ���浽 100��ûѹ����

- �������������� 3���Ժ󣬻���ͨ������ߵȼ�������һֱ���� 6����

- ���������ξ��ɾ�����Ʒ�ʸߵ�װ����������Ҳ���ߣ�

- ��ʦ�г����ʿ�ĳ�����߿���������ȼ����������Է������ǳ�ʵ�ã�

- ��ɱ�����Ʒ�֮�࣬���ܵȼ�Խ�ߣ���ɱ�����Ĺ���Խ�죬ˬ֮��ˬ��

### ֧�ֶ�ƽ̨����

�����֧���� Linux��Windows��Docker ƽ̨�ϲ���

<img src="Images/docker.jpg" title="Docker ���н�ͼ">
<br/>
	
### ��ݴ���

ÿ������ʯ�����Է���ش��͵������ͼ��<br/>
<img src="Images/chuansong.jpg" title="Docker ���н�ͼ">
<br/>

## ����������

### ������Ӫ����

�����˵�ͼ���ݱȽϴ�ѹ��֮����Ȼ�н� 800Mb�����ֻ�ܷ��������С�

��[�ٶ�����](https://pan.baidu.com/s/1OMkb834cOtxF8KIrlJMKRQ?pwd=h1bv)��

����Ӱٶ�����̫���������Ӫ������Ҳ���浽�� QQ Ⱥ�ļ��У���[QQȺ��915941142](https://qm.qq.com/q/JeoJOJ4z4e)��

�����˺ţ� **zrf@zrf.zrf��raphael@gm.gm** �������Ϊ  **123456** �� ��ֱ�ӵ�¼���档

����  **raphael@gm.gm**  Ϊ����Ա�˺ţ���������������Ա��ɫ **raphael01��raphael02��raphael03** ����Ա����Ϊ  **123456** ��

ȥ�汾 [����ҳ��](https://gitee.com/raphaelcheung/zircon-legend-server/releases) �������µķ����������ļ�` Server.ini `��������Ҫ�޸ķ����� IP �Լ��˿ڡ�

### ������Ϸ����

- #### �Ƽ��� docker-composer ����

�������ƣ�` raphzhang/zirconlegend:latest `��ÿ�η���������µ� docker��

ע��Ҫ����һ������������Ӫ���ݽ�ѹ��ӳ�䵽������` /zircon/datas `Ŀ¼��

�����ļ�` Server.ini `ͬ��ӳ�䵽������Ŀ¼` /zircon/datas `�¡�

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
      - /etc/localtime:/etc/localtime:ro
      - /etc/timezone:/etc/timezone:ro
version: "3"
networks:
	1panel-network:
		external: true
```

�ɹ����к��ܿ����������������ͱ�ʾ���гɹ���

```
�ʯ������ v1.9.0.25852
��ѿ�Դ�Ĵ����������������������鹫�����QQȺ 915941142
�ͻ��˸���·����
��ͼ�ļ�·����./datas/Map/
��������������ƣ�200
[2025-01-28 14:37:52]: ����������.
[2025-01-28 14:37:52]: Web ����������.
[2025-01-28 14:37:52]: ת����������Ż���True
[2025-01-28 14:37:52]: ���鴬ͨ���ͼ�������
[2025-01-28 14:37:52]: ����֮��ͨ���ͼ�����������
[2025-01-28 14:37:52]: ���غ�ʱ: 3 ��
```

- #### Windows 10 ����

�½�һ��Ӣ������������Ŀ¼���� [����ҳ��](https://gitee.com/raphaelcheung/zircon-legend-server/releases) �������а���ѹ����Ŀ¼�¡�

��Ŀ¼�½���һ����Ŀ¼` datas `��

��ǰ�����ص���Ӫ���ݽ�ѹ��ͬ�����ļ�` Server.ini `������Ŀ¼` datas `�С�

�ù���ԱȨ�����и�Ŀ¼��` Server.exe `��

### �����Զ�����

- Ҫʵ�ֿͻ��˵��Զ����£�Ҫ�� [������](https://gitee.com/raphaelcheung/zircon-legend-launcher)���ͻ��ˡ��������İ汾���� **v1.0.0** ���ϡ�

- ���ȱ�֤��������ֹͣ���е�״̬��

- �Է����� Windows10 Ϊ������` datas `Ŀ¼�´���һ��` Client `Ŀ¼������Ҫ���µĿͻ����ļ����������С�

- �޸�����` Server.ini `��

```
[System]
ClientPath=./datas/Client
```

- �����������󣬻���ɨ�����ɸ����嵥��������������׼�����ˡ�

- �����Ҫ����������` Launcher.exe `��ͬ������` Client `Ŀ¼���ɸ��¡�

### �������

�����������` Nginx `���������Ϸ�����Ӷ�������Ϸ��������ȡ������ʵ�ͻ��� IP��

����Ҫ��` Nginx `�Ͽ���` proxy_protocol `��ͬʱ�򿪷������������
```
[Network]
UseProxy=True
```

�������������ܻ�ȡ��ת����������ʵ IP ��ַ��

### GM ����

��[GM����ʹ��ָ��](GM����ʹ��ָ��.md)��

## �ͻ���

��ȡ�ͻ���ȥ���￴ ��[ZirconLegend-Client](https://gitee.com/raphaelcheung/zircon-legend-client)��

## ������ �������

��������������

- Microsoft Visual Studio Community 2022

- .Net 8.0

��װ��Щ����ȡȫ����롣

��Ŀ��������ģ�飬��ȡ��ʱ��Ҫѡ��` Recursive `��

�������ܰ���ģ��һ����ȡ������

���ɴ��ڳ�������״̬���Ƽ���ȡ�����汾��<br/>
<img src="Images/������ȡ.jpg" title="��ȡ����"><br/>

���������������ģ�飬
���԰ѡ�[ZirconLegend-Library](https://gitee.com/raphaelcheung/zircon-legend-library)����������
Ȼ���ƶ���` Library `Ŀ¼�¡�

��Ŀ�ı�����������Ԥ��ã�ֱ�ӱ��뼴��