using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TranslationLens.Models;
using WinFormsTimer = System.Windows.Forms.Timer;

namespace TranslationLens
{
    public partial class MainForm : Form
    {
        // フォームの外のクリックをっ検出する為のAPI
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(System.Windows.Forms.Keys vKey);
        private bool flgClick = false;

        private WinFormsTimer clickTimer = new WinFormsTimer();

        // OCRや翻訳のキャンセル用
        private CancellationTokenSource cts;

        private Panel panel;
        // Formの端をドラッグしてサイズ変更するためのクラス(効かない)
        private FormDragResizer formResizer;

        private readonly string tempPngPath = "screenshot.png";

        private Processor processor = null;

        internal MainForm(Processor processor)
        {
            this.processor = processor;
            InitializeComponent();

            // Formのイニシャル処理で生成する
        //    formResizer = new FormDragResizer(this, FormDragResizer.ResizeDirection.All, 8);

        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            panel = new Panel();
            panel.BackColor = Color.Magenta;
            this.Controls.Add(panel);

            this.TransparencyKey = Color.Magenta;
            this.TopMost = true;

            AdjustPanelBounds();
            this.Resize += (s, ev) => AdjustPanelBounds();

            this.clickTimer= new Timer();
            this.clickTimer.Tick += Timer1_Tick;
            this.clickTimer.Interval = 300; // 500ミリ秒ごとにチェック

        }

        /// <summary>
        /// 画面外クリック検出用のタイマー
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (GetAsyncKeyState(Keys.LButton) != 0)
            {
                if (flgClick == false)
                {
                    // 画面外がクリックされた
                    StatusStrip1.Text = "画面外クリック検出";
                    flgClick = true;
                }
            }
            else
            {
                flgClick = false;
            }
        }

        private void AdjustPanelBounds()
        {
            int margin = 8; // 枠を残す幅
            panel.Bounds = new Rectangle(
                margin,
                margin,
                this.ClientSize.Width- TextsTextBox.Width - margin * 2,
                this.ClientSize.Height - margin * 2
            );
        }

        /// <summary>
        /// 試験用のスクリーンショット撮影処理
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        private void ScreenShotMenu_Click(object sender, EventArgs e)
        {
            var bmp = TakeScreenshot();
        }

        private Bitmap TakeScreenshot()
        {
            this.Cursor = Cursors.WaitCursor;

            // Panelのスクリーン上の座標を取得
            Point panelScreenPos = panel.PointToScreen(Point.Empty);

            // Rectangleを作成
            Rectangle rect = new Rectangle(
                panelScreenPos.X,
                panelScreenPos.Y,
                panel.Width,
                panel.Height
            );

            // snap関数を呼び出す
            Bitmap bmp = processor.snap(rect);

            // 例：ファイルに保存する場合
            bmp.Save(this.tempPngPath, System.Drawing.Imaging.ImageFormat.Png);
            return bmp;
        }

        /// <summary>
        /// 翻訳（テスト）同期版
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        private async void MenuTransLate_Click_Async(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;

            var myString = await CallOcr(); // これだけで OK

            this.Invoke((Action)(() =>
            {
                var texts = TextSplitter.SplitSentences(myString);

                Console.WriteLine($"result = {myString}");
                Console.WriteLine($"----------------------------------------------------------");

                foreach (var text in texts)
                {
                    Console.WriteLine($"sentence: {text}");
                }

                TextsTextBox.Text = string.Join("------------------\n", texts);

                MessageBox.Show("OK");
            }));
        }

        private async Task<string> CallOcr(CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested(); // OCR前にキャンセルチェック

                var imagePath = Path.GetFullPath(this.tempPngPath);

                // OCR呼び出し
                var myString = await this.processor.OCRByGoogle(imagePath);

                token.ThrowIfCancellationRequested(); // OCR後にキャンセルチェック

                Console.WriteLine($"result = {myString}");
                return myString;
            }
            catch (OperationCanceledException)
            {
                // キャンセル時の処理
                StatusStrip1.Text = "OCRがキャンセルされました";
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
                return null;
            }
        }


        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
        }

        private void TextsTextBox_DoubleClick(object sender, EventArgs e)
        {
            // スクリーンショットを撮る
            var bmp = TakeScreenshot();

        }

        private async void TextsTextBox_DoubleClick_Async(object sender, EventArgs e)
        {
            cts = new CancellationTokenSource();

            this.TextsTextBox.Text = string.Empty;
            StatusStrip1.Text = string.Empty;
            try
            {
                this.UseWaitCursor = true;

                StatusStrip1.Text = "スクリーンショットを撮っています...";
                var bmp = TakeScreenshot();

                StatusStrip1.Text = "OCRを実行しています...";
                var myString = await CallOcr(cts.Token);   // ← トークンを渡す

                StatusStrip1.Text = "翻訳を実行しています...";
                var texts = TextSplitter.SplitSentences(myString);

                var japaneseList = await this.processor.TranslateListAsync(texts, cts.Token);

                var result = new List<TranslationResult>();
                for (int i = 0; i < texts.Count; i++)
                {
                    // キャンセルされていたら例外が出る
                    cts.Token.ThrowIfCancellationRequested();
                    result.Add(new TranslationResult(texts[i], japaneseList[i]));
                }

                SetResltToTextBox(result);
                StatusStrip1.Text = "完了しました。";
            }
            catch (OperationCanceledException)
            {
                StatusStrip1.Text = "キャンセルされました。";
            }
            finally
            {
                this.UseWaitCursor = false;
                cts = null;
            }
        }

        /// <summary>
        /// スクリーンショットのテスト
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        private async void TextsTextBox_DoubleClick__Async(object sender, EventArgs e)
        {
            var bmp = TakeScreenshot();


        }

        /// <summary>
        /// テキストファイルの翻訳呼び出し（テスト）（非同期版）
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        private async void MemuTranslationText_Click_Async(object sender, EventArgs e)
        {
            var path = "baseText.txt";
            var texts = new List<string>();
            System.IO.StreamReader sr = new System.IO.StreamReader(path);
            while (sr.Peek() > -1)
            {
                texts.Add(sr.ReadLine());
            }
            // 閉じる
            sr.Close();

            var japaneseList = await this.processor.TranslateListAsync(texts,cts.Token);
            var result = new List<TranslationResult>();
            for (int i = 0; i < texts.Count; i++)
            {
                result.Add(new TranslationResult(texts[i], japaneseList[i]));
            }
            SetResltToTextBox(result);
        }

        /// <summary>
        /// 結果をRichTextBoxにセットする
        /// </summary>
        /// <param name="result">結果</param>
        private void SetResltToTextBox(List<TranslationResult> result)
        {
            this.TextsTextBox.Text = string.Join("\n\n", result);

        }

        private void TextsTextBox_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
