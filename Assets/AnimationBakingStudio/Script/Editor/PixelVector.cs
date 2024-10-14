using UnityEngine;

namespace ABS
{
    public class PixelVector
    {
        public int x, y;

        public PixelVector(Vector3 vector3)
        {
            this.x = (int)vector3.x;
            this.y = (int)vector3.y;

            if (x < 0)
                x = 0;
            if (y < 0)
                y = 0;
        }

        public PixelVector(PixelVector other)
        {
            this.x = other.x;
            this.y = other.y;
        }

        public PixelVector(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override string ToString()
        {
            return x + ", " + y;
        }

        public PixelVector Copy()
        {
            return new PixelVector(x, y);
        }

        public static PixelVector operator+(PixelVector p1, PixelVector p2)
        {
            return new PixelVector(p1.x + p2.x, p1.y + p2.y);
        }

        public static PixelVector operator-(PixelVector p1, PixelVector p2)
        {
            return new PixelVector(p1.x - p2.x, p1.y - p2.y);
        }

        public void SubtractWithMargin(PixelVector pos, int margin)
        {
            x -= pos.x - margin;
            y -= pos.y - margin;
        }
    }
}
