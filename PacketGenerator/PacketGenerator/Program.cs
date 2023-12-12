﻿using System;
using System.Xml;

namespace PacketGenerator
{
    class Program
    {
        static string genPackets;    // 실시간으로 계속 생성되는 string 저장해둘 변수

        static void Main(string[] args) 
        {
            XmlReaderSettings settings = new XmlReaderSettings()
            {
                IgnoreComments = true,  // 주석 무시
                IgnoreWhitespace = true,    // 공백 무시
            };

            using (XmlReader r = XmlReader.Create("PDL.xml", settings)) // using으로 감싸면 using을 벗어날때 알아서 XmlReader.Dispose() 발동해줌
            {
                r.MoveToContent();  //헤더문 넘기고 내용물 읽기 시작
                
                while (r.Read()) 
                {
                    if (r.Depth == 1 && r.NodeType == XmlNodeType.Element)  // Depth : 0부터 시작한 계층 구조, xml파일 참조 // Element : </PDL>같은 종료자를 포함하지 않고 내용물만
                    {
                        ParsePacket(r);
                        //Console.WriteLine(r.Name + " " + r["name"]);  
                    }
                }

                File.WriteAllText("GenPackets.cs", genPackets);
            }   
        }

        public static void ParsePacket(XmlReader r)     // 패킷이 정상적인지 판단해줌
        {
            if (r.NodeType == XmlNodeType.EndElement)   // </PDL>같은 종료자면 오류처리
                return;

            if (r.Name.ToLower() != "packet")   // packet이 아니면 오류처리
            {
                Console.WriteLine("err : invalid Packet node");
                return;
            }

            string packetName = r["name"];  // r.Name 아님 주의! r.Name은 type, PDL로 예를들면 pcaket, Long, string 등을 반환함
            if (string.IsNullOrEmpty(packetName))   // packetName이 비어있으면 오류처리
            {
                Console.WriteLine("err : Packet without name");
                return;
            }

            Tuple<string, string, string> t = ParseMembers(r);
            genPackets += string.Format(PacketFormat.packetFormat, packetName, t.Item1, t.Item2, t.Item3);
            // packetFormat에 들어갈 {0}, {1}, {2}, {3}을 지정해서 string을 만들기
        }

        // packetFormat의 {1}{2}{3}과 동일
        // {1} 멤버 변수들      //tuple.Item1
        // {2} 멤버 변수 Read   //tuple.Item2
        // {3} 멤버 변수 Write  //tuple.Item3
        public static Tuple<string, string, string> ParseMembers(XmlReader r)    // 패킷의 내용물이 정상적인지 판단하고 멤버의 타입에 따라 작동
        {
            string packetName = r["name"];

            string memberCode = "";
            string readCode = "";
            string writeCode = "";

            int depth = r.Depth + 1;    // playerInfoRequirement 내의 목록들을 원함
            while(r.Read())     // r의 content를 순차적으로 읽어감
            {
                if (r.Depth != depth)   // 목표 목록들을 빠져나가면 break
                    break;

                string memberName = r["name"];
                if(string.IsNullOrEmpty(memberName))
                {
                    Console.WriteLine("err : Member without name");
                    return null;
                }

                if (string.IsNullOrEmpty(memberCode) == false)
                    memberCode += Environment.NewLine;  // Environment.NewLine : 한줄 이후 줄넘기기(엔터)해주는 코드
                if (string.IsNullOrEmpty(readCode) == false)
                    readCode += Environment.NewLine;
                if (string.IsNullOrEmpty(writeCode) == false)
                    writeCode += Environment.NewLine;

                string memberType = r.Name.ToLower();   // 오류 방지를 위해 type명들 소문자화 시켜줌
                switch (memberType) 
                {
                    case "bool":
                    case "byte":
                    case "short":
                    case "ushort":
                    case "int":
                    case "long":
                    case "float":
                    case "double":
                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName); // memberFormat, {0}, {1}
                        readCode += string.Format(PacketFormat.readFormat, memberName, ToMemberType(memberType), memberType); // readFormat, {0}, {1}, {2}
                        writeCode += string.Format(PacketFormat.writeFormat, memberName, memberType); // writeFormat, {0}, {1}
                        break;
                    case "string":
                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
                        readCode += string.Format(PacketFormat.readStringFormat, memberName);
                        writeCode += string.Format(PacketFormat.writeStringFormat, memberName);
                        break;
                    case "list":
                        break;
                    default:
                        break;
                }
            }

            memberCode = memberCode.Replace("\n", "\n\t");  // 다음줄로 넘긴 이후 탭이 없는 경우 서식이 보기불편함으로 강제적으로 탭을 넣어줌
            readCode = readCode.Replace("\n", "\n\t\t");    // 마찬가지로 ReadBuffer안 내용에 탭2번 넣어줌
            writeCode = writeCode.Replace("\n", "\n\t\t");
            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
        }

        public static string ToMemberType(string memberType)    // 받은 형식을 BitConverter에서 사용하기 위해 To~~의 형태로 바꿔주는 함수
        {
            switch (memberType)
            {
                case "bool":
                    return "ToBoolean";
                case "short":
                    return "ToInt16";
                case "ushort":
                    return "ToUInt16";
                case "int":
                    return "ToInt32";
                case "long":
                    return "ToInt64";
                case "float":
                    return "ToSingle";
                case "double":
                    return "ToDouble";
                default:
                    return "";
            }
        }
    }
}