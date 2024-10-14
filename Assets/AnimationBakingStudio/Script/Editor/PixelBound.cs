namespace ABS
{
    public class PixelBound
    {
        public PixelVector min;
        public PixelVector max;

        public PixelBound()
        {
            min = new PixelVector(int.MaxValue, int.MaxValue);
            max = new PixelVector(int.MinValue, int.MinValue);
        }

        public PixelBound(int minX, int maxX, int minY, int maxY)
        {
            min = new PixelVector(minX, minY);
            max = new PixelVector(maxX, maxY);
        }

        public PixelBound(PixelVector min, PixelVector max)
        {
            this.min = min.Copy();
            this.max = max.Copy();
        }

        public PixelBound Copy()
        {
            return new PixelBound(min, max);
        }

        public PixelBound CopyExtendedBy(int ex)
        {
            return new PixelBound(min.x - ex, max.x + ex, min.y - ex, max.y + ex);
        }
    };
}
