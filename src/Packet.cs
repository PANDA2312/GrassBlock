using GrassBlock.Types;
using GrassBlock.Network;
using System.Net;
using Serilog;
namespace GrassBlock
{
    namespace Protocol
    {
		//包类型
		public static class PacketType
		{
			public const Int16 HANDSHAKE = 0x00;
		}
		//包的接口
		public interface IPacket
        {
			public static IPacket Create(BytesReader reader, IPEndPoint _remoteEndPoint)
			{
				throw new MethodAccessException("Type is not implements this method");
			}
			public static IPacket Create(BytesReader reader, Connection conn)
			{
				throw new MethodAccessException("Type is not implements this method");
			}
			public void Process(BytesReader reader);
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
                HandShakePacket packet = new HandShakePacket(_protocolVersion, _nextState, _addr, _port, _remoteEndPoint);
				return packet;
            }
			//握手包处理
			public void Process(BytesReader reader)
			{
				//日志
				Log.Debug("Recvied HandShakePacket ProtocolVersion:{ProtocolVersion}, NextState:{NextState}", ProtocolVersion, NextState);
				//创建连接
				Connection conn = new Connection(RemoteEndPoint);
				//判断下个状态是否为登录
				if(NextState==2)
				{
					//读取并创建开始登录包然后处理
					//读取长度
					int len = reader.ReadVarInt();
					//丢弃ID
					_ = reader.ReadVarInt();
					//创建
					IPacket packet = StartLoginPacket.Create(reader, conn);
					//处理
					packet.Process(reader);
				}
			}
        }
		//登录开始包
		public class StartLoginPacket(string username, Guid uuid, Connection _connection) : IPacket
		{
			public string Username { get; set; } = username;
			public Guid UUID { get; set; } = uuid;
			private Connection connection = _connection;
			public static IPacket Create(BytesReader reader, Connection conn)
			{
				string _username = reader.ReadString();	
				Guid _uuid = reader.ReadUUID();
				StartLoginPacket packet = new StartLoginPacket(_username, _uuid, conn);
				return packet;
			}
			public void Process(BytesReader reader)
			{
				Log.Information("User: {Username} StartLogin! UUID: {UUID}", Username, UUID);
				connection.Status = Connection.ConnectionStatus.StartLogin;
			}
		}
    }
}
