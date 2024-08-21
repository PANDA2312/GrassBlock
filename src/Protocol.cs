using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using GrassBlock.Types;
using GrassBlock.Protocol;
namespace GrassBlock
{
    namespace Protocol
	{
		//监听器
        public class Listener
        {
			//当前实例
			public static Listener Instance { get; set; }
			//连接列表
			public List<Connection> Connections = new List<Connection>();
			//连接列表索引
			public Connection? this[IPEndPoint addr]
			{
				get
				{
					Connection? res = null;
					Connections.ForEach(conn=>{
						if(conn.RemoteEndPoint==addr)res = conn;
					});
					return res;
				}
				set
				{
					Connections.ForEach(conn=>{
						if(conn.RemoteEndPoint==addr)
						{
							conn = value;
							return;
						}
					});
					throw new InvalidDataException("Didn't find it.");
				}
			}
			//ip
            public readonly string IpAddr;
			//端口
            public readonly int Port;
			//运行token,如果为false就关闭
            private bool runningToken = true;
			//包大小
            private const int packetLen = 1024 * 1024;
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
			//接受内容
            public class RecivedContent(byte[] buffer,IPEndPoint? remoteEndPoint)
            {
                public byte[] Buffer = buffer;
				public IPEndPoint? RemoteEndPoint = remoteEndPoint;
            }
			//创建线程并开始监听
            public void StartListen()
            {
				Instance = this;
                Thread thread = new Thread(() => Listen());
                thread.Start();
            }
			//监听器主循环
            public void Listen()
            {
                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(IpAddr), Port);
                listener.Bind(ipEndPoint);
                listener.Listen();
                while (true)
                {
					//循环接收消息
                    if (!runningToken) break;
                    Socket clientSocket = listener.Accept();
                    byte[] buffer = new byte[packetLen];
                    RecivedContent recivedContent = new RecivedContent(buffer,(IPEndPoint)clientSocket.RemoteEndPoint);
                    clientSocket.BeginReceive(buffer, 0, packetLen, SocketFlags.None, new AsyncCallback(ReceiveCallback), recivedContent);
                }
            }
			//接收消息异步回调
            private void ReceiveCallback(IAsyncResult asyncResult)
            {
                if (asyncResult.AsyncState == null) throw new ArgumentNullException(nameof(asyncResult.AsyncState));
                RecivedContent content = (RecivedContent)asyncResult.AsyncState;
                byte[] buffer = content.Buffer;
                BytesReader reader = new BytesReader(buffer);
                int len = reader.ReadVarInt();
                Int16 packetId = (Int16)reader.ReadVarInt();
				Console.WriteLine(packetId);
				ProcessPacket(packetId,reader,content.RemoteEndPoint);
            }
			//处理包
			private void ProcessPacket(Int16 packetId, BytesReader reader, IPEndPoint remoteEndPoint)
			{
				IPacket? packet = null;
				//判断是否为握手或登录开始包
				if(packetId == PacketType.HANDSHAKE_OR_STARTLOGIN)
				{
					if(Instance[remoteEndPoint] is null) packet = HandShakePacket.Create(reader, remoteEndPoint);
					else packet = StartLoginPacket.Create(reader, remoteEndPoint);
				}
				Console.WriteLine("Create packet");
				//处理
				if(packet != null)packet.Process();
			}
		}
		//字节数组读取器
        public class BytesReader
        {
            private byte[] data = { };
            private int index;
            public BytesReader(byte[] _data)
            {
                data = _data;
            }
			//读VarInt和VarLong
            public int ReadVarInt() => VarNum.ReadVarInt(data, ref index);
            public long ReadVarLong() => VarNum.ReadVarLong(data, ref index);
			//读String
            public string ReadString()
            {
                int len = ReadVarInt();
                byte[] buffer = data.ToList().GetRange(index, len).ToArray();
                index += len;
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
        }
		//连接类
        public class Connection
        {
            public IPEndPoint RemoteEndPoint { get; set; }
            public string PlayerName { get; set; }
			public Guid UUID { get; set; }
			//连接状态
			public enum ConnectionStatus
			{
				HandShaking = 0,StartLogin,EncryptionReq,EncryptionRes,LoginSuccess
			}
			public ConnectionStatus Status { get; set; }
			//构造函数
			public Connection(IPEndPoint remoteEndPoint)
			{
				RemoteEndPoint = remoteEndPoint;
				Status = ConnectionStatus.HandShaking;
				Listener.Instance.Connections.Add(this);
			}
        }

    }
}
