using Meandermites.Sim;
using UnityEngine;

namespace Meandermites.View
{
    /// <summary>The look of a Spawn Portal. View only: the Sim does not know about it.</summary>
    public enum PortalStyle
    {
        Ring,   // pulsing concentric rings
        Swirl,  // small lights orbiting the centre
        Rift,   // a tall slit that opens
        Bloom   // petals that unfold
    }

    /// <summary>
    /// One Spawn Portal drawn with placeholder shapes. It fades in while the
    /// Sim counts down to the release, holds while the Mites come out one by
    /// one, then pops and fades away. Everything here is a function of the step count.
    /// </summary>
    public sealed class PortalArt
    {
        public const int StyleCount = 4;
        private const int Parts = 9;
        private const int LingerSteps = 25; // 0.5 s fade-out after the release

        private readonly SpriteRenderer[] _parts = new SpriteRenderer[Parts];
        private readonly Transform _holder;
        private readonly PortalStyle _style;

        public static string StyleName(PortalStyle style) => style.ToString();

        public PortalArt(Transform parent, PortalStyle style, SimVec2 position, int index)
        {
            _style = style;
            _holder = new GameObject("Portal" + index + "_" + style).transform;
            _holder.SetParent(parent, false);
            _holder.position = new Vector3(position.X, position.Y, 0f);

            for (int i = 0; i < Parts; i++)
            {
                var go = new GameObject("part" + i);
                go.transform.SetParent(_holder, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteFor(style, i);
                sr.sharedMaterial = ViewSprites.SpriteMaterial;
                sr.sortingOrder = i == 0 ? 0 : 1;
                sr.enabled = false;
                _parts[i] = sr;
            }
        }

        private static Sprite SpriteFor(PortalStyle style, int i)
        {
            switch (style)
            {
                case PortalStyle.Ring: return i <= 2 ? ViewSprites.Ring : ViewSprites.Disc;
                case PortalStyle.Rift: return (i == 1 || i == 2) ? ViewSprites.Square : ViewSprites.Disc;
                default: return ViewSprites.Disc;
            }
        }

        private static Color Tint(PortalStyle style)
        {
            switch (style)
            {
                case PortalStyle.Ring: return new Color(0.7f, 0.4f, 1f);
                case PortalStyle.Swirl: return new Color(0.35f, 0.9f, 0.8f);
                case PortalStyle.Rift: return new Color(1f, 0.55f, 0.85f);
                default: return new Color(1f, 0.85f, 0.4f);
            }
        }

        public void Refresh(PortalSim portal, long step)
        {
            // Before the release it fades in; after it, it pops outward and lingers out.
            float alpha;
            float size;
            if (!portal.Released)
            {
                float f = portal.FadeProgress(step);
                alpha = f;
                size = f * f * (3f - 2f * f); // smoothstep
            }
            else
            {
                float k = 1f - Mathf.Clamp01((float)(step - portal.LastReleaseStep) / LingerSteps);
                alpha = k;
                size = 1f + 0.5f * (1f - k);
            }

            foreach (var p in _parts) p.enabled = false;
            if (alpha <= 0.001f) return;

            Color c = Tint(_style);
            float t = step;
            switch (_style)
            {
                case PortalStyle.Ring:
                    for (int i = 0; i < 3; i++)
                    {
                        float d = (0.7f + i * 0.55f) * size * (1f + 0.08f * Mathf.Sin(t * 0.2f + i));
                        Show(i, Vector2.zero, d, d, c, alpha * (1f - i * 0.25f));
                    }
                    Show(3, Vector2.zero, 0.6f * size, 0.6f * size, c, alpha * 0.5f);
                    break;

                case PortalStyle.Swirl:
                    for (int i = 0; i < 6; i++)
                    {
                        float a = t * 0.12f + i * (Mathf.PI * 2f / 6f);
                        var off = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.55f * size;
                        Show(i, off, 0.22f * size, 0.22f * size, c, alpha);
                    }
                    Show(6, Vector2.zero, 0.5f * size, 0.5f * size, c, alpha * 0.35f);
                    break;

                case PortalStyle.Rift:
                    Show(0, Vector2.zero, 1.5f * size, 1.5f * size, c, alpha * 0.25f);
                    float wobble = 0.85f + 0.15f * Mathf.Sin(t * 0.3f);
                    Show(1, Vector2.zero, 0.34f * size * wobble, 1.9f * size, c, alpha * 0.9f);
                    Show(2, Vector2.zero, 0.12f * size, 1.7f * size, Color.white, alpha);
                    break;

                default: // Bloom
                    for (int i = 0; i < 8; i++)
                    {
                        float a = t * 0.03f + i * (Mathf.PI * 2f / 8f);
                        var off = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.6f * size;
                        Show(i, off, 0.36f * size, 0.36f * size, c, alpha * 0.9f);
                    }
                    Show(8, Vector2.zero, 0.4f * size, 0.4f * size, Color.white, alpha * 0.8f);
                    break;
            }
        }

        private void Show(int i, Vector2 offset, float width, float height, Color c, float alpha)
        {
            SpriteRenderer sr = _parts[i];
            sr.enabled = true;
            sr.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
            sr.transform.localScale = new Vector3(width, height, 1f);
            sr.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(alpha));
        }
    }
}
