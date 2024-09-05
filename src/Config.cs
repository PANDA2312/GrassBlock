using Nett;
using System.Diagnostics.CodeAnalysis;
namespace GrassBlock.Config
{
	public class MainConfig
	{
		public static MainConfig CurrentConfig { get; set; } = Toml.ReadFile<MainConfig>("./config.toml");
		[DisallowNull]
		public string Motd { get; set; } = string.Empty;
		public string IPAddr { get; set; } = "127.0.0.1";
		public Int16 Port { get; set; } = 25565;
	}
}
