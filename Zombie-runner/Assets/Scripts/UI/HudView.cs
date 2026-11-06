using UnityEngine;
using UnityEngine.UI;

namespace ZombieRunner
{
    /// <summary>In-run HUD: health, ammo, score, combo and the verb buttons.</summary>
    public class HudView : MonoBehaviour
    {
        public GameObject Root;

        public Image HpFill;
        public Text HpText;
        public Text CoinsText;
        public Text DistanceText;
        public Text ScoreText;
        public Text ComboText;
        public Text AmmoText;
        public Image AmmoBadge;

        public ZRButton JumpButton;
        public ZRButton SlideButton;
        public ZRButton FireButton;
        public ZRButton PauseButton;
        public ZRButton MuteButton;

        int m_cachedHp = -999, m_cachedCoins = -1, m_cachedDistance = -1,
            m_cachedScore = -1, m_cachedAmmo = -1, m_cachedCombo = -1;

        public void Build(Transform canvas)
        {
            Root = new GameObject("HUD", typeof(RectTransform));
            Root.transform.SetParent(canvas, false);
            RectTransform rootRect = (RectTransform)Root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // ------------------------------------------------------ top bar
            Image bar = UIFactory.Panel(Root.transform, "TopBar", new Color(0.043f, 0.071f, 0.094f, 0.78f));
            UIFactory.Place(bar.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -18f), new Vector2(1320f, 76f));

            Text hpLabel = UIFactory.Label(bar.transform, "HpLabel", "HP", 22, UIFactory.Dimmer, TextAnchor.MiddleLeft, FontStyle.Bold);
            Left(hpLabel.rectTransform, 20f, 60f);

            Image hpTrack = UIFactory.Panel(bar.transform, "HpTrack", new Color(0.11f, 0.16f, 0.2f, 1f));
            UIFactory.Place(hpTrack.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(66f, 0f), new Vector2(240f, 18f));

            HpFill = UIFactory.Panel(hpTrack.transform, "HpFill", UIFactory.Green);
            HpFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            HpFill.rectTransform.anchorMax = new Vector2(1f, 1f);
            HpFill.rectTransform.offsetMin = Vector2.zero;
            HpFill.rectTransform.offsetMax = Vector2.zero;

            HpText = UIFactory.Label(bar.transform, "HpText", "100/100", 22, UIFactory.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            Left(HpText.rectTransform, 318f, 150f);

            CoinsText = UIFactory.Label(bar.transform, "Coins", "* 0", 24, UIFactory.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            Left(CoinsText.rectTransform, 480f, 150f);

            DistanceText = UIFactory.Label(bar.transform, "Distance", "0 m", 24, UIFactory.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            Left(DistanceText.rectTransform, 650f, 170f);

            ScoreText = UIFactory.Label(bar.transform, "Score", "0", 24, UIFactory.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            Left(ScoreText.rectTransform, 840f, 170f);

            ComboText = UIFactory.Label(bar.transform, "Combo", "", 26, UIFactory.Orange, TextAnchor.MiddleLeft, FontStyle.Bold);
            Left(ComboText.rectTransform, 1010f, 150f);

            AmmoBadge = UIFactory.Panel(bar.transform, "AmmoBadge", new Color(0.235f, 0.118f, 0.055f, 0.85f));
            UIFactory.Place(AmmoBadge.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-18f, 0f), new Vector2(160f, 46f));
            AmmoText = UIFactory.Label(AmmoBadge.transform, "AmmoText", "12/15", 24, UIFactory.Orange, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(AmmoText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // -------------------------------------------------- top-right icons
            PauseButton = UIFactory.Button(Root.transform, "II", new Vector2(64f, 64f),
                new Color(0.043f, 0.071f, 0.094f, 0.78f), UIFactory.Ink, null);
            UIFactory.Place(PauseButton.Rect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-24f, -18f), new Vector2(64f, 64f));

            MuteButton = UIFactory.Button(Root.transform, "SFX", new Vector2(64f, 64f),
                new Color(0.043f, 0.071f, 0.094f, 0.78f), UIFactory.Ink, null);
            UIFactory.Place(MuteButton.Rect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-96f, -18f), new Vector2(64f, 64f));

            // ------------------------------------------------------ verb pad
            FireButton = UIFactory.Button(Root.transform, "FIRE", new Vector2(160f, 160f),
                new Color(0.275f, 0.133f, 0.055f, 0.82f), UIFactory.Orange, null);
            UIFactory.Place(FireButton.Rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-40f, 40f), new Vector2(160f, 160f));
            FireButton.PressBg = new Color(0.94f, 0.47f, 0.24f, 0.7f);

            JumpButton = UIFactory.Button(Root.transform, "JUMP", new Vector2(130f, 130f),
                new Color(0.043f, 0.071f, 0.094f, 0.78f), UIFactory.Ink, null);
            UIFactory.Place(JumpButton.Rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-224f, 54f), new Vector2(130f, 130f));

            SlideButton = UIFactory.Button(Root.transform, "SLIDE", new Vector2(130f, 130f),
                new Color(0.043f, 0.071f, 0.094f, 0.78f), UIFactory.Ink, null);
            UIFactory.Place(SlideButton.Rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-372f, 54f), new Vector2(130f, 130f));

