﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server_Base
{
    internal class FileName
    {
        static void Main(string[] args)
        {
            // DNS (Domain Name System) : 주소 이름으로 IP 찾는 방식
            string host = Dns.GetHostName();    // 내 컴퓨터 주소의 이름을 알아내고 host에 저장
            IPHostEntry ipHost = Dns.GetHostEntry(host);    // 알아낸 주소의 여러 정보가 담김 Dns가 알아서 해줌
            IPAddress ipAddr = ipHost.AddressList[0];   // ip는 여러개를 리스트로 묶어서 보내는 경우(구글이라던가)가 많음 => 일단 우리는 1개니깐 첫번째로만 사용
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777); // 뽑아낸 ip를 가공한 최종 주소, port == 입장문 번호

            // 문지기가 든 휴대폰 (listen Socket)
            Socket listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                // 문지기 교육 (Bind)
                listenSocket.Bind(endPoint);

                // 영업 시작
                listenSocket.Listen(10);    // backlog == 최대 대기 수

                while (true)
                {
                    Console.WriteLine("Listening...");

                    // 손님의 문의가 오면 입장시키기
                    Socket clientSocket = listenSocket.Accept();

                    // 받는다
                    byte[] receiveBuffer = new byte[1024];
                    int receiveByte = clientSocket.Receive(receiveBuffer);  // clientSocket에서 받은 데이터 receiveBuffer에 저장
                    string receiveData = Encoding.UTF8.GetString(receiveBuffer, 0, receiveByte); // (문자열, 문자열이 시작되는 index, 문자의 갯수)
                    Console.WriteLine($"[From Clien] : {receiveData}");            // 서버상 보내려면 인코딩 맞춰야함 여기선 UTF8 사용

                    // 보낸다
                    byte[] senfBuff = Encoding.UTF8.GetBytes("Welcome to MMO RPG Server!!");
                    clientSocket.Send(senfBuff);

                    // 손님 내보낸다
                    clientSocket.Shutdown(SocketShutdown.Both); // 양쪽 모두 서로 대화 나눌것 없다고 공지
                    clientSocket.Close();     // 대화끝
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }          
        }      
    }
}