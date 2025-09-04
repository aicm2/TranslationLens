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
using static Google.Apis.Requests.BatchRequest;

namespace TranslationLens
{
    internal class Processor
    {
        // ロガーのインスタンスを作成
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        // トークン保存場所
        private string credPath;

        private readonly string gASURL = "https://script.google.com/macros/s/AKfycbwIbppF5BsIZ7BkewR3pXFN_eK0ExnUpy90GNV2JGpgc-hYs0itzDkcDVWdsa_CwEVEFA/exec";

        private readonly string clientId = "629337539653-pdu7usoru0lb4e7fg83du66i54ph7e4v.apps.googleusercontent.com";
        private readonly string clientSecret = "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";

        String filename(String val)
        {
            String my_dir = "d:\\user temp\\" + DateTime.Now.ToString("yyyyMMdd") + "\\";
            String my_file = DateTime.Now.ToString("hhmmss") + DateTime.Now.Millisecond.ToString() + ".jpg";
            return (my_dir + val + my_file);

        }

        internal Processor()
        {

            // 保存先フォルダだけ指定する
            var appFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "TranslationLens"
            );

            appFolder = @"c:\tmp\TranslationLens";


            // フォルダがなければ作成
            Directory.CreateDirectory(appFolder);

            this.credPath = appFolder;
            Logger.Info("credPath: " + this.credPath);
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
        /// 画像をGoogle DriveにアップロードしてOCRした結果を返す
        /// token.json が既にある前提
        /// </summary>
        /// <param name="imagePath">ローカル画像パス</param>
        /// <returns>OCR結果文字列</returns>
        internal async Task<string> OCRByGoogle(string imagePath)
        {
            // 既存トークンを使用して DriveService を初期化
            string[] scopes = { DriveService.Scope.Drive, DriveService.Scope.DriveFile };

            UserCredential credential;
            using (var stream = new MemoryStream()) // ダミーストリームでロードしない場合は null でも可
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    new ClientSecrets
                    {
                        ClientId = this.clientId,
                        ClientSecret = this.clientSecret,
                    }, // OAuthByGoogle() で取得済みならダミーでOK
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(this.credPath, true)
                );
            }

            var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "OCR Test App"
            });

            // Google Docs 化（OCR用）
            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = "tempOCR_" + Guid.NewGuid(),
                MimeType = "application/vnd.google-apps.document"
            };

            using (var stream2 = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
            {
                var request = service.Files.Create(fileMetadata, stream2, "image/png");
                request.Fields = "id";

                Google.Apis.Drive.v3.Data.File file = null; // try の外で宣言

                try
                {
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    await request.UploadAsync(cts.Token);
                    file = request.ResponseBody;
                }
                catch (Google.GoogleApiException ex)
                {
                    Console.WriteLine($"StatusCode: {ex.HttpStatusCode}");
                    Console.WriteLine($"Error: {ex.Error.Message}");
                    throw;
                }

                // OCR結果をテキストで取得
                var exportRequest = service.Files.Export(file.Id, "text/plain");
                using (var ms = new MemoryStream())
                {
                    await exportRequest.DownloadAsync(ms);
                    ms.Position = 0;

                    using (var reader = new StreamReader(ms))
                    {
                        string ocrText = await reader.ReadToEndAsync();

                        // 一時ファイル削除
                        await service.Files.Delete(file.Id).ExecuteAsync();

                        return ocrText;
                    }
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

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                scopes,
                "user", // ユーザー識別子
                CancellationToken.None,
                new FileDataStore(this.credPath, true),
                new Google.Apis.Auth.OAuth2.LocalServerCodeReceiver() // ←任意ユーザー選択可能
            );

            Console.WriteLine("OAuth認証完了！");
            Console.WriteLine("ログインユーザー: " + credential.UserId);
            Console.WriteLine("アクセストークン: " + credential.Token.AccessToken);

            var dirInfo = new DirectoryInfo(this.credPath);
            dirInfo.Attributes &= ~FileAttributes.ReadOnly;

            Console.WriteLine("Enterで終了");
            Console.ReadLine();

        }
    }
}
