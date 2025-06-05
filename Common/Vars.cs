using System;
using Nettention.Proud;

namespace TankGame
{
    public class Vars
    {
        // Protocol version between server and client (must match)
        public static readonly System.Guid m_Version = new System.Guid("{ 0x3ae33249, 0xecc6, 0x4980, { 0xbc, 0x5d, 0x7b, 0xa, 0x99, 0x9c, 0x7, 0x39 } }");
        
        // Server port
        public const int ServerPort = 33334;
        
        // Server IP (for local testing)
        public const string ServerIP = "localhost";
    }
} 