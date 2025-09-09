using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TranslationLens.Models;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using WinFormsTimer = System.Windows.Forms.Timer;

namespace TranslationLens
{
    public partial class MainForm : Form
    {
        // フォームの外のクリックをっ検出する為のAPI
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(System.Windows.Forms.Keys vKey);
        private bool flgClick = false;

        private int characterLimit = 5000; // 1回の翻訳で送信できる最大文字数（Google翻訳APIの制限に合わせる）
        private string delimiter = "\\\\\\ ";// 区切り文字



        private WinFormsTimer clickTimer = new WinFormsTimer();

        // ロガーのインスタンスを作成
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

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

            SetStatus("ようこそ");

            this.clickTimer = new WinFormsTimer();
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
            int margin = 8;
            int topOffset = margin + MainMenu.Height; // MenuStripを避ける
            panel.Bounds = new Rectangle(
                margin,
                topOffset,
                this.ClientSize.Width - TextsTextBox.Width - margin * 2,
                this.ClientSize.Height - topOffset - margin
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

            var myString = await CallOcr(cts.Token); // これだけで OK

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
            /*
            ToolStripProgressBar1.Enabled = true;
 //           ToolStripProgressBar1.Style = ProgressBarStyle.Marquee;
 //           ToolStripProgressBar1.MarqueeAnimationSpeed = 30;
            ToolStripProgressBar1.Maximum = 100;

            cts = new CancellationTokenSource();

            this.TextsTextBox.Text = string.Empty;
            StatusStrip1.Text = string.Empty;

            try
            {
                this.UseWaitCursor = true;

                // スクリーンショット
                SetStatus("スクリーンショットを撮っています...");
                // プログレスバー更新
                ToolStripProgressBar1.Value += 25;

                await Task.Delay(50);
                await Task.Yield(); // ← ここで UI を即更新
                var bmp = TakeScreenshot();

                // OCR
                SetStatus("OCRを実行しています...");
                // プログレスバー更新
                ToolStripProgressBar1.Value += 25;

                await Task.Yield();
                await Task.Delay(50);
                var myString = await CallOcr(cts.Token);

                // 翻訳
                SetStatus("翻訳を実行しています...");
                // プログレスバー更新
                ToolStripProgressBar1.Value += 25;
                await Task.Yield();
                await Task.Delay(50);
                var texts = TextSplitter.SplitSentences(myString);

                Logger.Info($"翻訳対象の文数: {texts.Count}");

                // プログレスバー初期化（ここからプログレスバーは翻訳専用に使う）
                ToolStripProgressBar1.Minimum = 0;
                ToolStripProgressBar1.Maximum = texts.Count;
                ToolStripProgressBar1.Value = 0;

                var progress = new Progress<int>(doneCount =>
                {
                    ToolStripProgressBar1.Value = doneCount;
                });

                //var japaneseList = await this.processor.TranslateListAsync(texts, cts.Token);
                var japaneseList = await processor.TranslateListAsync(texts, cts.Token, progress);
                // 結果表示
                SetStatus("結果の表示中");

                var result = new List<TranslationResult>();
                for (int i = 0; i < texts.Count; i++)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    result.Add(new TranslationResult(texts[i], japaneseList[i]));
                }
                SetResltToTextBox(result);

                SetStatus("完了しました。");
            }
            catch (OperationCanceledException)
            {
                SetStatus("キャンセルされました。");
            }
            finally
            {
                this.UseWaitCursor = false;
                cts = null;
            }
            */
        }

