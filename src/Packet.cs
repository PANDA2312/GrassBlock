using GrassBlock.Type;

namespace GrassBlock
{
    namespace Protocol
    {
        public interface IPacket
        {
            public Int16 GetPackId();
            public byte[] GetBytes();
        }
        public class HandShakePacket : IPacket
        {
            private byte[] Data;
            public byte[] GetBytes() => Data;
            public Int16 GetPackId() => 0x00;
            public int ProtocolVersion { get; set; }
            public int NextState { get; set; }
            public HandShakePacket(int protocolVersion, int nextState)
            {
                ProtocolVersion = protocolVersion;
                NextState = nextState;
            }
            public void Process()
            {
                if (NextState == 1) return;
                System.Console.WriteLine("Recived Login Packet!!   ProtocolVersion:{0}   NextState:{1}", ProtocolVersion, NextState);
            }
        }

    }
}