using DummyClient;
using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class NetworkMgr : MonoBehaviour
{
    ServerSession _session = new ServerSession();

    public void Send(ArraySegment<byte> sendBuff)
    {
        _session.Send(sendBuff);
    }

    void Start()
    {
        // DNS (Domain Name System) : �ּ� �̸����� IP ã�� ���
        string host = Dns.GetHostName();    // �� ��ǻ�� �ּ��� �̸��� �˾Ƴ��� host�� ����
        IPHostEntry ipHost = Dns.GetHostEntry(host);    // �˾Ƴ� �ּ��� ���� ������ ��� Dns�� �˾Ƽ� ����
        IPAddress ipAddr = ipHost.AddressList[0];   // ip�� �������� ����Ʈ�� ��� ������ ���(�����̶����)�� ���� => �ϴ� �츮�� 1���ϱ� ù��°�θ� ���
        IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777); // �̾Ƴ� ip�� ������ ���� �ּ�, port == ���幮 ��ȣ

        Connector connector = new Connector();

        connector.Connect(endPoint, () => { return _session; });
    }

    void Update()
    {
        /*IPacket packet = PacketQueue.Instance.Pop();
        if(packet != null) 
        {
            PacketManager.Instance.HandlePacket(_session, packet);
        }*/

        List<IPacket> list = PacketQueue.Instance.PopAll();
        foreach (IPacket packet in list)
            PacketManager.Instance.HandlePacket(_session, packet);
    }

    private void OnApplicationQuit()
    {
        _session.DisConnect();
    }
}
