using NLog;
using System;
using System.Collections.Generic;
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

        private int characterLimit = 5000; // 1回の翻訳で送信できる最大文字数（Google翻訳APIの制限に合わせる）
        private string delimiter = "|||";// 区切り文字

        // 自動スナップ用タイマー
        private WinFormsTimer snapTimer = new WinFormsTimer();

        // 最後に翻訳したスクリーンショット画像
        private Bitmap lastSnap = null;

        // 翻訳処理中フラグ。 trueの間は次の翻訳処理を受け付けない
        private bool IsProcessing = false;

        // 自動スクリーンショットに対する処理を開始したかどうかのフラグ
        //（連続して自動スクリーンショットは撮らない）
        private bool IsStartProcess = false;

        // 画面外クリック検出用タイマー
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

            // 画面外クリック検出用タイマーの設定
            this.clickTimer = new WinFormsTimer();
            this.clickTimer.Tick += ClickTimer_Tick;
            this.clickTimer.Interval = 300; // 500ミリ秒ごとにチェック
            this.clickTimer.Start();

            // 自動スナップ用タイマーの設定
            this.snapTimer = new WinFormsTimer();
            this.snapTimer.Tick += SnapTimer_Tick;
            this.snapTimer.Interval = 1000; // 1000ミリ秒ごとにチェック
            
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            // フォームを配置する
            SetLocation();
        }

        /// <summary>
        /// フォームの位置とサイズを設定する
        /// </summary>
        private void SetLocation()
        {
            var c = this.processor.Configs;

            if (c == null || c.FormWidth <= 0)
            {
                // 無効な設定
                return;
            }

            // 位置とサイズを設定する
            this.Top = c.FormTop;
            this.Left = c.FormLeft;
            this.Width = c.FormWidth;
            this.Height = c.FormHeight;
        }

        /// <summary>
        /// フォームの位置とサイズを保存する
        /// </summary>
        private void SaveLocation()
        {
            var c = this.processor.Configs　?? new Configs();

            // 位置とサイズをk格納する
            c.FormTop = this.Top;
            c.FormLeft = this.Left;
            c.FormWidth = this.Width;
            c.FormHeight = this.Height;
        }

        /// <summary>
        /// 画面外クリック検出用のタイマー
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        private void ClickTimer_Tick(object sender, EventArgs e)
        {
            if (GetAsyncKeyState(Keys.LButton) != 0)
            {
                if (CusolOnForm())
                {
                    // フォーム上がクリックされた
                    return;
                }

                if (flgClick == false)
                {
                    // 画面外がクリックされた
                    StatusStrip1.Text = "画面外クリック検出";
                    flgClick = true;
 //                   MessageBox.Show(this,"画面外クリック検出", "画面外クリックを検出しました。");
                }
            }
            else
            {
                flgClick = false;
            }
        }

        /// <summary>
        /// マウスカーソルがフォーム上にあるかどうかを調べる
        /// </summary>
        /// <returns>フォーム上にある</returns>
        private bool CusolOnForm()
        {   var ret = false;
            var pos = System.Windows.Forms.Cursor.Position;

            if (pos.X >= this.Left && pos.X <= this.Right && pos.Y >= this.Top && pos.Y <= this.Bottom)
            {
                ret = true;
            }
            return ret;
        }

        /// <summary>
        /// 自動スナップ用タイマー
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        private async void SnapTimer_Tick(object sender, EventArgs e)
        {
            if(this.lastSnap == null)
            {
                return;
            }

            if (!this.IsProcessing && !this.IsStartProcess)
            {
                // 処理中でない場合

                // 新しいスクリーンショットを撮る
                var newSnap = TakeScreenshot();

                if (!this.processor.IsDifferentImages(this.lastSnap, newSnap))
                {
                    // 画像は変わっていない
                    return;
                }

                // 画像に差分があった

                Logger.Info("ページが切り替わった");
                SetStatus("ページが変わりました。");

                // 念のため、ここでもフラグを立てる
                this.IsStartProcess = true;

                try
                {
                    // 翻訳を行う
                    this.lastSnap = await Translate(newSnap);
                }
                catch(Exception ex)
                {
                    Logger.Error(ex);
                    MessageBox.Show(this, ex.Message);
                }


                // 念のため、ここでフラグを落とす
                this.IsStartProcess = false;
            }
        }

        private void AdjustPanelBounds()
        {
            // メニューとステータスバーを避けるようにパネルの位置とサイズを調整
            int margin = 8;
            int topOffset = margin + MainMenu.Height; // MenuStripを避ける
            int bottomOffset = margin + StatusStrip1.Height; // StatusStripを避ける

            panel.Bounds = new Rectangle(
                margin,
                topOffset,
                this.ClientSize.Width - TextsTextBox.Width - margin * 2,
                this.ClientSize.Height - topOffset - bottomOffset
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
                var texts = SplitSentences(myString);

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

        /// <summary>
        /// テキストを文章単にに分割する
        /// </summary>
        /// <param name="text">元のテキスト</param>
        /// <returns>分割したリスト</returns>
        private List<string> SplitSentences(string text)
        {
            switch(this.processor.Configs.SourceLang)
            {
                case "en":
                    return TextSplitter.SplitSentencesEn(text);
                case "ja":
                    // 日本語用の分割関数を実装するか、別のライブラリを使用する
                    // ここでは仮に英語用の関数を使うことにします
                    return TextSplitter.SplitSentencesEn(text);
                default:
                    // デフォルトは英語用の関数を使う
                    return TextSplitter.SplitSentencesEn(text);
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
            SetResultToTextBox(result);
        }

        /// <summary>
        /// 結果をRichTextBoxにセットする
        /// </summary>
        /// <param name="result">結果</param>
        private void SetResultToTextBox(List<TranslationResult> result)
        {
            this.TextsTextBox.Text = string.Join("\n", result);

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
                    foreColor = Color.Blue; // 元文行
                }
                else if (flg == 2)
                {
                    // 現在のフォントを取得し、太字スタイルを適用します
                    var font = TextsTextBox.SelectionFont ?? TextsTextBox.Font;
                    FontStyle newStyle = font.Style | FontStyle.Bold;
                    TextsTextBox.SelectionFont = new Font(font, newStyle);

                    foreColor = Color.Black; // 訳文行
                }
                else
                {
                    foreColor = Color.Gray; // 空行
                }

                // 色を変更
                this.TextsTextBox.SelectionColor = foreColor;

                // 選択解除
                this.TextsTextBox.Select(0, 0);
                this.TextsTextBox.SelectionColor = this.TextsTextBox.ForeColor; // 元に戻す
            }
            this.TextsTextBox.Invalidate();        // 再描画
            this.TextsTextBox.Update();            // 即時描画
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.processor.SaveConfig();
        }

        /// <summary>
        /// formのサイズ変更終了イベント
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        private void MainForm_ResizeEnd(object sender, EventArgs e)
        {
            SaveLocation();
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

            // 自動スナップ処理を停止
            this.snapTimer.Stop();

            // 翻訳開始
            this.lastSnap = await Translate();
        }

        // #######################################################
        // ↑ここまでフォーム上の処理（イベントハンドラとか）
        // ↓ここからアプリの本体処理
        // #######################################################

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

        /// <summary>
        /// 翻訳処理
        /// </summary>
        /// <param name="bmp">スクリーンショット画像</param>
        /// <returns>Task</returns>
        private async Task<Bitmap> Translate(Bitmap bmp = null)
        {
            // 実行中フラグを立てる
            this.IsProcessing = true;

            ToolStripProgressBar1.Enabled = true;
            ToolStripProgressBar1.Maximum = 100;

            cts = new CancellationTokenSource();

            this.TextsTextBox.Text = string.Empty;
            StatusStrip1.Text = string.Empty;
            try
            {
                this.UseWaitCursor = true;

                if (bmp == null)
                {
                    // スクリーンショット
                    SetStatus("スクリーンショットを撮っています...");
                    await Task.Yield(); // ← ここで UI を即更新
                    bmp = TakeScreenshot();
                }

                // プログレスバー更新
                ToolStripProgressBar1.Value += 25;

                await Task.Delay(50);

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
                var texts = SplitSentences(myString);

                if (texts.Any(x => x.Contains(this.delimiter)))
                {
                    // デリミタが含まれていると分割に失敗するので「タブスペース」に変更する
                    Logger.Warn($"区切り文字 '{this.delimiter}' が翻訳対象に含まれているので、タブ文字に変更しました。");
                    this.delimiter = "\t";
                }

                var sendTexts = GroupingTextList(texts);

                // 一度テキストに保存して読み直す
                var buf = string.Join("\n", sendTexts);
                CommonMethodLight.OutputUtf8("sendTexts.txt",buf);

                // テキストから読み直す
                sendTexts = CommonMethodLight.InputUtf8("sendTexts.txt").Split('\n').ToList();

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

                // 結果表示
                SetStatus("結果の表示中");

                // 結果をテキストに保存して読み直す
                buf = string.Join("\n", resultList);
                CommonMethodLight.OutputUtf8("translated.txt", buf);

　               resultList = CommonMethodLight.InputUtf8("translated.txt").Split('\n').ToList();

                var translatedTexts = UngroupingTextList(resultList, groupSizes);


                var result = new List<TranslationResult>();
                for (int i = 0; i < texts.Count; i++)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    result.Add(new TranslationResult(texts[i], translatedTexts[i]));
                }

                // 最終結果をテキストに保存する
                buf = string.Join("\n\n", result);
                CommonMethodLight.OutputUtf8("resultTexts.txt", buf);

                SetResultToTextBox(result);

                SetStatus("完了しました。");
                // 自動スナップ処理を開始
                this.snapTimer.Start();


                return bmp;
            }
            catch (OperationCanceledException)
            {
                SetStatus("キャンセルされました。");
                // ボタンを再表示する
                ReadyButton.Visible =true;

            }
            catch (Exception ex)
            {
                var msg = ex.Message + "\n" + ex.StackTrace;
                MessageBox.Show(msg);
                Logger.Error(ex, "翻訳処理中にエラーが発生しました。");
                SetStatus("エラーが発生しました。");
                // ボタンを再表示する
                ReadyButton.Visible = true;

            }
            finally
            {
                // フォームのサイズと位置を保存する
                SaveLocation();

                // 一時ファイルを履歴フォルダに移動する
                this.processor.Configs.HistoryFolderNames.Add(MoveToHistory());

                // 設定保存
                this.processor.SaveConfig();
                this.UseWaitCursor = false;
                cts = null;

                // 実行中フラグを消す
                this.IsProcessing = false;
            }

            return null;
        }


        /// <summary>
        /// 翻訳時に出力した画像とテキストを履歴フォルダに移動する
        /// </summary>
        /// <returns>移動先フォルダ</returns>
        private string MoveToHistory()
        {
            var dir = string.Empty;
            var tempDir = Path.GetTempPath();
            dir = Path.Combine(tempDir, "TranslationLens");
            var date = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            dir = Path.Combine(dir, date);

            var fileList = new List<string>()
            {
                "screenshot.png",
                "sendTexts.txt",
                "translated.txt",
                "resultTexts.txt",
            };

            foreach (var file in fileList)
            {
                var src = Path.Combine(Directory.GetCurrentDirectory(), file);
                if (File.Exists(src))
                {
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    var dest = Path.Combine(dir, file);
                    File.Move(src, dest);
                }
            }

            Logger.Info("履歴フォルダ: " + dir);
            return dir;


        }
    }
}
