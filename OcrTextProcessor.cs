using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace TranslationLens
{
    internal static class TextSplitter
    {
        /// <summary>
        /// OCRで取得したテキストを文単位に分割する
        /// </summary>
        /// <param name="text">OCR結果文字列</param>
        /// <returns>文ごとのリスト</returns>
        public static List<string> SplitSentences(string text)
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

