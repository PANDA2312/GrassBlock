using Nett;
using Serilog;
namespace GrassBlock.Config
{
	public class MainConfig
	{
		public static MainConfig? CurrentConfig { get; set; } = null;
		public string Motd { get; set; }
		public string IPAddr { get; set; }
		public Int16 Port { get; set; }
		public static void Load()
		{
			CurrentConfig = Toml.ReadFile<MainConfig>("./config.toml");
			Log.Debug("Config:{@CurrentConfig}", CurrentConfig);
		}
	}
}
