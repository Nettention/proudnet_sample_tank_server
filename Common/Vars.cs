using System;
using Nettention.Proud;

namespace TankGame
{
    public class Vars
    {
        // 서버와 클라이언트 간 프로토콜 버전 (일치해야 함)
        public static readonly System.Guid m_Version = new System.Guid("{ 0x3ae33249, 0xecc6, 0x4980, { 0xbc, 0x5d, 0x7b, 0xa, 0x99, 0x9c, 0x7, 0x39 } }");
        
        // 서버 포트
        public const int ServerPort = 33334;
        
        // 서버 IP (로컬 테스트용)
        public const string ServerIP = "localhost";
    }
} 