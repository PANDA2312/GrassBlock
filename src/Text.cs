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
	public NormalText? extra { get; set; } = null;
	public static NormalText Read(string content)
	{
		NormalText res = new NormalText();
		NormalText t = res;
		for(int i=0; i<content.Length; i++)
		{
			string text = string.Empty;
			if(content[i]=='(')
			{
				while(content[i]!=')')
				{
					if(content[i]=='b') t.bold = true;
					if(content[i]=='i') t.italic = true;
					if(content[i]=='u') t.underlined = true;
					if(content[i]=='d') t.strikethrough = true;
					if(content[i]=='r') t.obfuscated = true;
					if(content[i]=='#')
					{
						t.color = content.Substring(i,7);
						i+=6;
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
						t.color = color;
						break;
					}
					i++;
				}
				i++;
			}	
			while(content[i]!='|')
			{
				text += content[i];
				i++;
			}
			t.text = text;
			i++;
			t.extra = new NormalText();
			t = t.extra;
		}
		return res;
	}
}
