using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TranslationLens
{
    /// <summary>
    /// 共通クラス（軽量版）
    /// </summary>
    internal static class CommonMethodLight
    {
        /// <summary>
        /// UTF8形式でテキストファイルを保存する
        /// </summary>
        /// <param name="path">保存先</param>
        /// <param name="value">書き込む値</param>
        internal static void OutputUtf8(string path, string value)
        {
            // フルパスに変換
            path = GetFullPath(path);

            File.WriteAllText(
              path,        // 書き込み先のファイル。
              value,    // ファイルに書き込む文字列。
              System.Text.Encoding.UTF8);   // 出力ファイルのエンコーディング。
        }

        /// <summary>
        /// UTF8形式でテキストファイルを読み込む
        /// </summary>
        /// <param name="path">読み込み元</param>
        /// <returns>読んだ内容</returns>
        internal static string InputUtf8(string path)
        {
            // フルパスに変換
            path = GetFullPath(path);

            if (!File.Exists(path))
            {
                return string.Empty;
            }

            using (var sr = new System.IO.StreamReader(path))
            {
                return sr.ReadToEnd();
            }
        }
        /// <summary>
        /// フルパスを取得
        /// </summary>
        /// <param name="fileName">ファイル名</param>
        /// <returns>ファイル名（フルパス）</returns>
        internal static string GetFullPath(string fileName)
        {
            // EXEファイルが存在するフォルダを取得する
            var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            if (Path.GetDirectoryName(fileName) == path)
            {
            // フルパスかどうかの判定
                return fileName;
            }

            return Path.Combine(path, fileName);
        }
    }
}
