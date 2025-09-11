using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranslationLens.Models
{
    /// <summary>
    /// 翻訳結果を格納するクラス。
    /// </summary>
    internal class TranslationResult
    {

        private string sourceText = string.Empty;
        private string resultText = string.Empty;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="sourceText">翻訳元</param>
        /// <param name="resultText">翻訳結果</param>
        internal TranslationResult(string sourceText, string resultText)
        {
            this.sourceText = sourceText;
            this.resultText = resultText;
        }

        /// <summary>
        /// ToString override
        /// </summary>
        /// <returns>文字列</returns>
        public override string ToString()
        {
            return $"{sourceText}\n{resultText}\n";
        }

    }
}
