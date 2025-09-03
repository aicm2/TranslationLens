using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TranslationLens
{
    internal static class Program
    {
        // ロガーのインスタンスを作成
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {

            var processor = new Processor();

            try
            {
            // OAuth認証を同期的に実行
            OAuth(processor).GetAwaiter().GetResult();

            }
            catch (Exception ex)
            {
                MessageBox.Show("OAuth認証に失敗しました: " + ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logger.Error(ex, "OAuth認証に失敗しました");
                return; // アプリケーションを終了
            }

            Logger.Info("OAuth認証に成功しました");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(processor));
        }

        /// <summary>
        /// Google OAuth認証処理
        /// </summary>
        /// <param name="processor">プロセッサ</param>
        /// <returns>Task</returns>
        private static async Task OAuth(Processor processor)
        {
          await processor.OAuthByGoogle(); 

        }
    }
}
