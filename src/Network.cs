using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using GrassBlock.Protocol;
using GrassBlock.Types;
using Serilog;
using GrassBlock.Text;
using GrassBlock.Config;
namespace GrassBlock
{
    namespace Network
	{
		//接受内容
    	public class RecivedContent(byte[] buffer, Socket clientSocket, int bufferLength)
		{
            public byte[] Buffer = buffer;
        	public Socket ClientSocket = clientSocket;
			public int BufferLength = bufferLength;
		}
		//监听器
        public class Listener
        {
			//当前实例
			public static Listener Instance { get; set; }
			//连接列表
			public ConcurrentDictionary<IPEndPoint, Connection> Connections = new ConcurrentDictionary<IPEndPoint,Connection>();
			private Socket ServerSocket { get; set; }
			public void Stop() => runningToken = false;
			//ip
            public readonly string IpAddr;
			//端口
            public readonly int Port;
			//运行token,如果为false就关闭
            private bool runningToken = true;
			//包大小
            private const int packetLen = 1048576;
			//构造函数
            public Listener(string _IpAddr, int _Port)
            {
                if (_IpAddr == null) throw new ArgumentNullException(nameof(_IpAddr));
                if (_Port < 0) throw new InvalidDataException(nameof(_Port));
                string[] IpNum = _IpAddr.Split('.');
                if (IpNum.Length != 4) throw new InvalidDataException(nameof(_IpAddr));
                IpNum.ToList().ForEach(x =>
                {
                    int v = Convert.ToInt32(x);
                    if (v < 0 || v > 255) throw new InvalidDataException(nameof(_IpAddr));
                });
                IpAddr = _IpAddr;
                Port = _Port;
            }
			//创建线程并开始监听
            public void StartListen()
            {
				Instance = this;
                Thread thread = new Thread(() => Listen());
                thread.Start();
				PacketHandler.Start();
            }
			//监听器主循环
            public async void Listen()
            {
                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
				listener.ReceiveBufferSize = 1024 * 1024;
				IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(IpAddr), Port);
                listener.Bind(ipEndPoint);
				ServerSocket = listener;
                listener.Listen();
				Log.Information("Start Listen on {IpAddr}:{Port}", IpAddr, Port);
                while (true)
                {
					//循环接收消息
                    if (!runningToken) break;
                    Socket clientSocket = await listener.AcceptAsync();
					byte[] buffer = new byte[packetLen];
					ArraySegment<byte> bufferSegment = new ArraySegment<byte>(buffer);
					int bytesRead = await clientSocket.ReceiveAsync(bufferSegment);
					PacketHandler.Add(buffer, clientSocket, bytesRead);
                }
            }
		}
		//字节数组读取器
        public class BytesReader
        {
            public byte[] data { get; }= { };
			private int index = 0;
            public int Index 
			{
				get => index;
				set => index = value;
			}
			public BytesReader(byte[] _data)
            {
                data = _data;
            }
			//读VarInt和VarLong
            public int ReadVarInt(out int len) => VarNum.ReadVarInt(data, ref index, out len);
            public long ReadVarLong() => VarNum.ReadVarLong(data, ref index);
			//读String
            public string ReadString()
            {
                int len = ReadVarInt(out _);	
                byte[] buffer = data.ToList().GetRange(index, len).ToArray();
                index += buffer.Length;
                return Encoding.UTF8.GetString(buffer);
            }
			//读UInt16
            public UInt16 ReadUInt16()
            {
                byte[] buffer = data.ToList().GetRange(index, 2).ToArray();
                index += 2;
                Array.Reverse(buffer);
                return BitConverter.ToUInt16(buffer);
            }
			//读UUID(GUID)
			public Guid ReadUUID()
			{
				byte[] buffer = data.ToList().GetRange(index, 16).ToArray();
				index += 16;
				Array.Reverse(buffer);
				return new Guid(buffer);
			}
			public long ReadLong()
            {
                byte[] buffer = data.ToList().GetRange(index, 8).ToArray();
                index += 8;
                Array.Reverse(buffer);
                return BitConverter.ToInt64(buffer);
            }

        }
		//连接类
        public class Connection
        {
			public Socket Socket { get; set; }
            public IPEndPoint RemoteEndPoint { get; set; }
            public string? PlayerName { get; set; }
			public Guid UUID { get; set; }
			//连接状态
			public enum ConnectionStatus
			{
				HandShaking = 0,StartLogin,EncryptionReq,EncryptionRes,LoginSuccess,Status,Ping
			}
			public ConnectionStatus NextState { get; set; }
			//构造函数
			public Connection(Socket socket)
			{
				Socket = socket;
				RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint ?? throw new ArgumentNullException("RemoteEndPoint is null");
				NextState = ConnectionStatus.HandShaking;
				Listener.Instance.Connections.TryAdd((IPEndPoint)Socket.RemoteEndPoint, this);
			}
        }
		public static class PacketHandler
		{
			public static ConcurrentQueue<RecivedContent> RecivedContentQueue = new ConcurrentQueue<RecivedContent>();
			private static Thread ThreadProcess = new Thread(Process);
			public static void Start() => ThreadProcess.Start();	
			public static void Stop() => ThreadProcess.Interrupt();
			public static void Add(byte[] bytes, Socket clientSocket, int len)
			{
				RecivedContentQueue.Enqueue(new RecivedContent(bytes, clientSocket, len));
			}
			private static void SplitAndProcess(RecivedContent recivedContent)
			{	
				byte[] buffer = recivedContent.Buffer;
				BytesReader reader = new BytesReader(buffer);
				int index = 0;
				while(index < buffer.Length)
				{
					reader.Index = index;  
					int len = reader.ReadVarInt(out _);
					index = reader.Index;
					Int16 id = (Int16)reader.ReadVarInt(out int idLen);
					if(len == 0) break;
					ReadAndProcess(len-1,id, buffer[(index+idLen)..(index+len)], recivedContent.ClientSocket);
					index += len;
				}
			}
			private static void ReadAndProcess(int len, Int16 packetId, byte[] content, Socket clientSocket)
			{
				BytesReader reader = new BytesReader(content);
				IClientPacket? packet = null;
				Listener.Instance.Connections.TryGetValue((IPEndPoint)clientSocket.RemoteEndPoint, out Connection? conn);
				if(packetId == PacketType.HANDSHAKE)
				{
					if(conn is null) packet = HandShakePacket.Read(reader, clientSocket);
					else if(conn.NextState == Connection.ConnectionStatus.HandShaking) packet = StartLoginPacket.Read(reader,conn);
					else if(conn.NextState == Connection.ConnectionStatus.Status && len == 0)
					{
						Log.Debug("Recvied Status Packet");
						byte[] data = new ServerListPingResponse(NormalText.Read(MainConfig.CurrentConfig.Motd)).bytes;
						clientSocket.Send(data);
						conn.NextState = Connection.ConnectionStatus.Ping;
					}
				}
				else if(packetId == PacketType.PING && conn.NextState == Connection.ConnectionStatus.Ping)
				{
					Log.Debug("Ping");
				}
				//处理
				if(packet is not null)packet.Process();
			}
			private static void Process()
			{
				try
				{
					while (true)
					{ 
						if(RecivedContentQueue.Count > 0)
						{
							RecivedContentQueue.TryDequeue(out RecivedContent current);
							SplitAndProcess(current);
						}
						Thread.Sleep(0);
					}
				}
				catch (ThreadInterruptedException) { }
			}
		}
    }
}
