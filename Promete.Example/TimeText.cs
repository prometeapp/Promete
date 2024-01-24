using System.Drawing;
using Promete.Elements;
using Promete.Graphics;

namespace Promete.Example;

public class TimeText(GlyphRenderer glyphRenderer) : Text(glyphRenderer, "", Graphics.Font.GetDefault(64), System.Drawing.Color.White)
{
	private int updateTimer = 0;
	protected override void OnUpdate()
	{
		if (updateTimer == 0)
		{
			Content = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
		}

		updateTimer = (updateTimer + 1) % 60;
	}
}
