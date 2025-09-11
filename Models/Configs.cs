using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranslationLens.Models
{
    /// <summary>
    /// 設定クラス
    /// </summary>
    public class Configs
    {
        /// <summary>
        /// コンフィグのパス
        /// </summary>
        internal const string ConfigPath = "TranslationLensConfig.json";

        /// <summary>
        /// フォームのロケーションとサイズ
        /// </summary>
        public int FormTop { get; set; } = 0;

        public int FormLeft { get; set; } = 0;

        public int FormWidth { get; set; }=0;

        public int FormHeight { get; set; } = 0;

        /// <summary>
        /// 翻訳の際に履歴を保存したフォルダ名のリスト
        /// </summary>
        public List<string> HistoryFolderNames { get; set; } = new List<string>();

        /// <summary>
        /// 翻訳元言語コード (デフォルト: "en" - 英語)
        /// </summary>
        public string SourceLang { get; set; } = "en";

        /// <summary>
        /// 翻訳先,言語コード (デフォルト: "ja" - 日本語)
        /// </summary>
        public string TargetLang { get; set; } = "ja";
    }
}
