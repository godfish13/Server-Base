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

    void Start()
    {
        // DNS (Domain Name System) : �ּ� �̸����� IP ã�� ���
        string host = Dns.GetHostName();    // �� ��ǻ�� �ּ��� �̸��� �˾Ƴ��� host�� ����
        IPHostEntry ipHost = Dns.GetHostEntry(host);    // �˾Ƴ� �ּ��� ���� ������ ��� Dns�� �˾Ƽ� ����
        IPAddress ipAddr = ipHost.AddressList[0];   // ip�� �������� ����Ʈ�� ��� ������ ���(�����̶����)�� ���� => �ϴ� �츮�� 1���ϱ� ù��°�θ� ���
        IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777); // �̾Ƴ� ip�� ������ ���� �ּ�, port == ���幮 ��ȣ

        Connector connector = new Connector();

        connector.Connect(endPoint, () => { return _session; }, 1);

        StartCoroutine("CoSendPacket");
    }

    void Update()
    {
        IPacket packet = PacketQueue.Instance.Pop();
        if (packet != null)
        {
            PacketManager.Instance.HandlePacket(_session, packet);
        }
    }

    IEnumerator CoSendPacket()
    {
        while(true)
        {
            yield return new WaitForSeconds(3.0f);

            C_Chat chatPacket = new C_Chat();
            chatPacket.chat = "Hi! Im Unity!";
            ArraySegment<byte> segment = chatPacket.WriteBuffer();

            _session.Send(segment);
        }
    }

    private void OnApplicationQuit()
    {
        _session.DisConnect();
    }
}
