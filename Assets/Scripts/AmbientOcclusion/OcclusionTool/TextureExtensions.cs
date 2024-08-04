using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AmbientOcclusion.OcclusionTool {
    
    public static class TextureExtensions {
        public static void SetPoint(this Texture2D texture, Vector2Int position, Color c) {
            texture.SetPixel(position.x, position.y, c);
        }
        public static void SetPoint(this Texture2D texture, int x, int y, Color c) {
            texture.SetPixel(x, y, c);
        }
        // Save the image to the specified path
        public static void SaveImage(this Texture2D texture, string savePath, string fileName) {
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(savePath + fileName + ".png", bytes);
            Debug.Log("Image saved at: " + savePath + "generated_image.png");
        }
    }
}