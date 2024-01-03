using System;
using System.Xml;

namespace PacketGenerator   // ServerSession.cs등 패킷을 자동으로 생성하기 위한 스크립트
{
    class Program       // ServerSession.cs 등 완성시켜둔 스크립트 참조
    {
        static string genPackets;    // 실시간으로 계속 생성되는 string 저장해둘 변수

        static ushort packetID;
        static string packetEnums;

        static string clientRegister;   // 서버가 클라에게 보내는 패킷(S_~~~) packet register
        static string serverRegister;   // 클라가 서버에게 보내는 패킷(C_~~~) packet register

        static void Main(string[] args) 
        {
            string pdlPath = "../../PDL.xml";       // ../ 를 경로에 넣으면 현재 경로에서 한칸 뒤를 의미함

            XmlReaderSettings settings = new XmlReaderSettings()
            {
                IgnoreComments = true,  // 주석 무시
                IgnoreWhitespace = true,    // 공백 무시
            };

            if (args.Length >= 1)
                pdlPath = args[0];

            using (XmlReader r = XmlReader.Create(pdlPath, settings)) // using으로 감싸면 using을 벗어날때 알아서 XmlReader.Dispose() 발동해줌
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

                string fileText = string.Format(PacketFormat.fileFormat, packetEnums, genPackets);
                File.WriteAllText("GenPackets.cs", fileText);
                string clientManagerText = string.Format(PacketFormat.managerFormat, clientRegister);
                File.WriteAllText("ClientPacketManager.cs", clientManagerText);
                string serverManagerText = string.Format(PacketFormat.managerFormat, serverRegister);
                File.WriteAllText("ServerPacketManager.cs", serverManagerText);
            }   
        }

        public static void ParsePacket(XmlReader r)     // 패킷이 정상적인지 판단하고 패킷 내용 분석 후 genPackets/packetManager에 저장
        {
            if (r.NodeType == XmlNodeType.EndElement)   // </PDL>같은 종료자면 오류처리
                return;

            if (r.Name.ToLower() != "packet")   // packet이 아니면 오류처리
            {
                Console.WriteLine("err : invalid Packet node");
                return;
            }

            string packetName = r["name"];  // r.Name 아님 주의! r.Name은 type, PDL로 예를들면 packet, Long, string 등을 반환함
            if (string.IsNullOrEmpty(packetName))   // packetName이 비어있으면 오류처리
            {
                Console.WriteLine("err : Packet without name");
                return;
            }

            Tuple<string, string, string> t = ParseMembers(r);
            genPackets += string.Format(PacketFormat.packetFormat, packetName, t.Item1, t.Item2, t.Item3);
            // packetFormat에 들어갈 {0}, {1}, {2}, {3}을 지정해서 string을 만들기
            packetEnums += string.Format(PacketFormat.packetEnumFormat, packetName, ++packetID) + Environment.NewLine + "\t";

            if (packetName.StartsWith("S_") || packetName.StartsWith("s_"))
                clientRegister += string.Format(PacketFormat.managerRegisterFormat, packetName) + Environment.NewLine;
            else
                serverRegister += string.Format(PacketFormat.managerRegisterFormat, packetName) + Environment.NewLine;
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

            int depth = r.Depth + 1;    // playerInfoRequirement 내의 컨텐츠들을 원함
            while(r.Read())     // r(playerInfoRequirement)의 content를 순차적으로 읽어감
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
                    case "byte":
                    case "sbyte":
                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
                        readCode += string.Format(PacketFormat.readByteFormat, memberName, memberType);
                        writeCode += string.Format(PacketFormat.writeByteFormat, memberName, memberType);
                        break;
                    case "bool":                    
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
                        Tuple<string, string, string> t = ParseList(r);
                        memberCode += t.Item1;
                        readCode += t.Item2;
                        writeCode += t.Item3;
                        break;
                    default:
                        break;
                }
            }

            memberCode = memberCode.Replace("\n", "\n\t");  // 다음줄로 넘긴 이후 탭이 없는 경우 서식이 보기불편함으로 강제적으로 탭을 넣어줌
            readCode = readCode.Replace("\n", "\n\t\t");    // 마찬가지로 ReadBuffer 내부 내용에 탭2번 넣어줌
            writeCode = writeCode.Replace("\n", "\n\t\t");
            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
        }

        public static Tuple<string, string, string> ParseList(XmlReader r)  // list관련 Xml 분석하고 list파트 완성시켜줌
        {
            string listName = r["name"];
            if(string.IsNullOrEmpty(listName)) 
            {
                Console.WriteLine("List without Name");
                return null;
            }

            Tuple<string, string, string> t = ParseMembers(r);

            string memberCode = string.Format(PacketFormat.memberListFormat, FirstCharToUpper(listName), FirstCharToLower(listName), t.Item1, t.Item2, t.Item3);
            string readCode = string.Format(PacketFormat.readListFormat, FirstCharToUpper(listName), FirstCharToLower(listName));
            string writeCode = string.Format(PacketFormat.writeListFormat, FirstCharToUpper(listName), FirstCharToLower(listName));
        
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

        public static string FirstCharToUpper(string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;
            return input[0].ToString().ToUpper() + input.Substring(1);
        }

        public static string FirstCharToLower(string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;
            return input[0].ToString().ToLower() + input.Substring(1);
        }
    }
}