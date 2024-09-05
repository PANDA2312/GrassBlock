using Serilog;
namespace GrassBlock.Text;
public class NormalText
{
	public string text { get; set; } = string.Empty;
	public string color { get; set; } = "white";
	public const string font = "minecraft:default";
	public bool bold { get; set; } = false;
	public bool italic { get; set; } = false;
	public bool underlined { get; set; } = false;
	public bool strikethrough { get; set; } = false;
	public bool obfuscated { get; set; } = false;
	public List<NormalText>? extra { get; set; } = null;
	public static NormalText Read(string content)
	{
		NormalText res = new NormalText();
		int i=0;
		while(true)
		{
			NormalText tmp = new NormalText();
			string text = string.Empty;
			if(content[i]=='(')
			{
				i++;
				while(content[i]!=')')
				{
					if(content[i]=='b') tmp.bold = true;
					if(content[i]=='i') tmp.italic = true;
					if(content[i]=='u') tmp.underlined = true;
					if(content[i]=='d') tmp.strikethrough = true;
					if(content[i]=='r') tmp.obfuscated = true;
					if(content[i]=='#')
					{
						tmp.color = content.Substring(i,7);
						i+=7;
						continue;
					}
					if(content[i]=='@')
					{
						string color = string.Empty;
						i++;
						while(content[i]!=')')
						{
							color += content[i];
							i++;
						}
						tmp.color = color;
						continue;
					}
					i++;
				}
				i++;
			}
			while(i < content.Length && content[i] != '(')
			{
				text += content[i];
				i++;
			}
			tmp.text = text;
			if(i==0) res = tmp;
			if(i>0)
			{
				if(res.extra is null) res.extra = new List<NormalText>();
				res.extra.Add(tmp);
			}
			if(i >= content.Length) break;
		}
		return res;
	}
}
