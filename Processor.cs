using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms; // これを追加
using Tesseract;

namespace TranslationLens
{
    internal class Processor
    {
        // ロガーのインスタンスを作成
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        String filename(String val)
        {
            String my_dir = "d:\\user temp\\" + DateTime.Now.ToString("yyyyMMdd") + "\\";
            String my_file = DateTime.Now.ToString("hhmmss") + DateTime.Now.Millisecond.ToString() + ".jpg";
            return (my_dir + val + my_file);

        }

        /// <summary>
        /// 範囲を指定してスクリーンショットを撮る
        /// </summary>
        /// <param name="my_rectangle">範囲</param>
        public Bitmap snap(Rectangle my_rectangle)
        {
            Bitmap my_bmp = new Bitmap(my_rectangle.Width, my_rectangle.Height);
            Graphics my_graphics = Graphics.FromImage(my_bmp);

            // new Point(40, 40), new Size(100, 100));
            my_graphics.CopyFromScreen(my_rectangle.X, my_rectangle.Y, 0, 0, my_rectangle.Size);

            return my_bmp;

            //my_bmp.Save(filename(""), System.Drawing.Imaging.ImageFormat.Jpeg);
        }

        /// <summary>
        /// Bitmap から文字列を取得
        /// https://github.com/tesseract-ocr/tessdata_best?utm_source=chatgpt.com
        /// </summary>
        /// <param name="bitmap">OCR対象の画像</param>
        /// <returns>抽出テキスト</returns>
        internal string GetTextFromImage(Bitmap bitmap)
        {
            // tessdata フォルダのフルパス
            string langPath = Path.GetFullPath("tessdata");

            if(!Directory.Exists(langPath))
            {
                throw new FileNotFoundException("言語フォルダが見つかりません: " + langPath);
            }

            var dataPath = Path.Combine(langPath, "jpn.traineddata");
            if(!File.Exists(dataPath))
            {
                throw new FileNotFoundException("言語データが見つかりません: " + dataPath);
            }

            dataPath = Path.Combine(langPath, "eng.traineddata");
            if (!File.Exists(dataPath))
            {
                throw new FileNotFoundException("言語データが見つかりません: " + dataPath);
            }

            // OCR用の前処理
            bitmap = OcrHelper.GetBitmap(bitmap);

            // OCR 実行
            var img = PixConverter.ToPix(bitmap);
            // TesseractEngine の初期化
            using (var engine = new TesseractEngine(langPath, "eng+jpn", EngineMode.LstmOnly))
            {
                using (var page = engine.Process(img))
                {
                    string text = page.GetText();

                    return text;
                }
            }

            return null; ;
        }
    }
}
