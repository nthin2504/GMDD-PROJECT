using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieRunner
{
    /// <summary>
    /// Owns the canvas, the HUD, every screen, and routes pointer input
    /// (buttons, swipes, tap-to-fire) without depending on an EventSystem.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public Canvas Canvas;
        public HudView Hud;
        public ScreenViews Screens;

        Image m_vignette;
        float m_vignetteAlpha;
        Color m_vignetteColor = new Color(0.88f, 0.24f, 0.16f);

        ZRButton m_captured;
        Vector2 m_swipeStart;
        bool m_swipeTracking;

        const float SwipeThreshold = 46f;

        public void Build()
        {
            Canvas = UIFactory.CreateCanvas("ZombieRunnerCanvas", 100);
            Canvas.transform.SetParent(transform, false);

            // damage / heal vignette sits under the HUD
            m_vignette = UIFactory.Panel(Canvas.transform, "Vignette", new Color(1f, 1f, 1f, 0f));
            m_vignette.rectTransform.anchorMin = Vector2.zero;
            m_vignette.rectTransform.anchorMax = Vector2.one;
            m_vignette.rectTransform.offsetMin = Vector2.zero;
            m_vignette.rectTransform.offsetMax = Vector2.zero;

            Hud = gameObject.AddComponent<HudView>();
            Hud.Build(Canvas.transform);

            Screens = gameObject.AddComponent<ScreenViews>();
            Screens.Build(Canvas.transform);

            m_vignette.transform.SetAsFirstSibling();
        }

        // ------------------------------------------------------------- screens
        public void ShowScreen(ScreenId id)
        {
            if (Screens != null) Screens.Show(id);
        }

        public void ShowHud(bool visible)
        {
            if (Hud != null) Hud.SetVisible(visible);
        }

        // ------------------------------------------------------------ effects
        public void FlashDamage()
        {
            m_vignetteColor = new Color(0.88f, 0.24f, 0.16f);
            m_vignetteAlpha = 0.5f;
        }

        public void FlashHeal()
        {
            m_vignetteColor = new Color(0.31f, 0.86f, 0.71f);
            m_vignetteAlpha = 0.45f;
        }

        public void FlashAmmo()
        {
            if (Hud != null) Hud.Refresh(GameManager.Instance, true);
        }

        // -------------------------------------------------------------- input
        void Update()
        {
            Vector2 pointer;
            bool down, held, up;
            InputBridge.GetPointer(out pointer, out down, out held, out up);

            // hover highlight (only when the pointer is idle)
            if (!down && !up)
            {
                ZRButton hovered = Topmost(pointer);
                List<ZRButton> all = ZRButton.All;
                for (int i = all.Count - 1; i >= 0; i--)
                {
                    if (i >= all.Count) continue;          // list changed under us
                    ZRButton button = all[i];
                    if (button != null) button.SetHover(button == hovered);
                }
            }

            if (down)
            {
                ZRButton hit = Topmost(pointer);
                if (hit != null && hit.Interactable)
                {
                    m_captured = hit;
                    hit.Press();
                    m_swipeTracking = false;
                }
                else
                {
                    m_captured = null;
                    m_swipeStart = pointer;
                    m_swipeTracking = true;
                }
            }

            if (up)
            {
                if (m_captured != null)
                {
                    bool inside = m_captured.Contains(pointer);
                    m_captured.Release();
                    ZRButton clicked = m_captured;
                    m_captured = null;
                    if (inside) clicked.Click();
                }
                else if (m_swipeTracking)
                {
                    HandleSwipe(pointer - m_swipeStart);
                }
                m_swipeTracking = false;
            }

            if (!held && m_captured != null)
            {
                m_captured.Release();
                m_captured = null;
            }

            // fade the damage vignette
            if (m_vignetteAlpha > 0f)
            {
                m_vignetteAlpha = Mathf.Max(0f, m_vignetteAlpha - Time.unscaledDeltaTime * 1.6f);
                m_vignette.color = new Color(m_vignetteColor.r, m_vignetteColor.g, m_vignetteColor.b, m_vignetteAlpha);
            }
            else if (m_vignette != null && m_vignette.color.a != 0f)
            {
                m_vignette.color = new Color(m_vignetteColor.r, m_vignetteColor.g, m_vignetteColor.b, 0f);
            }
        }

        static ZRButton Topmost(Vector2 pointer)
        {
            List<ZRButton> all = ZRButton.All;
            for (int i = all.Count - 1; i >= 0; i--)
            {
                ZRButton button = all[i];
                if (button == null || !button.isActiveAndEnabled) continue;
                if (button.Contains(pointer)) return button;
            }
            return null;
        }

        void HandleSwipe(Vector2 delta)
        {
            GameManager game = GameManager.Instance;
            if (game == null || game.State != GameState.Playing) return;

            float ax = Mathf.Abs(delta.x);
            float ay = Mathf.Abs(delta.y);
            if (Mathf.Max(ax, ay) < SwipeThreshold)
            {
                game.Weapon.TryFire();   // tap = fire
                return;
            }

            if (ax > ay) game.Player.MoveLane(delta.x > 0f ? 1 : -1);
            else if (delta.y > 0f) game.Player.Jump();
            else game.Player.Slide();
        }
    }
}
