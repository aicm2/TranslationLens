using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoenM.ImageHash;
using Image = SixLabors.ImageSharp.Image;

namespace TranslationLens
{
    /// <summary>
    /// 処理クラスの画像処理部分
    /// </summary>
    internal partial class Processor
    {
        /// <summary>
        /// 類似度を求める
        /// </summary>
        /// <param name="basePicture">元画像</param>
        /// <param name="newPicture">先画像</param>
        /// <returns>true;異なる画像とみなす</returns>
        public bool IsDifferentImages(Bitmap basePicture, Bitmap newPicture)
        {
            var diff = GetThreshold(basePicture, newPicture);

            var ret = !(Configs.DifferentThreshold <= diff);

            if (ret)
            {
                Logger.Info($"差分発生。差違値={diff}%");
            }
            return ret; 
        }

        /// <summary>
        /// 類似度を求める
        /// </summary>
        /// <param name="basePicture">元画像</param>
        /// <param name="newPicture">先画像</param>
        /// <returns>類似度</returns>
        public float GetThreshold(Bitmap basePicture, Bitmap newPicture)
        {
            // 画像の読み込み
            MemoryStream ms = new MemoryStream();
            basePicture.Save(ms, ImageFormat.Bmp);

            Image<Rgba32> image1 = Image.Load<Rgba32>(ms.GetBuffer());

            newPicture.Save(ms, ImageFormat.Bmp);
            Image<Rgba32> image2 = Image.Load<Rgba32>(ms.GetBuffer());

            // ハッシュアルゴリズムのインスタンス化(この例ではdHash)
            CoenM.ImageHash.IImageHash dHashArgorithm = new CoenM.ImageHash.HashAlgorithms.DifferenceHash();

            // ハッシュを求める
            ulong dHash1 = dHashArgorithm.Hash(image1);
            ulong dHash2 = dHashArgorithm.Hash(image2);

            // ハッシュ値間の類似度を求める
            double dHashSimilarity = CoenM.ImageHash.CompareHash.Similarity(dHash1, dHash2);

            return (float)dHashSimilarity;
        }
    }
}