        /// <summary>
        /// ステータスバーにメッセージを表示する
        /// </summary>
        /// <param name="text">メッセージ</param>
        private void SetStatus(string text)
        {
            Logger.Debug(text);
            Console.WriteLine(text);
            ToolStripStatusLabel1.Text = text;
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

            var japaneseList = await this.processor.TranslateListAsync(texts, cts.Token);
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

            // 行ごとに分解する
            var sp = this.TextsTextBox.Text.Split('\n');

            var rtb = this.TextsTextBox;
            var lines = rtb.Lines;
            var starts = GetLogicalLineStarts(rtb);

            for (var index = 0; index < sp.Length; ++index)
            {
                // 元文行、訳文行。空欄なので、3で割った余りで判定する
                var flg = (index + 1) % 3;
                int start = starts[index];         // ←論理行の正しい開始
                int length = lines[index].Length;   // ←論理行の長さ
                                                    // var text = this.TextsTextBox.Lines[index];
                                                    // Console.WriteLine($"flg = {flg}、index={index}, start={start}, length={length}, text={text}");

                var foreColor = Color.Black;
                // 選択
                this.TextsTextBox.Select(start, length);
                if (flg == 1)
                {
                    //                 Console.WriteLine($"元文行: {this.TextsTextBox.SelectedText}");
                    foreColor = Color.Blue; // 元文行
                }
                else if (flg == 2)
                {
                    //                   Console.WriteLine($"訳文行: {this.TextsTextBox.SelectedText}");
                    // 現在のフォントを取得し、太字スタイルを適用します
                    var font = TextsTextBox.SelectionFont;
                    FontStyle newStyle = font.Style | FontStyle.Bold; // 既存のスタイルに太字を追加

                    // 新しいフォントオブジェクトを作成して設定します
                    TextsTextBox.SelectionFont = new Font(font.FontFamily, font.Size, newStyle);

                    foreColor = Color.Black; // 訳文行
                }
                else
                {
                    //                Console.WriteLine($"空行: {this.TextsTextBox.SelectedText}");
                    foreColor = Color.Gray; // 空行
                }

                // 色を変更
                this.TextsTextBox.SelectionColor = foreColor;


                // 選択解除
                this.TextsTextBox.Select(0, 0);
                this.TextsTextBox.SelectionColor = this.TextsTextBox.ForeColor; // 元に戻す
            }
        }

        /// <summary>
        /// 翻訳への送信回数を減らすため、テキストリストをグルーピングする
        /// </summary>
        /// <param name="baseList">グルーピングするテキストリスト</param>
        /// <returns>グループ化した結果</returns>
        private List<string> GroupingTextList(List<string> baseList)
        {
            var ret = new List<string>();
            var sb = new StringBuilder();

            foreach (var text in baseList)
            {
                // もし追加したら制限オーバーになるなら、ここで確定
                if (sb.Length > 0 && sb.Length + this.delimiter.Length + text.Length > this.characterLimit)
                {
                    ret.Add(sb.ToString());
                    sb.Clear();
                }

                if (sb.Length > 0)
                {
                    sb.Append(this.delimiter);
                }

                sb.Append(text);
            }

            // 最後のバッファを追加
            if (sb.Length > 0)
            {
                ret.Add(sb.ToString());
            }

            return ret;
        }

        /// <summary>
        /// 翻訳結果を元のリストに分解する
        /// </summary>
        /// <param name="translatedGroups">翻訳後のグループ化された文字列</param>
        /// <param name="groupSizes">それぞれのグループに含まれる元の要素数</param>
        /// <returns>元の件数に分割された翻訳結果リスト</returns>
        private List<string> UngroupingTextList(List<string> translatedGroups, List<int> groupSizes)
        {
            var ret = new List<string>();
            int idx = 0;

            foreach (var group in translatedGroups)
            {
                // なんか上手く分割できないんで、前処理してから分割する
                var text = group.Replace('｜', '|')   // 全角パイプを半角に
           .Replace("\u200B",string.Empty) // ゼロ幅スペース除去
           .Normalize(NormalizationForm.FormKC);

                // 分割
                var parts = SplitByDelimiter(text,this.delimiter).ToList();

                //var buf = parts.Any(x => x.Contains(this.delimiter));
                // 想定件数とズレる場合は調整
                if (parts.Count < groupSizes[idx])
                {
                    // 足りない → 最後に空文字を追加
                    while (parts.Count < groupSizes[idx])
                    {
                        parts.Add(string.Empty);
                    }
                }
                else if (parts.Count > groupSizes[idx])
                {
                    // 多い → 余分を最後の要素にまとめる
                    var extra = string.Join(" ", parts.Skip(groupSizes[idx] - 1));
                    parts = parts.Take(groupSizes[idx] - 1).ToList();
                    parts.Add(extra);
                }

                ret.AddRange(parts);
                idx++;
            }

            return ret;
        }

