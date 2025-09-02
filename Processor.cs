using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Drawing;
using System.Threading;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms; // これを追加


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

        internal string Translate(string filePath)
        {

            // ChromeDriver を Chrome のバージョンに合わせて自動セットアップ
            new DriverManager().SetUpDriver(new ChromeConfig());

            // Chrome オプション
            var options = new ChromeOptions();
            options.AddArgument("--start-maximized");
            // options.AddArgument("--headless"); // ユーザーに見せない場合

            using (var driver = new ChromeDriver(options))
            {
                try
                {
                    // 画像ファイルを指定
                    var imagePath = Path.GetFullPath(filePath);

                    // Google翻訳の画像翻訳ページ
                    driver.Navigate().GoToUrl("https://translate.google.com/?sl=auto&tl=en&op=docs");

                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                    // input[type=file] を探す
                    var uploadInput = wait.Until(drv => drv.FindElement(By.CssSelector("input[type='file']")));

                    // hidden を解除
                    IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                    js.ExecuteScript(
                        "arguments[0].style.display='block'; arguments[0].style.visibility='visible';",
                        uploadInput
                    );

                    // ファイルを送信
                    uploadInput.SendKeys(imagePath);

                    // 結果部分をスクリーンショット
                    var resultElement = wait.Until(drv =>
                    {
                        try
                        {
                            var elem = drv.FindElement(By.CssSelector("div.result-container"));
                            return elem.Displayed ? elem : null; // 表示されるまで null を返す
                        }
                        catch (NoSuchElementException)
                        {
                            return null;
                        }
                    });

                    Screenshot screenshot = ((ITakesScreenshot)resultElement).GetScreenshot();
                    screenshot.SaveAsFile("translated_result.png"); // ✅

                    return "translated_result.png";
                }
                catch (Exception ex)
                {
                    Logger.Error("エラー: " + ex.Message);
                    MessageBox.Show("エラーが発生しました: " + ex.Message);
                }
                finally
                {
                    // Chrome を閉じる
                    driver.Quit();
                }
                return string.Empty;
            }

        }
    }
}
