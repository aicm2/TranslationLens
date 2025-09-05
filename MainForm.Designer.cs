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
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
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
            this.MenuTransrate});
            this.MainMenu.Name = "MainMenu";
            this.MainMenu.Size = new System.Drawing.Size(59, 23);
            this.MainMenu.Text = "メニュー";
            // 
            // ScreenShotMenu
            // 
            this.ScreenShotMenu.Name = "ScreenShotMenu";
            this.ScreenShotMenu.Size = new System.Drawing.Size(180, 24);
            this.ScreenShotMenu.Text = "撮影（テスト用）";
            this.ScreenShotMenu.Click += new System.EventHandler(this.ScreenShotMenu_Click);
            // 
            // MenuTransrate
            // 
            this.MenuTransrate.Name = "MenuTransrate";
            this.MenuTransrate.Size = new System.Drawing.Size(180, 24);
            this.MenuTransrate.Text = "翻訳（テスト用）";
            this.MenuTransrate.Click += new System.EventHandler(this.MenuTransLate_Click_Async);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
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
    }
}
