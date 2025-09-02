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
            // options.AddArgument("--headless"); // UI が必要なのでオフ

            using (var driver = new ChromeDriver(options))
            {
                try
                {
                    var imagePath = Path.GetFullPath(filePath);

                    // Google翻訳のドキュメント翻訳ページを開く
                    driver.Navigate().GoToUrl("https://translate.google.com/?sl=auto&tl=en&op=docs");

                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                    // 「ファイルを選択」ボタンをクリックして OS ダイアログを開く
                    var selectButton = wait.Until(drv => drv.FindElement(By.CssSelector("button[jsname='V67aGc']")));
                    selectButton.Click();

                    // ★ここで AutoIt や UIAutomation を呼び出して OS ダイアログ操作★
                    // AutoIt の例：
                    // System.Diagnostics.Process.Start(@"C:\path\to\upload.au3");
                    // upload.au3 の中でファイルパスを入力して Enter

                    // ダイアログ操作が完了するまで少し待つ
                    Thread.Sleep(3000);

                    // 翻訳結果の表示を待つ
                    var resultElement = new WebDriverWait(driver, TimeSpan.FromSeconds(30)).Until(drv =>
                    {
                        try
                        {
                            // Google 翻訳は結果が iframe 内に出る場合もあるので注意
                            var elem = drv.FindElement(By.CssSelector("div.result-container"));
                            return elem.Displayed ? elem : null;
                        }
                        catch
                        {
                            return null;
                        }
                    });

                    // 結果のスクリーンショットを保存
                    Screenshot screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                    var savePath = Path.Combine(Environment.CurrentDirectory, "translated_result.png");
                    screenshot.SaveAsFile(savePath);

                    return savePath;
                }
                catch (Exception ex)
                {
                    Logger.Error("エラー: " + ex.Message);
                    MessageBox.Show("エラーが発生しました: " + ex.Message);
                    return string.Empty;
                }
                finally
                {
                    driver.Quit();
                }
            }
        }
    }
}
