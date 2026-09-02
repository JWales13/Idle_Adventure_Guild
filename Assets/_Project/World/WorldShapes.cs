using UnityEngine;

namespace IdleGuild.World
{
    /// <summary>
    /// Makes the coloured rectangles the grey box is built out of.
    ///
    /// One 1x1 white sprite, generated in code and tinted per renderer, is the whole
    /// mechanism. It means the grey box needs no art file, no import settings and no
    /// .meta -- which matters more than it sounds, because Day 15 lost an afternoon to
    /// this project importing textures as Sprite Mode MULTIPLE by default and auto-slicing
    /// an icon into pieces. A sprite that never touches the importer cannot be sliced.
    ///
    /// The sprite is one world unit across at one pixel per unit, so a renderer's local
    /// scale IS its size in world units and no conversion constant appears anywhere.
    /// </summary>
    internal static class WorldShapes
    {
        private static Sprite _unitSprite;

        /// <summary>
        /// A white 1x1 sprite, one world unit square. Tint it with
        /// <see cref="SpriteRenderer.color"/> and scale it to size.
        /// </summary>
        internal static Sprite UnitSprite
        {
            get
            {
                // Deliberately not a null-coalescing assignment. Unity overloads ==
                // against destroyed objects, and a domain reload with Enter Play Mode
                // Options on leaves this field holding a destroyed texture that is not
                // null to C# but is null to Unity. The explicit comparison uses Unity's
                // overload; ??= would not, and would hand back a dead sprite.
                if (_unitSprite == null)
                {
                    _unitSprite = BuildUnitSprite();
                }

                return _unitSprite;
            }
        }

        /// <summary>
        /// Adds a coloured rectangle to <paramref name="parent"/>, sized and positioned in
        /// world units.
        /// </summary>
        internal static SpriteRenderer AddRect(
            Transform parent, string name, Rect rect, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(rect.center.x, rect.center.y, 0f);
            go.transform.localScale = new Vector3(rect.width, rect.height, 1f);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = UnitSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            return renderer;
        }

        private static Sprite BuildUnitSprite()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false)
            {
                name = "WorldUnitPixel",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,

                // Generated, not authored: it must not be saved into the scene, and it
                // must survive a scene change so the next Rebuild does not find it gone.
                hideFlags = HideFlags.HideAndDontSave,
            };

            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 1f);

            sprite.name = "WorldUnitSprite";
            sprite.hideFlags = HideFlags.HideAndDontSave;

            return sprite;
        }
    }
}
