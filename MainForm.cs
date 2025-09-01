using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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

        Processor processor = new Processor();

        public MainForm()
        {
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
            bmp.Save("screenshot.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
        }
    }
}
