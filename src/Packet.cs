using GrassBlock.Config;
using GrassBlock.Network;
using System.Net;
using System.Text;
using Serilog;
using Newtonsoft.Json;
using GrassBlock.Text;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using GrassBlock.Types;
namespace GrassBlock
{
    namespace Protocol
    {
		//包类型
		public static class PacketType
		{
			public const Int16 HANDSHAKE = 0x00;
			public const Int16 PING = 0x01;
		}
		//包的接口
		public interface IClientPacket
        {
			public static IClientPacket Read(BytesReader reader, Socket _remoteEndPoint)
			{
				throw new MethodAccessException("Type is not implements this method");
			}
			public static IClientPacket Read(BytesReader reader, Connection conn)
			{
				throw new MethodAccessException("Type is not implements this method");
			}
			public void Process();
        }
		public interface IServerPacket
		{
			public byte[] bytes { get; }
		}
		//握手包
        public class HandShakePacket(int protocolVersion, int nextState, string addr, int port, Socket clientSocket) : IClientPacket
        {
            public int ProtocolVersion { get; set; } = protocolVersion;
            public int NextState { get; set; } = nextState;
			public string Addr { get; set; } = addr;
			public int Port { get; set; } = port;
			public Socket ClientSocket { get; set; } = clientSocket;
			public static IClientPacket Read(BytesReader reader, Socket _clientSocket)
			{
				//读取并返回握手包实例
                int _protocolVersion = reader.ReadVarInt(out _);
                string _addr = reader.ReadString();
                UInt16 _port = reader.ReadUInt16();
                int _nextState = reader.ReadVarInt(out _);
                HandShakePacket packet = new HandShakePacket(_protocolVersion, _nextState, _addr, _port, _clientSocket);
				return packet;
            }
			//握手包处理
			public void Process()
			{
				//日志
				Log.Debug("Recvied HandShakePacket ProtocolVersion:{ProtocolVersion}, NextState:{NextState}", ProtocolVersion, NextState);
				//创建连接
				Connection conn = new Connection(ClientSocket);
				if(NextState == 1)
				{ 
					conn.Status = Connection.ConnectionStatus.ServerListPing;
					byte[] data = new ServerListPingResponse(NormalText.Read(MainConfig.CurrentConfig.Motd)).bytes;
					ClientSocket.Send(data);
					ClientSocket.Send(new byte[]{ 0x01, 0x01});
				}
			}
        }
		//登录开始包
		public class StartLoginPacket(string username, Guid uuid, Connection _connection) : IClientPacket
		{
			public string Username { get; set; } = username;
			public Guid UUID { get; set; } = uuid;
			private Connection connection = _connection;
			public static IClientPacket Read(BytesReader reader, Connection conn)
			{
				string _username = reader.ReadString();	
				Guid _uuid = reader.ReadUUID();
				StartLoginPacket packet = new StartLoginPacket(_username, _uuid, conn);
				return packet;
			}
			public void Process()
			{
				Log.Information("User: {Username} StartLogin! UUID: {UUID}", Username, UUID);
				connection.Status = Connection.ConnectionStatus.StartLogin;
			}
		}
		public class ServerListPingResponse([NotNull] dynamic motd) : IServerPacket
		{
			public static Int16 PacketId = 0x00;
			public readonly dynamic version = new {
				name = "1.20.4",
				protocol = 765
			};
			public readonly dynamic players = new {
				max = 20,
				online = 0
			};
			public readonly dynamic description = motd;
			public readonly string favicon = "data:image/png;base64,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";
			public readonly bool enforcesSecureChat = false;
			[JsonIgnore]	
			public byte[] bytes
			{
				get
				{
					string jsonString = JsonConvert.SerializeObject(this);
					byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
					jsonBytes = VarNum.GetVarNum(jsonBytes.Length).Concat(jsonBytes).ToArray();
					byte[] bytes = VarNum.GetVarNum(jsonBytes.Length + 1)
								.Concat(VarNum.GetVarNum(PacketId))
								.Concat(jsonBytes)
								.ToArray();
					return bytes;
				}
			}
		}
    }
}
