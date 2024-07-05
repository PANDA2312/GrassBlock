using GrassBlock.Protocol;

namespace GrassBlock
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Listener listener = new Listener("127.0.0.1", 20001);
            listener.StartListen();
        }
    }
}