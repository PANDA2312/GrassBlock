using GrassBlock.Types;
using GrassBlock.Protocol;
using System.Net;
namespace GrassBlock
{
    namespace Protocol
    {
		//包类型
		public static class PacketType
		{
			public const Int16 HANDSHAKE_OR_STARTLOGIN = 0x00;
		}
		//包的接口
		public interface IPacket
        {
			public static abstract IPacket Create(BytesReader reader, IPEndPoint _remoteEndPoint);
			public void Process();
        }
		//握手包
        public class HandShakePacket(int protocolVersion, int nextState, string addr, int port, IPEndPoint remoteEndPoint) : IPacket
        {
            public int ProtocolVersion { get; set; } = protocolVersion;
            public int NextState { get; set; } = nextState;
			public string Addr { get; set; } = addr;
			public int Port { get; set; } = port;
			public IPEndPoint RemoteEndPoint{ get; set; } = remoteEndPoint;
			public static IPacket Create(BytesReader reader, IPEndPoint _remoteEndPoint)
			{
				//读取并返回握手包实例
                int _protocolVersion = reader.ReadVarInt();
                string _addr = reader.ReadString();
                UInt16 _port = reader.ReadUInt16();
                int _nextState = reader.ReadVarInt();
				string username = reader.ReadString();
				Console.WriteLine(username);
				Guid uuid = reader.ReadUUID();
				Console.WriteLine(uuid.ToString());
                HandShakePacket packet = new HandShakePacket(_protocolVersion, _nextState, _addr, _port, _remoteEndPoint);
				return packet;
            }
			//处理
			public void Process()
			{
				Console.WriteLine($"ProtocolVersion: {ProtocolVersion},NextState: {NextState}");
				Console.WriteLine($"IP: {Addr}, Port:{Port}");
				Connection connection = new Connection(RemoteEndPoint);
			}
        }
		//登录开始包
		public class StartLoginPacket(string username, Guid uuid, IPEndPoint remoteEndPoint) : IPacket
		{
			public string Username { get; set;} = username;
			public Guid UUID { get; set; } = uuid;
			public IPEndPoint RemoteEndPoint { get; set; } = remoteEndPoint;
			public static IPacket Create(BytesReader reader, IPEndPoint _remoteEndPoint)
			{
				string _username = reader.ReadString();	
				Guid _uuid = reader.ReadUUID();
				StartLoginPacket packet = new StartLoginPacket(_username, _uuid, _remoteEndPoint);
				return packet;
			}
			public void Process()
			{
				Console.WriteLine($"User: {Username} StartLogin! UUID: {UUID.ToString()}");
				Connection connetction = Listener.Instance[RemoteEndPoint];
				connetction.Status = Connection.ConnectionStatus.StartLogin;
			}
		}
    }
}
