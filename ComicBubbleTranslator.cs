using System;
using System.Drawing;
using System.Drawing.Imaging;
using OpenCvSharp;

public class ComicBubbleTranslator
{
    /// <summary>
    /// 画像から吹き出しを検出して翻訳文字を描画
    /// C# 7.3 / .NET Framework 4.6.1 以上対応
    /// 型衝突 (Point) 解消済み
    /// </summary>
    public static Bitmap TranslateBubbles(string imagePath)
    {
        // OpenCvSharp の Mat を読み込む
        Mat src = Cv2.ImRead(imagePath);
        Mat hsv = new Mat();
        Cv2.CvtColor(src, hsv, ColorConversionCodes.BGR2HSV);

        // 白・黄色・薄青のマスク
        Mat maskWhite = new Mat();
        Cv2.InRange(hsv, new Scalar(0, 0, 200), new Scalar(180, 30, 255), maskWhite);

        Mat maskYellow = new Mat();
        Cv2.InRange(hsv, new Scalar(20, 50, 200), new Scalar(40, 255, 255), maskYellow);

        Mat maskCyan = new Mat();
        Cv2.InRange(hsv, new Scalar(80, 50, 150), new Scalar(100, 255, 255), maskCyan);

        Mat mask = new Mat();
        Cv2.BitwiseOr(maskWhite, maskYellow, mask);
        Cv2.BitwiseOr(mask, maskCyan, mask);

        // モルフォロジー処理でノイズ除去
        Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
        Cv2.MorphologyEx(mask, mask, MorphTypes.Close, kernel);

        // 輪郭検出
        OpenCvSharp.Point[][] contours;
        HierarchyIndex[] hierarchy;
        Cv2.FindContours(mask, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        // Mat → Bitmap に変換（Extensions がなくても自作関数でOK）
        Bitmap bmp = MatToBitmap(src);

        // Graphics で描画
        Graphics g = Graphics.FromImage(bmp);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        foreach (var contour in contours)
        {
            OpenCvSharp.Rect rectCv = Cv2.BoundingRect(contour);

            if (rectCv.Width < 30 || rectCv.Height < 30) continue;

            double ratio = (double)rectCv.Width / rectCv.Height;
            if (ratio < 0.3 || ratio > 3.0) continue;

            // OpenCvSharp.Rect → System.Drawing.Rectangle に変換
            Rectangle drawRect = new Rectangle(rectCv.X, rectCv.Y, rectCv.Width, rectCv.Height);

            // 仮の翻訳文字列
            string translatedText = "Hello!";

            // フォントサイズを吹き出し高さに合わせる
            float fontSize = rectCv.Height / 2.0f;
            Font font = new Font("Arial", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);

            // 文字列中央寄せ
            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;

            // 半透明の背景で文字が見やすくなる
            using (Brush bgBrush = new SolidBrush(Color.FromArgb(128, Color.White)))
            {
                g.FillRectangle(bgBrush, drawRect);
            }

            using (Brush textBrush = new SolidBrush(Color.Black))
            {
                g.DrawString(translatedText, font, textBrush, drawRect, sf);
            }

            // Dispose
            font.Dispose();
            sf.Dispose();
        }

        g.Dispose();

        return bmp;
    }

    /// <summary>
    /// OpenCvSharp Mat → System.Drawing.Bitmap 変換 (Extensions 不要)
    /// </summary>
    private static Bitmap MatToBitmap(Mat mat)
    {
        // 24bppRgb に変換
        Mat matRgb = new Mat();
        if (mat.Channels() == 1)
            Cv2.CvtColor(mat, matRgb, ColorConversionCodes.GRAY2BGR);
        else if (mat.Channels() == 4)
            Cv2.CvtColor(mat, matRgb, ColorConversionCodes.BGRA2BGR);
        else
            matRgb = mat.Clone();

        Bitmap bmp = new Bitmap(matRgb.Width, matRgb.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        BitmapData data = bmp.LockBits(
            new Rectangle(0, 0, bmp.Width, bmp.Height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format24bppRgb);

        int bytes = matRgb.Rows * matRgb.Cols * matRgb.ElemSize(); // ElemSize() は 1ピクセルあたりのバイト数
        byte[] buffer = new byte[bytes];

        // Mat のデータを byte[] にコピー
        System.Runtime.InteropServices.Marshal.Copy(matRgb.Data, buffer, 0, bytes);

        // Bitmap にコピー
        System.Runtime.InteropServices.Marshal.Copy(buffer, 0, data.Scan0, bytes);

        bmp.UnlockBits(data);
        matRgb.Dispose();

        return bmp;
    }
}
