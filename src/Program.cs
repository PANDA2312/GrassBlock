using GrassBlock.Protocol;

namespace GrassBlock
{
    public static class Program
    {
        public static void Main(string[] args)
        {
<<<<<<< HEAD
            Listener listener = new Listener("127.0.0.1", 20001);
=======
			Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Console().CreateLogger();
            Listener listener = new Listener(MainConfig.CurrentConfig.IPAddr, MainConfig.CurrentConfig.Port);
>>>>>>> dev
            listener.StartListen();
        }
    }
}