using UnityEngine;
using UnityEngine.UI;

namespace TienLo
{
    internal sealed class TrainingView
    {
        public static readonly Color Gold = Hex("#F5D18F");
        public static readonly Color Ink = Hex("#0C1524");
        public static readonly Color Muted = Hex("#AEC1D4");
        private readonly Font regular = Resources.Load<Font>("TienLo/OpenSans-Regular");
        private readonly Font bold = Resources.Load<Font>("TienLo/OpenSans-Bold");
        private readonly Sprite rounded;

        public TrainingView()
        {
            const int size = 32;
            const int radius = 10;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Rounded UI panel",
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var corner = new Vector2(Mathf.Clamp(x, radius, size - radius - 1),
                    Mathf.Clamp(y, radius, size - radius - 1));
                float alpha = Mathf.Clamp01(radius + 0.5f - Vector2.Distance(new Vector2(x, y), corner));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            rounded = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        public static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString(value, out Color color);
            return color;
        }

        public static RectTransform Node(Transform parent, string name, float x, float y, float w, float h)
        {
            var node = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            node.SetParent(parent, false);
            node.anchorMin = node.anchorMax = node.pivot = new Vector2(0, 1);
            SetRect(node, x, y, w, h);
            return node;
        }

        public static void SetRect(RectTransform node, float x, float y, float w, float h)
        {
            node.anchoredPosition = new Vector2(x, -y);
            node.sizeDelta = new Vector2(w, h);
        }

        public Image Panel(Transform parent, string name, float x, float y, float w, float h, Color color,
            bool round = true)
        {
            var image = Node(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (round)
            {
                image.sprite = rounded;
                image.type = Image.Type.Sliced;
            }
            return image;
        }

        public Text Label(Transform parent, string value, float x, float y, float w, float h,
            int size, Color color, bool strong = false, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var label = Node(parent, "Text", x, y, w, h).gameObject.AddComponent<Text>();
            label.text = value;
            label.font = strong ? bold : regular;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        public RawImage Picture(Transform parent, string name, Texture2D texture, float x, float y, float w, float h)
        {
            var picture = Node(parent, name, x, y, w, h).gameObject.AddComponent<RawImage>();
            picture.texture = texture;
            picture.raycastTarget = false;
            return picture;
        }

        public static Button Clickable(Image surface)
        {
            surface.raycastTarget = true;
            var button = surface.gameObject.AddComponent<Button>();
            button.targetGraphic = surface;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1, 1, 1, 0.85f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f);
            button.colors = colors;
            return button;
        }
    }
}
