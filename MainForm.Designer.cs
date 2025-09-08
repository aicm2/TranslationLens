namespace TranslationLens
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.MenuStrip1 = new System.Windows.Forms.MenuStrip();
            this.MainMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.ScreenShotMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuTransrate = new System.Windows.Forms.ToolStripMenuItem();
            this.MemuTranslationText = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.TextsTextBox = new System.Windows.Forms.RichTextBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.MenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // MenuStrip1
            // 
            this.MenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MainMenu});
            this.MenuStrip1.Location = new System.Drawing.Point(0, 0);
            this.MenuStrip1.Name = "MenuStrip1";
            this.MenuStrip1.Size = new System.Drawing.Size(800, 27);
            this.MenuStrip1.TabIndex = 0;
            this.MenuStrip1.Text = "メニュー";
            // 
            // MainMenu
            // 
            this.MainMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ScreenShotMenu,
            this.MenuTransrate,
            this.MemuTranslationText});
            this.MainMenu.Name = "MainMenu";
            this.MainMenu.Size = new System.Drawing.Size(59, 23);
            this.MainMenu.Text = "メニュー";
            // 
            // ScreenShotMenu
            // 
            this.ScreenShotMenu.Name = "ScreenShotMenu";
            this.ScreenShotMenu.Size = new System.Drawing.Size(223, 24);
            this.ScreenShotMenu.Text = "撮影（テスト用）";
            this.ScreenShotMenu.Click += new System.EventHandler(this.ScreenShotMenu_Click);
            // 
            // MenuTransrate
            // 
            this.MenuTransrate.Name = "MenuTransrate";
            this.MenuTransrate.Size = new System.Drawing.Size(223, 24);
            this.MenuTransrate.Text = "翻訳（テスト用）";
            this.MenuTransrate.Click += new System.EventHandler(this.MenuTransLate_Click_Async);
            // 
            // MemuTranslationText
            // 
            this.MemuTranslationText.Name = "MemuTranslationText";
            this.MemuTranslationText.Size = new System.Drawing.Size(223, 24);
            this.MemuTranslationText.Text = "テキスト翻訳（テスト用）";
            this.MemuTranslationText.Click += new System.EventHandler(this.MemuTranslationText_Click_Async);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // TextsTextBox
            // 
            this.TextsTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TextsTextBox.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.TextsTextBox.Location = new System.Drawing.Point(544, 30);
            this.TextsTextBox.Name = "TextsTextBox";
            this.TextsTextBox.Size = new System.Drawing.Size(256, 425);
            this.TextsTextBox.TabIndex = 1;
            this.TextsTextBox.Text = "";
            this.TextsTextBox.DoubleClick += new System.EventHandler(this.TextsTextBox_DoubleClick);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Location = new System.Drawing.Point(0, 428);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(800, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            this.statusStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.statusStrip1_ItemClicked);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.TextsTextBox);
            this.Controls.Add(this.MenuStrip1);
            this.MainMenuStrip = this.MenuStrip1;
            this.Name = "MainForm";
            this.Text = "MainForm";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.MenuStrip1.ResumeLayout(false);
            this.MenuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip MenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem MainMenu;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem ScreenShotMenu;
        private System.Windows.Forms.ToolStripMenuItem MenuTransrate;
        private System.Windows.Forms.RichTextBox TextsTextBox;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripMenuItem MemuTranslationText;
    }
}
