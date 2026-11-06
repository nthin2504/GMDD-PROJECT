using UnityEngine;
using UnityEngine.UI;

namespace ZombieRunner
{
    /// <summary>
    /// Builds uGUI from code: canvas, panels, labels, bars and buttons.
    /// The port ships no prefabs or sprites — everything is generated at boot so
    /// the project imports cleanly into any Unity version.
    /// </summary>
    public static class UIFactory
    {
        // ------------------------------------------------------------- palette
        public static readonly Color Ink = new Color(0.87f, 0.91f, 0.94f, 1f);
        public static readonly Color Dim = new Color(0.56f, 0.64f, 0.69f, 1f);
        public static readonly Color Dimmer = new Color(0.37f, 0.45f, 0.51f, 1f);
        public static readonly Color Orange = new Color(0.94f, 0.47f, 0.24f, 1f);
        public static readonly Color OrangeDark = new Color(0.76f, 0.35f, 0.16f, 1f);
        public static readonly Color Green = new Color(0.56f, 0.75f, 0.37f, 1f);
        public static readonly Color Teal = new Color(0.29f, 0.64f, 0.64f, 1f);
        public static readonly Color Red = new Color(0.88f, 0.30f, 0.23f, 1f);
        public static readonly Color PanelBg = new Color(0.067f, 0.106f, 0.141f, 0.92f);
        public static readonly Color PanelBgSoft = new Color(0.086f, 0.137f, 0.18f, 0.9f);
        public static readonly Color Line = new Color(0.149f, 0.208f, 0.247f, 1f);

        static Font s_font;

        public static Font UIFont
        {
            get
            {
                if (s_font != null) return s_font;
#if UNITY_2022_2_OR_NEWER
                s_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
                s_font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
                if (s_font == null)
                {
                    s_font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Segoe UI", "Arial", "Helvetica", "DejaVu Sans", "Liberation Sans" }, 24);
                }
                return s_font;
            }
        }

        // -------------------------------------------------------------- canvas
        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            GameObject go = new GameObject(name);
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>Rect helper: anchor a child precisely (offset is from the anchor).</summary>
        public static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
                                 Vector2 anchoredPosition, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
        }

        // ------------------------------------------------------------- widgets
        public static Image Panel(Transform parent, string name, Color color)
        {
            RectTransform rt = Rect(parent, name);
            Image image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Image SpriteImage(Transform parent, string name, Sprite sprite, Color tint)
        {
            RectTransform rt = Rect(parent, name);
            Image image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = tint;
            image.raycastTarget = false;
            return image;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor anchor,
                                 FontStyle style = FontStyle.Normal)
        {
            RectTransform rt = Rect(parent, "Label");
            Text label = rt.gameObject.AddComponent<Text>();
            label.font = UIFont;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = anchor;
            label.fontStyle = style;
            label.supportRichText = true;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color, TextAnchor anchor,
                                 FontStyle style = FontStyle.Normal)
        {
            Text label = Label(parent, text, size, color, anchor, style);
            label.name = name;
            return label;
        }

        /// <summary>Standard dark, rounded-looking panel (squared edges, subtle border).</summary>
        public static Image Card(Transform parent, string name)
        {
            Image card = Panel(parent, name, PanelBg);
            Image border = Panel(card.transform, "Border", new Color(Line.r, Line.g, Line.b, 0.55f));
            Place(border.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            border.rectTransform.offsetMin = new Vector2(-2f, -2f);
            border.rectTransform.offsetMax = new Vector2(2f, 2f);
            border.transform.SetAsFirstSibling();
            return card;
        }

        // ------------------------------------------------------------- buttons
        public static ZRButton Button(Transform parent, string caption, Vector2 size, Color background,
                                      Color textColor, System.Action onClick)
        {
            RectTransform rt = Rect(parent, "Button_" + caption);
            Image bg = rt.gameObject.AddComponent<Image>();
            bg.color = background;
            bg.raycastTarget = false;

            Text label = Label(rt, "Caption", caption, 26, textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            ZRButton button = rt.gameObject.AddComponent<ZRButton>();
            button.Rect = rt;
            button.Background = bg;
            button.Caption = label;
            button.OnClick = onClick;
            button.NormalBg = background;
            button.HoverBg = new Color(
                Mathf.Min(1f, background.r + 0.08f),
                Mathf.Min(1f, background.g + 0.08f),
                Mathf.Min(1f, background.b + 0.08f),
                background.a);
            button.PressBg = new Color(Orange.r, Orange.g, Orange.b, 0.9f);
            button.NormalText = textColor;
            button.HoverText = Color.white;

            rt.sizeDelta = size;
            return button;
        }

        // ----------------------------------------------------------------- fx
        static Sprite s_gradient;

        /// <summary>Vertical gradient sprite used for menu backdrops.</summary>
        public static Sprite GradientSprite
        {
            get
            {
                if (s_gradient != null) return s_gradient;
                const int height = 64;
                Texture2D tex = new Texture2D(4, height, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                Color[] pixels = new Color[4 * height];
                for (int y = 0; y < height; y++)
                {
                    float t = y / (float)(height - 1);
                    // dark at the top and bottom, lighter through the middle band
                    float a = Mathf.Lerp(0.92f, 0.28f, Mathf.Sin(t * Mathf.PI));
                    for (int x = 0; x < 4; x++) pixels[y * 4 + x] = new Color(0.027f, 0.043f, 0.059f, a);
                }
                tex.SetPixels(pixels);
                tex.Apply();
                s_gradient = Sprite.Create(tex, new Rect(0, 0, 4, height), new Vector2(0.5f, 0.5f));
                return s_gradient;
            }
        }

        public static Image Bar(Transform parent, string name, Color fillColor, out Image fill)
        {
            Image track = Panel(parent, name, new Color(0.11f, 0.16f, 0.2f, 1f));
            Image inner = Panel(track.transform, "Fill", fillColor);
            Place(inner.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, Vector2.zero);
            inner.rectTransform.offsetMin = new Vector2(1f, 1f);
            inner.rectTransform.offsetMax = new Vector2(-1f, -1f);
            fill = inner;
            return track;
        }
    }
}
