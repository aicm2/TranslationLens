using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms; // これを追加
using Tesseract;

namespace TranslationLens
{
    internal class Processor
    {
        // ロガーのインスタンスを作成
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string gASURL = "https://script.google.com/macros/s/AKfycbwIbppF5BsIZ7BkewR3pXFN_eK0ExnUpy90GNV2JGpgc-hYs0itzDkcDVWdsa_CwEVEFA/exec";

        private readonly string clientId = "629337539653-pdu7usoru0lb4e7fg83du66i54ph7e4v.apps.googleusercontent.com";
        private readonly string clientSecret = "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";

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

        internal async Task<string> OCRByGoogle(string imagePath)
        {

            string gasUrl = this.gASURL;

            using (var client = new HttpClient())
            using (var content = new MultipartFormDataContent())
            {
                var fileBytes = File.ReadAllBytes(imagePath);
                var byteContent = new ByteArrayContent(fileBytes);
                byteContent.Headers.ContentType =
                            new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");

                content.Add(byteContent, "file", "sample.png");

                var response = await client.PostAsync(gasUrl, content);
                string result = await response.Content.ReadAsStringAsync();

                return result;
            }
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
        }

        /// <summary>
        /// google OAuth認証処理
        /// </summary>
        /// <returns>Task</returns>
        internal async Task OAuthByGoogle()
        {
            // OAuth 2.0 クライアント情報
            var clientId = this.clientId;
            var clientSecret = this.clientSecret;

            // 使用するスコープ
            string[] scopes = { "https://www.googleapis.com/auth/drive.file" };

            // トークン保存場所
            var credPath = "token.json";

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                scopes,
                "user", // ユーザー識別子
                CancellationToken.None,
                new FileDataStore(credPath, true),
                new Google.Apis.Auth.OAuth2.LocalServerCodeReceiver() // ←任意ユーザー選択可能
            );

            Console.WriteLine("OAuth認証完了！");
            Console.WriteLine("ログインユーザー: " + credential.UserId);
            Console.WriteLine("アクセストークン: " + credential.Token.AccessToken);

            Console.WriteLine("Enterで終了");
            Console.ReadLine();

        }
        internal void OAuth()
        {
            throw new NotImplementedException();
        }
    }
}
