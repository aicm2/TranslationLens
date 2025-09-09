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
        internal const string ConfigFileName = "TranslationLensConfig.json";

        /// <summary>
        /// フォームのロケーションとサイズ
        /// </summary>
        public int FormTop { get; set; } = 0;

        public int FormLeft { get; set; } = 0;

        public int FormWidth { get; set; }=0;

        public int FormHeight { get; set; } = 0;

    }
}