        /// <summary>
        /// 文字列を「文字列で」分割する処理
        /// </summary>
        /// <param name="text">分割するテキスト</param>
        /// <param name="delimiter">区切り文字列</param>
        /// <returns>分割結果</returns>
        private List<string> SplitByDelimiter(string text, string delimiter)
        {
            var result = new List<string>();
            int start = 0;

            while (start < text.Length)
            {
                int idx = text.IndexOf(delimiter, start, StringComparison.Ordinal);
                if (idx == -1)
                {
                    // 区切り文字が見つからなければ残り全部
                    result.Add(text.Substring(start));
                    break;
                }

                // 区切り文字までの部分を切り出す
                result.Add(text.Substring(start, idx - start));

                // 次の検索開始位置は区切り文字の直後
                start = idx + delimiter.Length;
            }


            var errors = result.Where(x => x.Contains(delimiter)).ToList();

            foreach (var err in errors)
            {
                // なんか失敗してる
                Logger.Warn($"SplitByDelimiter: うまく分割できてない。text={err}, delimiter={delimiter}");

                var buf = SplitByDelimiter(err, delimiter);
            }

            return result;
        }

        // 論理行（Lines[i]）の開始位置を全部求める
        private static int[] GetLogicalLineStarts(RichTextBox rtb)
        {
            var text = rtb.Text;
            var lines = rtb.Lines;
            var starts = new int[lines.Length];

            int pos = 0; // text 内の走査位置
            for (int i = 0; i < lines.Length; i++)
            {
                starts[i] = pos;                 // この論理行の開始
                pos += lines[i].Length;          // 行本体ぶん進める

                // 行末の改行（\n または \r\n）をスキップ
                if (pos < text.Length)
                {
                    if (text[pos] == '\r') { pos++; if (pos < text.Length && text[pos] == '\n') pos++; }
                    else if (text[pos] == '\n') { pos++; }
                }
            }
            return starts;
        }

        private void TextsTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {

        }


        /// <summary>
        /// 翻訳開始（Async）
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        private async void ReadyButton_Click_Async(object sender, EventArgs e)
        {
            // ボタンは消す
            ReadyButton.Visible = false;

            // 翻訳開始
            await Translate();
        }

        /// <summary>
        /// 翻訳処理
        /// </summary>
        /// <returns>Task</returns>
        private async Task<Bitmap> Translate()
        {
            ToolStripProgressBar1.Enabled = true;
            ToolStripProgressBar1.Maximum = 100;

            cts = new CancellationTokenSource();

            this.TextsTextBox.Text = string.Empty;
            StatusStrip1.Text = string.Empty;
            try
            {
                this.UseWaitCursor = true;

                // スクリーンショット
                SetStatus("スクリーンショットを撮っています...");
                // プログレスバー更新
                ToolStripProgressBar1.Value += 25;

                await Task.Delay(50);
                await Task.Yield(); // ← ここで UI を即更新
                var bmp = TakeScreenshot();

                // OCR
                SetStatus("OCRを実行しています...");
                // プログレスバー更新
                ToolStripProgressBar1.Value += 25;

                await Task.Yield();
                await Task.Delay(50);
                var myString = await CallOcr(cts.Token);

                // 翻訳
                SetStatus("翻訳を実行しています...");
                // プログレスバー更新
                ToolStripProgressBar1.Value += 25;
                await Task.Yield();
                await Task.Delay(50);
                var texts = TextSplitter.SplitSentences(myString);

                var sendTexts = GroupingTextList(texts);

                Logger.Info($"翻訳対象の文数: {sendTexts.Count}");

                // プログレスバー初期化（ここからプログレスバーは翻訳専用に使う）
                ToolStripProgressBar1.Minimum = 0;
                ToolStripProgressBar1.Maximum = texts.Count;
                ToolStripProgressBar1.Value = 0;

                var progress = new Progress<int>(doneCount =>
                {
                    ToolStripProgressBar1.Value = doneCount;
                });

                //var japaneseList = await this.processor.TranslateListAsync(texts, cts.Token);

                var resultList = await processor.TranslateListAsync(sendTexts, cts.Token, progress);

                var groupSizes = sendTexts.Select(t => t.Split(new[] { this.delimiter }, StringSplitOptions.None).Length).ToList();
                var translatedTexts = UngroupingTextList(resultList, groupSizes);

                // 結果表示
                SetStatus("結果の表示中");

                var result = new List<TranslationResult>();
                for (int i = 0; i < texts.Count; i++)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    result.Add(new TranslationResult(texts[i], translatedTexts[i]));
                }
                SetResltToTextBox(result);

                SetStatus("完了しました。");
                return bmp;
            }
            catch (OperationCanceledException)
            {
                SetStatus("キャンセルされました。");
            }
            finally
            {
                this.UseWaitCursor = false;
                cts = null;
            }
            return null;
        }
    }
}
