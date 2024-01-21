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
        // DNS (Domain Name System) : 주소 이름으로 IP 찾는 방식
        string host = Dns.GetHostName();    // 내 컴퓨터 주소의 이름을 알아내고 host에 저장
        IPHostEntry ipHost = Dns.GetHostEntry(host);    // 알아낸 주소의 여러 정보가 담김 Dns가 알아서 해줌
        IPAddress ipAddr = ipHost.AddressList[0];   // ip는 여러개를 리스트로 묶어서 보내는 경우(구글이라던가)가 많음 => 일단 우리는 1개니깐 첫번째로만 사용
        IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777); // 뽑아낸 ip를 가공한 최종 주소, port == 입장문 번호

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
