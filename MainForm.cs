using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TranslationLens
{
    public partial class MainForm : Form
    {
        private Panel panel;
        // Formの端をドラッグしてサイズ変更するためのクラス(効かない)
        private FormDragResizer formResizer;

        private Processor processor = null;

        internal MainForm(Processor processor)
        {
            this.processor = processor;
            InitializeComponent();

            // Formのイニシャル処理で生成する
            formResizer = new FormDragResizer(this, FormDragResizer.ResizeDirection.All, 8);

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
        }

        private void AdjustPanelBounds()
        {
            int margin = 8; // 枠を残す幅
            panel.Bounds = new Rectangle(
                margin,
                margin,
                this.ClientSize.Width - margin * 2,
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
            bmp.Save("screenshot.png", System.Drawing.Imaging.ImageFormat.Png);
        }

        /// <summary>
        /// 翻訳（テスト）
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">p</param>
        private void MenuTransLate_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;

             var myString = CallOcr().GetAwaiter().GetResult();

            Console.WriteLine($"result = {myString}");
            MessageBox.Show("OK");
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
                Console.WriteLine($"result = {myString}");
                MessageBox.Show("OK");
            }));
        }

        private async Task<string> CallOcr()
        {
            try
            {
                var imagePath = Path.GetFullPath("screenshot.png");
                var myString = await this.processor.OCRByGoogle(imagePath);

               // var myString = await this.processor.OCRByGoogleTest(imagePath);

                Console.WriteLine($"result = {myString}");
                return myString;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
            return null;
        }

    }
}
