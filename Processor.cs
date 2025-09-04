using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using NLog.Targets;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms; // これを追加
using Tesseract;
using static Google.Apis.Requests.BatchRequest;
using static TranslationLens.LoggingHandler;

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

        private HttpClient client = null;

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

        /// <summary>
        /// 画像をGoogle DriveにアップロードしてOCRした結果を返す
        /// token.json が既にある前提
        /// </summary>
        /// <param name="imagePath">ローカル画像パス</param>
        /// <returns>OCR結果文字列</returns>
        internal async Task<string> OCRByGoogle(string imagePath)
        {
            // 非同期タスクや未処理例外の捕捉を強化
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Console.WriteLine("UnhandledException: " + e.ExceptionObject);
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Console.WriteLine("UnobservedTaskException: " + e.Exception);
                e.SetObserved(); // 例外を既知として扱う
            };

            try
            {
                // 既存トークンを使用して DriveService を初期化
                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    new ClientSecrets { ClientId = this.clientId, ClientSecret = this.clientSecret },
                    new[] { DriveService.Scope.Drive, DriveService.Scope.DriveFile },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(this.credPath, true)
                );

                var loggingHandler = new LoggingHandler(new HttpClientHandler());
                var service = new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "OCR Test App",
                    HttpClientFactory = new CustomHttpClientFactory(loggingHandler)
                });

                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = "tempOCR_" + Guid.NewGuid(),
                    MimeType = "application/vnd.google-apps.document"
                };

                FileStream stream = null;
                Google.Apis.Drive.v3.Data.File file = null;

                try
                {
                    stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);

                    var request = service.Files.Create(fileMetadata, stream, "image/png");
                    request.Fields = "id";
                    request.ChunkSize = ResumableUpload.MinimumChunkSize;

                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                    await request.UploadAsync(cts.Token);

                    file = request.ResponseBody;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("=== UploadAsync 例外 ===");
                    Console.WriteLine(ex.GetType());
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    throw;
                }
                finally
                {
                    stream?.Dispose();
                }

                try
                {
                    var exportRequest = service.Files.Export(file.Id, "text/plain");
                    using (var ms = new MemoryStream())
                    {
                        await exportRequest.DownloadAsync(ms);
                        ms.Position = 0;

                        using (var reader = new StreamReader(ms))
                        {
                            string ocrText = await reader.ReadToEndAsync();
                            await service.Files.Delete(file.Id).ExecuteAsync();
                            return ocrText;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("=== OCR / Export 例外 ===");
                    Console.WriteLine(ex.GetType());
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== OCRByGoogle 全体例外 ===");
                Console.WriteLine(ex.GetType());
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// 接続確認用のテストコード
        /// </summary>
        /// <returns></returns>
        internal async Task<string> OCRByGoogleTest(string imagePath)
        {
            string gasUrl = "https://script.google.com/macros/s/AKfycbzHo2ZjbR4HLiyi5k8HzvwAOWE3iwCQqsGHitY8_QKMGJlILsjq4YDbtaNFYYDyzz8jcg/exec";

            // テスト用に小さい画像データを固定
            var payload = new
            {
                filename = "tiny.png",
                data = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/wIAAgMBAY/6zFIAAAAASUVORK5CYII="
            };

            try
            {
                // HttpClientHandler を明示
                var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    UseCookies = true,
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
                };

                this.client = new HttpClient(handler);
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.DefaultRequestHeaders.AcceptEncoding.Clear();

                    // JsonSerializer で型を明示してシリアライズ
                    string json = JsonSerializer.Serialize(payload, typeof(object), new JsonSerializerOptions
                    {
                        WriteIndented = false
                    });

                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    try
                    {
                        using (var response = await client.PostAsync(gasUrl, content))
                        {
                            string responseText = await response.Content.ReadAsStringAsync();

                            Console.WriteLine("Status Code: " + response.StatusCode);
                            Console.WriteLine("Response Body:");
                            Console.WriteLine(responseText);

                            return responseText;
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        Console.WriteLine("タイムアウトしました (10秒)");
                        return null;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("例外が発生しました:");
                        Console.WriteLine(ex);
                        return ex.Message;
                    }
                finally
                {
                    this.client.Dispose();
                    this.client = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred:");
                Console.WriteLine(ex);
                return ex.Message;
            }
        }
    }



    /// <summary>
    /// 既存の HttpClient を DriveService に渡すためのファクトリ
    /// </summary>
    public class CustomHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public CustomHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public ConfigurableHttpClient CreateHttpClient(CreateHttpClientArgs args)
        {
            // ConfigurableHttpClient は HttpMessageHandler を渡して作成
            return new ConfigurableHttpClient(new ConfigurableMessageHandler(_handler));
        }
    }

    /// <summary>
    /// HTTP リクエスト/レスポンスをログ出力するハンドラ
    /// </summary>
    public class LoggingHandler : DelegatingHandler
    {
        public LoggingHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // リクエスト情報
            Console.WriteLine($"[Request] {request.Method} {request.RequestUri}");

            if (request.Content != null)
            {
                var length = request.Content.Headers.ContentLength ?? -1;
                Console.WriteLine($"[Request Content] {length} バイトのデータ");
            }

            // リクエスト送信
            var response = await base.SendAsync(request, cancellationToken);

            // レスポンス情報
            Console.WriteLine($"[Response] {(int)response.StatusCode} {response.ReasonPhrase}");

            if (response.Content != null)
            {
                var length = response.Content.Headers.ContentLength ?? -1;
                Console.WriteLine($"[Response Content] {length} バイトのデータ");
            }

            return response;
        }
    }
}
