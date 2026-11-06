using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieRunner
{
    /// <summary>
    /// Self-hitting button. The game ships without an EventSystem/InputModule on
    /// purpose: this component tests the pointer against its own rect, so UI
    /// input works identically in every input backend and every render pipeline.
    /// </summary>
    public class ZRButton : MonoBehaviour
    {
        public static readonly List<ZRButton> All = new List<ZRButton>();

        public RectTransform Rect;
        public Image Background;
        public Text Caption;
        public bool Interactable = true;
        [System.NonSerialized] public System.Action OnClick;

        public Color NormalBg = new Color(1f, 1f, 1f, 0.06f);
        public Color HoverBg = new Color(1f, 1f, 1f, 0.14f);
        public Color PressBg = new Color(0.94f, 0.47f, 0.24f, 0.85f);
        public Color DisabledBg = new Color(1f, 1f, 1f, 0.03f);
        public Color NormalText = new Color(0.87f, 0.91f, 0.94f, 1f);
        public Color HoverText = Color.white;
        public Color DisabledText = new Color(0.45f, 0.53f, 0.6f, 1f);

        bool m_pressed;
        bool m_hovered;

        public bool IsPressed { get { return m_pressed; } }
        public bool IsHovered { get { return m_hovered; } }

        void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
            Apply();
        }

        void OnDisable()
        {
            All.Remove(this);
            m_pressed = false;
            m_hovered = false;
        }

        public bool Contains(Vector2 screenPosition)
        {
            if (Rect == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(Rect, screenPosition, null);
        }

        public void SetHover(bool hovered)
        {
            if (m_hovered == hovered) return;
            m_hovered = hovered;
            Apply();
        }

        public void Press()
        {
            if (!Interactable) return;
            m_pressed = true;
            Apply();
        }

        public void Release()
        {
            m_pressed = false;
            Apply();
        }

        public void Click()
        {
            if (!Interactable) return;
            if (OnClick != null) OnClick();
        }

        public void Apply()
        {
            if (Background != null)
            {
                Background.color = !Interactable ? DisabledBg : (m_pressed ? PressBg : (m_hovered ? HoverBg : NormalBg));
            }
            if (Caption != null)
            {
                Caption.color = !Interactable ? DisabledText : (m_hovered || m_pressed ? HoverText : NormalText);
            }
        }

        public void SetText(string text)
        {
            if (Caption != null) Caption.text = text;
        }
    }
}
