using UnityEditor;
using UnityEngine;

namespace VN.EditorTools
{
    /// <summary>
    /// 배경/캐릭터/CG 폴더에 넣은 이미지를 자동으로 Sprite로 임포트합니다.
    /// (처음 임포트될 때만 적용되므로, 이후 Inspector에서 바꾼 설정은 유지됩니다)
    /// </summary>
    public class VNTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetImporter.importSettingsMissing) return;
            string p = assetPath.Replace('\\', '/');
            var ti = (TextureImporter)assetImporter;

            if (p.StartsWith("Assets/Resources/Backgrounds/") ||
                p.StartsWith("Assets/Resources/Characters/") ||
                p.StartsWith("Assets/Resources/CG/"))
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.maxTextureSize = 4096;
                ti.textureCompression = TextureImporterCompression.CompressedHQ;
            }
            else if (p.StartsWith("Assets/VN/UI/"))
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.spriteBorder = new Vector4(24, 24, 24, 24); // 9-슬라이스
            }
        }
    }
}
