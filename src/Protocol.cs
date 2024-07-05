using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using GrassBlock.Type;
namespace GrassBlock
{
    namespace Protocol
    {
        public class Listener
        {
            public const int HANDSHAKE = 0x00;
            public readonly string IpAddr;
            public readonly int Port;
            private bool runningToken = true;
            private const int packetLen = 1024 * 1024;
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
            public class RecivedContent(byte[] buffer)
            {
                public byte[] Buffer = buffer;
            }
            public void StartListen()
            {
                Thread thread = new Thread(() => Listen());
                thread.Start();
            }
            public void Listen()
            {
                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(IpAddr), Port);
                listener.Bind(ipEndPoint);
                listener.Listen();
                while (true)
                {
                    if (!runningToken) break;
                    Socket clientSocket = listener.Accept();
                    byte[] buffer = new byte[packetLen];
                    RecivedContent recivedContent = new RecivedContent(buffer);
                    clientSocket.BeginReceive(buffer, 0, packetLen, SocketFlags.None, new AsyncCallback(ReceiveCallback), recivedContent);
                }
            }
            private void ReceiveCallback(IAsyncResult asyncResult)
            {
                if (asyncResult.AsyncState == null) throw new ArgumentNullException(nameof(asyncResult.AsyncState));
                RecivedContent content = (RecivedContent)asyncResult.AsyncState;
                byte[] buffer = content.Buffer;
                BytesReader reader = new BytesReader(buffer);
                int len = reader.ReadVarInt();
                int packetId = reader.ReadVarInt();
                if (packetId == HANDSHAKE)
                {
                    int protocolVersion = reader.ReadVarInt();
                    string addr = reader.ReadString();
                    UInt16 port = reader.ReadUInt16();
                    int nextState = reader.ReadVarInt();
                    System.Console.WriteLine(addr + "     " + port.ToString());
                    HandShakePacket packet = new HandShakePacket(protocolVersion, nextState);
                    packet.Process();
                }
            }
            public class BytesReader
            {
                private byte[] data = { };
                private int index;
                public BytesReader(byte[] _data)
                {
                    data = _data;
                }
                public int ReadVarInt() => VarNum.ReadVarInt(data, ref index);
                public long ReadVarLong() => VarNum.ReadVarLong(data, ref index);
                public string ReadString()
                {
                    int len = ReadVarInt();
                    byte[] buffer = data.ToList().GetRange(index, len).ToArray();
                    index += len;
                    return Encoding.UTF8.GetString(buffer);
                }
                public UInt16 ReadUInt16()
                {
                    byte[] buffer = data.ToList().GetRange(index, 2).ToArray();
                    index += 2;
                    Array.Reverse(buffer);
                    return BitConverter.ToUInt16(buffer);
                }
            }
        }
        public class Connection(IPEndPoint remoteEndPoint)
        {
            public IPEndPoint RemoteEndPoint { get; set; }
            public string PlayerName { get; set; }
            //[SupportedOSPlatform(OSPlatform.Linux)];
        }

    }
}