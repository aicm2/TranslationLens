using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace TranslationLens
{
    /// <summary>
    /// テキストを文章ごとに分割するユーティリティクラス
    /// </summary>
    internal static class TextSplitter
    {
        /// <summary>
        /// OCRで取得したテキストを文単位に分割する（英語用）
        /// </summary>
        /// <param name="text">OCR結果文字列</param>
        /// <returns>文ごとのリスト</returns>
        public static List<string> SplitSentencesEn(string text)
        {
            var sentences = new List<string>();

            if (string.IsNullOrWhiteSpace(text))
            {
                return sentences;
            }

            // 改行をスペースに統一
            text = Regex.Replace(text, @"\r?\n", " ");

            // 文末記号 (.,!,?) のあとに空白/改行 または テキスト終端
            var parts = Regex.Split(text, @"(?<=[\.!\?])\s+|(?<=[\.!\?])$");

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    sentences.Add(trimmed);
                }
            }

            return sentences;
        }
    }
}

