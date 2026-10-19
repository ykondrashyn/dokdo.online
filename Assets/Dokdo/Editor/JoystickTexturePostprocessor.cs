using System;
using UnityEditor;
using UnityEngine;

namespace Dokdo.Editor
{
    public sealed class JoystickTexturePostprocessor : AssetPostprocessor
    {
        public const string TexturePath =
            "Assets/Texture2D/UI_ring003.png";
        public const string WebGlPlatform = "WebGL";
        public const int SourceSize = 256;

        public override uint GetVersion()
        {
            return 1;
        }

        private void OnPreprocessTexture()
        {
            if (!string.Equals(assetPath, TexturePath,
                    StringComparison.Ordinal))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;

            var webGl = importer.GetPlatformTextureSettings(WebGlPlatform);
            webGl.name = WebGlPlatform;
            webGl.overridden = true;
            webGl.maxTextureSize = SourceSize;
            webGl.format = TextureImporterFormat.RGBA32;
            webGl.textureCompression = TextureImporterCompression.Uncompressed;
            webGl.crunchedCompression = false;
            importer.SetPlatformTextureSettings(webGl);
        }
    }
}
