using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tesseract;

namespace TranslationLens
{
    public static class OcrHelper
    {
        public static Bitmap GetBitmap(Bitmap src)
        {
            // 1. 前処理
            Bitmap gray = ToGrayscale(src);
            Bitmap bin = Binarize(gray);
            Bitmap withMargin = AddMargin(bin, 20);

            // 2. 24bit 変換
            Bitmap finalImg = Ensure24bppRgb(withMargin);

            return finalImg;
        }

        private static Bitmap ToGrayscale(Bitmap src)
        {
            Bitmap gray = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(gray))
            {
                ColorMatrix colorMatrix = new ColorMatrix(new float[][]
                {
                new float[] {0.3f, 0.3f, 0.3f, 0, 0},
                new float[] {0.59f,0.59f,0.59f,0,0},
                new float[] {0.11f,0.11f,0.11f,0,0},
                new float[] {0,0,0,1,0},
                new float[] {0,0,0,0,1}
                });
                ImageAttributes attr = new ImageAttributes();
                attr.SetColorMatrix(colorMatrix);
                g.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height),
                    0, 0, src.Width, src.Height, GraphicsUnit.Pixel, attr);
            }
            return gray;
        }

        private static Bitmap Binarize(Bitmap src, int threshold = 128)
        {
            Bitmap bin = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    Color c = src.GetPixel(x, y);
                    int v = (c.R + c.G + c.B) / 3;
                    Color bw = v < threshold ? Color.Black : Color.White;
                    bin.SetPixel(x, y, bw);
                }
            }
            return bin;
        }

        private static Bitmap AddMargin(Bitmap src, int margin)
        {
            Bitmap result = new Bitmap(src.Width + margin * 2, src.Height + margin * 2, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.Clear(Color.White);
                g.DrawImage(src, new Rectangle(margin, margin, src.Width, src.Height));
            }
            return result;
        }

        private static Bitmap Ensure24bppRgb(Bitmap src)
        {
            if (src.PixelFormat == PixelFormat.Format24bppRgb) return src;

            Bitmap clone = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(clone))
            {
                g.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height));
            }
            return clone;
        }
    }
}
