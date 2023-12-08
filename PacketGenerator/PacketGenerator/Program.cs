using System;
using System.Xml;

namespace PacketGenerator
{
    class Program
    {
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
                    }

                    //Console.WriteLine(r.Name + " " + r["name"]);    
                }
            }   
        }

        public static void ParsePacket(XmlReader r) 
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

            ParseMembers(r);
        }

        public static void ParseMembers(XmlReader r) 
        {
            string packetName = r["name"];

            int depth = r.Depth + 1;    // playerInfoRequirement 내의 목록들을 원함
            while(r.Read())     // r의 content를 순차적으로 읽어감
            {
                if (r.Depth != depth)   // 목표 목록들을 빠져나가면 break
                    break;

                string memberName = r["name"];
                if(string.IsNullOrEmpty(memberName))
                {
                    Console.WriteLine("err : Member without name");
                    return;
                }

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
                    case "string":
                    case "list":
                        break;
                    default:
                        break;
                }
            }
        }
    }
}