using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

public class ComicBubbleTranslator
{
    /// <summary>
    /// 画像から吹き出しを検出して List に格納する
    /// C# 7.3 / .NET Framework 4.6.1 以上対応
    /// </summary>
    public List<Bitmap> TranslateBubbles(string imagePath)
    {
        List<Bitmap> bubbles = new List<Bitmap>();

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

        foreach (var contour in contours)
        {
            Rect rectCv = Cv2.BoundingRect(contour);

            if (rectCv.Width < 30 || rectCv.Height < 30) continue;

            double ratio = (double)rectCv.Width / rectCv.Height;
            if (ratio < 0.3 || ratio > 3.0) continue;

            // 吹き出しを切り出して 24bit Bitmap に変換
            Mat roi = new Mat(src, rectCv);
            Bitmap bmp = MatToBitmap(roi);

            bubbles.Add(bmp);
            roi.Dispose();
        }

        // 後処理
        src.Dispose();
        hsv.Dispose();
        maskWhite.Dispose();
        maskYellow.Dispose();
        maskCyan.Dispose();
        mask.Dispose();
        kernel.Dispose();

        return bubbles;
    }

    /// <summary>
    /// OpenCvSharp Mat → System.Drawing.Bitmap 変換 (24bit RGB)
    /// </summary>
    private static Bitmap MatToBitmap(Mat mat)
    {
        Mat matRgb = new Mat();
        if (mat.Channels() == 1)
            Cv2.CvtColor(mat, matRgb, ColorConversionCodes.GRAY2BGR);
        else if (mat.Channels() == 4)
            Cv2.CvtColor(mat, matRgb, ColorConversionCodes.BGRA2BGR);
        else
            matRgb = mat.Clone();

        Bitmap bmp = new Bitmap(matRgb.Width, matRgb.Height, PixelFormat.Format24bppRgb);
        BitmapData data = bmp.LockBits(
            new Rectangle(0, 0, bmp.Width, bmp.Height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format24bppRgb);

        int bytes = matRgb.Rows * matRgb.Cols * matRgb.ElemSize();
        byte[] buffer = new byte[bytes];

        System.Runtime.InteropServices.Marshal.Copy(matRgb.Data, buffer, 0, bytes);
        System.Runtime.InteropServices.Marshal.Copy(buffer, 0, data.Scan0, bytes);

        bmp.UnlockBits(data);
        matRgb.Dispose();

        return bmp;
    }
}
