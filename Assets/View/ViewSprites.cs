using UnityEngine;

namespace Meandermites.View
{
    /// <summary>
    /// Placeholder art generated in code, so the prototype needs no imported
    /// assets. Every accessor re-creates its asset if Unity has destroyed it,
    /// because Enter Play Mode options in this project skip the domain reload
    /// and static fields survive between play sessions.
    /// </summary>
    public static class ViewSprites
    {
        private const int Size = 128;

        private static Sprite _disc;
        private static Sprite _ring;
        private static Sprite _square;
        private static Material _material;

        /// <summary>Filled circle, 1 unit in diameter.</summary>
        public static Sprite Disc
        {
            get
            {
                if (_disc == null) _disc = MakeCircle(0f);
                return _disc;
            }
        }

        /// <summary>Circle outline, 1 unit in diameter.</summary>
        public static Sprite Ring
        {
            get
            {
                if (_ring == null) _ring = MakeCircle(0.12f);
                return _ring;
            }
        }

        /// <summary>White square, 1 unit across.</summary>
        public static Sprite Square
        {
            get
            {
                if (_square == null)
                {
                    var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                    var pixels = new Color32[16];
                    for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
                    tex.SetPixels32(pixels);
                    tex.Apply();
                    tex.filterMode = FilterMode.Point;
                    _square = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                }
                return _square;
            }
        }

        /// <summary>
        /// Unlit sprite material. "Sprites/Default" ignores 2D lights, so no
        /// Light2D is needed, and it is always included in builds.
        /// </summary>
        public static Material SpriteMaterial
        {
            get
            {
                if (_material == null) _material = new Material(Shader.Find("Sprites/Default"));
                return _material;
            }
        }

        // thickness 0 = filled disc; otherwise a ring whose width is that
        // fraction of the diameter.
        private static Sprite MakeCircle(float thickness)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var pixels = new Color32[Size * Size];
            float outer = Size * 0.5f - 1f;
            float inner = thickness > 0f ? outer - thickness * Size : -1f;
            float c = (Size - 1) * 0.5f;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dx = x - c;
                    float dy = y - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(outer - d + 0.5f);
                    if (inner > 0f) a *= Mathf.Clamp01(d - inner + 0.5f);
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        }
    }
}
