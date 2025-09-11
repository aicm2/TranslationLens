using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms; // これを追加
using TranslationLens.Models;

namespace TranslationLens
{
    internal class Processor
    {
        // ロガーのインスタンスを作成
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        internal Configs Configs { get; set; } = null;

        // トークン保存場所
        private string credPath;

        // OCR用のgas(現在未使用)
        // private readonly string gASURL = "https://script.google.com/macros/s/AKfycbwIbppF5BsIZ7BkewR3pXFN_eK0ExnUpy90GNV2JGpgc-hYs0itzDkcDVWdsa_CwEVEFA/exec";

        /// <summary>
        /// 翻訳用のgas
        /// </summary>
        private readonly string gasURL_T = "https://script.google.com/macros/s/AKfycbzoyWSXgC_Exu1p8yRtjlbeCnczFX-TI_jYtbjIcMHnCOLTUixKnV6Zw-HfzGAIz52xTw/exec";

        private readonly string clientId = "629337539653-pdu7usoru0lb4e7fg83du66i54ph7e4v.apps.googleusercontent.com";
        private readonly string clientSecret = "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";


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

            this.Configs = LoadConfig();
        }

        /// <summary>
        /// 設定の読み込み
        /// </summary>
        /// <returns>設定のインスタンス</returns>
        private Configs LoadConfig()
        {
            var path = Configs.ConfigPath;

            var jsonString = CommonMethodLight.InputUtf8(path);

            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return new Configs();
            }

            return JsonSerializer.Deserialize<Configs>(jsonString);
        }

        /// <summary>
        /// 設定ファイルを保存する
        /// </summary>
        internal void SaveConfig()
        {
            if (this.Configs == null)
            {
                // 設定のNullクリアを防ぐ
                return;
            }

            var path = Configs.ConfigPath;

            var jsonString = JsonSerializer.Serialize(this.Configs);

            CommonMethodLight.OutputUtf8(path, jsonString);
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
            // スコープ設定
            string[] scopes = { DriveService.Scope.Drive, DriveService.Scope.DriveFile };

            // 既存トークンを使って認証
            UserCredential credential;
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = this.clientId,
                    ClientSecret = this.clientSecret,
                },
                scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(this.credPath, true)
            ).ConfigureAwait(false); // GUIスレッドデッドロック回避

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

            Google.Apis.Drive.v3.Data.File file;

            using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
            {
                var request = service.Files.Create(fileMetadata, stream, "image/png");
                request.Fields = "id";

                try
                {
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    await request.UploadAsync(cts.Token).ConfigureAwait(false);
                    file = request.ResponseBody;
                }
                catch (Google.GoogleApiException ex)
                {
                    Console.WriteLine($"StatusCode: {ex.HttpStatusCode}");
                    Console.WriteLine($"Error: {ex.Error.Message}");
                    throw;
                }
            }

            // OCR結果をテキストで取得
            var exportRequest = service.Files.Export(file.Id, "text/plain");
            using (var ms = new MemoryStream())
            {
                await exportRequest.DownloadAsync(ms).ConfigureAwait(false);
                ms.Position = 0;

                using (var reader = new StreamReader(ms))
                {
                    string ocrText = await reader.ReadToEndAsync().ConfigureAwait(false);

                    // 一時ファイル削除
                    await service.Files.Delete(file.Id).ExecuteAsync().ConfigureAwait(false);

                    return ocrText;
                }
            }
        }

        private HttpClient client_t = null;

        /// <summary>
        /// テキスト翻訳の処置（非同期）
        /// </summary>
        /// <param name="text">元テキスト</param>
        /// <param name="source">翻訳元の言語</param>
        /// <param name="target">翻訳先の言語</param>
        /// <returns>翻訳結果</returns>
        internal async Task<string> TranslateAsync(string text, string source = "en", string target = "ja")
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            this.client_t = this.client_t ?? new HttpClient();

            var url = $"{this.gasURL_T}?text={Uri.EscapeDataString(text)}&source={source}&target={target}";
            return await this.client_t.GetStringAsync(url);
        }

        /// <summary>
        /// テキストリスト翻訳の処置（非同期）
        /// </summary>
        /// <param name="texts">元テキストリスト</param>
        /// >param name="token">キャンセルトークン
        /// <param name="progress">進捗報告用</param>
        /// <param name="source">翻訳元の言語</param>
        /// <param name="target">翻訳先の言語</param>
        /// <returns>翻訳結果のリスト</returns>
        internal async Task<List<string>> TranslateListAsync(
            List<string> texts,
            CancellationToken token,
            IProgress<int> progress = null,
            string source = "en",
            string target = "ja")
        {
            try
            {
                var results = new List<string>();

                for (int i = 0; i < texts.Count; i++)
                {
                    token.ThrowIfCancellationRequested();

                    var translated = await TranslateAsync(texts[i], source, target);
                    results.Add(translated);

                    // 今何件終わったかを報告（1件目なら1、2件目なら2…）
                    progress?.Report(i + 1);
                }

                return results;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
                return null;
            }
        }

        /// <summary>
        /// 画像をGoogle DriveにアップロードしてOCRした結果を返す（通信テスト用）
        /// token.json が既にある前提
        /// </summary>
        /// <param name="imagePath">ローカル画像パス</param>
        /// <returns>OCR結果文字列</returns>
        internal async Task<string> OCRByGoogleTest(string imagePath)
        {
            // 既存ハンドラ + ロギング
            var handler = new LoggingHandler(new HttpClientHandler());

            using (var client = new HttpClient(handler))
            {
                client.Timeout = TimeSpan.FromMinutes(2); // 念のため長め

                var payload = new
                {
                    filename = "tiny.png",
                    data = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/wIAAgMBAY/6zFIAAAAASUVORK5CYII="
                };

                string jsonBody = JsonSerializer.Serialize(payload);
                Console.WriteLine("JSON Body 作成完了:");
                Console.WriteLine(jsonBody);

                using (var content = new StringContent(jsonBody, Encoding.UTF8, "application/json"))
                {
                    Logger.Info("StringContent 作成完了");

                    try
                    {

                        Logger.Info("送信開始...");
                        var response = await client.PostAsync(
                            "https://script.google.com/macros/s/AKfycbzHo2ZjbR4HLiyi5k8HzvwAOWE3iwCQqsGHitY8_QKMGJlILsjq4YDbtaNFYYDyzz8jcg/exec",
                            content
                        ).ConfigureAwait(false);
                        Console.WriteLine("送信完了");

                        response.EnsureSuccessStatusCode();

                        string result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        Logger.Info("レスポンス取得完了:");
                        Logger.Info(result);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("エラー発生:");
                        Console.WriteLine(ex);
                        return null;
                    }
                }
            }
        }

    }

    /// <summary>
    /// 既存の HttpClient を DriveService に渡すためのファクトリ
    /// </summary>
    public class CustomHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _baseHandler;

        public CustomHttpClientFactory()
        {
            // 内側の HttpClientHandler に TLS 1.2 を指定
            var httpHandler = new HttpClientHandler
            {
                SslProtocols = SslProtocols.Tls12,
                UseProxy = true,
                Proxy = WebRequest.DefaultWebProxy,
                UseDefaultCredentials = true
            };

            // LoggingHandler でラップ
            _baseHandler = new LoggingHandler(httpHandler);
        }

        public ConfigurableHttpClient CreateHttpClient(CreateHttpClientArgs args)
        {
            // ConfigurableHttpClient にハンドラを渡す
            return new ConfigurableHttpClient(new ConfigurableMessageHandler(_baseHandler));
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
