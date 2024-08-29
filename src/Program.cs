using GrassBlock.Network;
using GrassBlock.Config;
using Serilog;
namespace GrassBlock
{
    public static class Program
    {
        public static void Main(string[] args)
        {
			Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Console().CreateLogger();
			MainConfig.Load();	
            Listener listener = new Listener(MainConfig.CurrentConfig.IPAddr, MainConfig.CurrentConfig.Port);
            listener.StartListen();
        }
    }
}