            SetVisible(false);
        }

        static void Left(RectTransform rt, float x, float width)
        {
            UIFactory.Place(rt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(x, 0f), new Vector2(width, 30f));
        }

        public void SetVisible(bool visible)
        {
            if (Root != null && Root.activeSelf != visible) Root.SetActive(visible);
        }

        public bool FireHeld
        {
            get { return FireButton != null && FireButton.IsPressed; }
        }

        public void Refresh(GameManager game, bool force)
        {
            int maxHp = Upgrades.MaxHp;
            int hpPercent = Mathf.RoundToInt((game.Hp / (float)maxHp) * 100f);
            if (force || hpPercent != m_cachedHp)
            {
                float pct = Mathf.Clamp01(game.Hp / (float)maxHp);
                HpFill.rectTransform.anchorMax = new Vector2(pct, 1f);
                HpFill.rectTransform.offsetMin = Vector2.zero;
                HpFill.rectTransform.offsetMax = Vector2.zero;
                HpFill.color = pct > 0.6f ? UIFactory.Green : (pct > 0.3f ? new Color(0.89f, 0.70f, 0.29f) : UIFactory.Red);
                HpText.text = Mathf.RoundToInt(game.Hp) + "/" + maxHp;
                m_cachedHp = hpPercent;
            }

            if (force || game.Coins != m_cachedCoins)
            {
                CoinsText.text = "* " + game.Coins.ToString("N0");
                m_cachedCoins = game.Coins;
            }

            int distance = Mathf.RoundToInt(game.Distance);
            if (force || distance != m_cachedDistance)
            {
                DistanceText.text = distance.ToString("N0") + " m";
                m_cachedDistance = distance;
            }

            int score = Mathf.RoundToInt(game.Score);
            if (force || score != m_cachedScore)
            {
                ScoreText.text = score.ToString("N0");
                m_cachedScore = score;
            }

            if (force || game.Ammo != m_cachedAmmo)
            {
                AmmoText.text = game.Ammo.ToString("00") + "/" + Upgrades.MaxAmmo;
                bool low = game.Ammo <= 3 && game.Ammo > 0;
                bool dry = game.Ammo <= 0;
                AmmoText.color = dry ? new Color(1f, 0.6f, 0.54f) : (low ? new Color(1f, 0.85f, 0.63f) : UIFactory.Orange);
                AmmoBadge.color = dry ? new Color(0.35f, 0.1f, 0.07f, 0.9f) : new Color(0.235f, 0.118f, 0.055f, 0.85f);
                m_cachedAmmo = game.Ammo;
            }

            if (force || game.Combo != m_cachedCombo)
            {
                bool show = game.Combo >= 3;
                ComboText.text = show ? "x" + game.ComboMultiplier.ToString("0.0") : "";
                m_cachedCombo = game.Combo;
            }
        }
    }
}
