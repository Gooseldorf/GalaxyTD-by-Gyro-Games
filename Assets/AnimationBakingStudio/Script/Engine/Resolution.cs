using System;
using UnityEngine;

namespace ABS
{
	[Serializable]
	public class Resolution
	{
		public int width;
		public int height;

		public Resolution(int width, int height)
        {
			this.width = width;
			this.height = height;
		}

		public Resolution(Vector2 vec2)
		{
			width = Mathf.RoundToInt(vec2.x);
			height = Mathf.RoundToInt(vec2.y);

			if (width < 1)
				width = 1;
			if (height < 1)
				height = 1;
		}
	}
}
