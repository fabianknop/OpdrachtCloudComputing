using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace OpdrachtCloudComputing
{
    public static class ImageHelper
    {
        public static Stream AddTextToImage(Stream imageStream, params (string text, (float x, float y) position)[] texts)
        {
            var image = Image.FromStream(imageStream);
            var bitmap = new Bitmap(image);
            var graphics = Graphics.FromImage(bitmap);
            var drawFont = new Font("Cambria", 20);

            foreach (var (text, (x, y)) in texts)
            {
                graphics.DrawString(text, drawFont, Brushes.Black, x, y);
            }

            var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, ImageFormat.Png);

            memoryStream.Position = 0;

            return memoryStream;
        }
    }
}