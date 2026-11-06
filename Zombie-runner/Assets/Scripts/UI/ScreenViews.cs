using UnityEngine;
using UnityEngine.UI;

namespace ZombieRunner
{
    public enum ScreenId { None, Menu, Stage, Camp, Over, Pause, Info }

    /// <summary>
    /// Every full-screen menu: main menu, sector select, base camp (upgrades),
    /// post-run report, pause and the field briefing.
    /// </summary>
    public class ScreenViews : MonoBehaviour
    {
        public GameObject MenuRoot, StageRoot, CampRoot, OverRoot, PauseRoot, InfoRoot;

        // ---- menu
        public Text MenuCoins, MenuDistance, MenuBest, MenuBestScore;
        public ZRButton StartButton, MenuCampButton;

        // ---- stage select
        public ZRButton[] SectorButtons = new ZRButton[3];
        public Text[] SectorStatus = new Text[3];
        public Text SectorNameText, SectorBlurbText, SectorSpeedText;
        public ZRButton DeployButton, StageBackButton;

        // ---- base camp
        public Text CampCoins, CampTokens;
        public ZRButton[] UpgradeButtons = new ZRButton[4];
        public Text[] UpgradePips = new Text[4];
        public ZRButton TokenButton, CampDeployButton, CampBackButton;

        // ---- game over
        public Text OverReason, OverScore, OverDistance, OverCoins, OverKills, OverCombo, OverBest, OverBestScore, OverLifetime;
        public ZRButton RetryButton, OverCampButton, ReviveButton;

        // ---- pause
        public ZRButton ResumeButton, QuitButton;

        // ---- info
        public Text InfoCountText;
        public ZRButton SkipInfoButton;

        // =================================================================
        public void Build(Transform canvas)
        {
            BuildMenu(canvas);
            BuildStage(canvas);
            BuildCamp(canvas);
            BuildOver(canvas);
            BuildPause(canvas);
            BuildInfo(canvas);
        }

        public void Show(ScreenId id)
        {
            SetActive(MenuRoot, id == ScreenId.Menu);
            SetActive(StageRoot, id == ScreenId.Stage);
            SetActive(CampRoot, id == ScreenId.Camp);
            SetActive(OverRoot, id == ScreenId.Over);
            SetActive(PauseRoot, id == ScreenId.Pause);
            SetActive(InfoRoot, id == ScreenId.Info);
        }

        static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }

        // =================================================================
        //  Main menu
        // =================================================================
        void BuildMenu(Transform canvas)
        {
            MenuRoot = MakeScreen(canvas, "Screen_Menu");

            Text title = UIFactory.Label(MenuRoot.transform, "Title", "ZOMBIE RUNNER :", 68, UIFactory.Ink,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(140f, 250f), new Vector2(1160f, 90f));

            Text title2 = UIFactory.Label(MenuRoot.transform, "Title2", "CITY RUINS", 68, UIFactory.Orange,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(title2.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(140f, 168f), new Vector2(1160f, 90f));

            Text tagline = UIFactory.Label(MenuRoot.transform, "Tagline",
                "3D  ENDLESS  ACTION  SURVIVAL      RUN  .  JUMP  .  SHOOT  .  SURVIVE", 22, UIFactory.Dim,
                TextAnchor.MiddleLeft, FontStyle.Normal);
            UIFactory.Place(tagline.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(140f, 104f), new Vector2(1160f, 40f));

            MenuCoins = StatBlock(MenuRoot.transform, 0f, "COINS BANKED");
            MenuDistance = StatBlock(MenuRoot.transform, 280f, "LIFETIME DISTANCE");
            MenuBest = StatBlock(MenuRoot.transform, 560f, "BEST RUN");
            MenuBestScore = StatBlock(MenuRoot.transform, 840f, "BEST SCORE");

            Image cta = UIFactory.Card(MenuRoot.transform, "CTA");
            UIFactory.Place(cta.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-60f, -10f), new Vector2(520f, 440f));

            StartButton = UIFactory.Button(cta.transform, "START MISSION  >", new Vector2(420f, 96f),
                new Color(0.36f, 0.53f, 0.24f, 0.95f), new Color(0.06f, 0.09f, 0.05f), null);
            UIFactory.Place(StartButton.Rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -40f), new Vector2(420f, 96f));

            MenuCampButton = UIFactory.Button(cta.transform, "BASE CAMP  .  UPGRADES", new Vector2(420f, 78f),
                new Color(1f, 1f, 1f, 0.06f), UIFactory.Ink, null);
            UIFactory.Place(MenuCampButton.Rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -160f), new Vector2(420f, 78f));

            Text hint = UIFactory.Label(cta.transform, "Hint",
                "A / D  lanes      W  jump      S  slide\nSPACE  fire      ESC  pause", 19,
                UIFactory.Dimmer, TextAnchor.MiddleRight, FontStyle.Normal);
            hint.lineSpacing = 1.35f;
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 48f), new Vector2(460f, 60f));

            Text footer = UIFactory.Label(MenuRoot.transform, "Footer",
                "PROTOTYPE BUILD 1.0   .   UNITY PORT   .   ALL ART AND AUDIO GENERATED AT RUNTIME", 16,
                UIFactory.Dimmer, TextAnchor.MiddleLeft, FontStyle.Normal);
            UIFactory.Place(footer.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(30f, 26f), new Vector2(1300f, 26f));
        }

        Text StatBlock(Transform parent, float x, string caption)
        {
            Image block = UIFactory.Panel(parent, "Stat_" + caption, new Color(0.043f, 0.071f, 0.094f, 0.82f));
            UIFactory.Place(block.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(140f + x, 10f), new Vector2(260f, 92f));

            Text value = UIFactory.Label(block.transform, "Value", "0", 32, UIFactory.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(value.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(18f, -12f), new Vector2(226f, 40f));

            Text label = UIFactory.Label(block.transform, "Caption", caption, 13, UIFactory.Dimmer, TextAnchor.MiddleLeft, FontStyle.Normal);
            UIFactory.Place(label.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(18f, 14f), new Vector2(226f, 24f));
            return value;
        }

        // =================================================================
        //  Sector select
        // =================================================================
        void BuildStage(Transform canvas)
        {
            StageRoot = MakeScreen(canvas, "Screen_Stage");

            Text title = UIFactory.Label(StageRoot.transform, "Title", "SELECT  SECTOR", 52, UIFactory.Ink,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 380f), new Vector2(900f, 70f));

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                SectorButtons[i] = UIFactory.Button(StageRoot.transform, GameConfig.Sectors[i].Code, new Vector2(300f, 300f),
                    new Color(0.06f, 0.09f, 0.11f, 0.9f), UIFactory.Dim,
                    delegate { if (OnSectorClicked != null) OnSectorClicked(index); });
                UIFactory.Place(SectorButtons[i].Rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-360f + i * 360f, 140f), new Vector2(300f, 300f));

                Text nameLabel = UIFactory.Label(SectorButtons[i].transform, "Name", GameConfig.Sectors[i].Name, 26,
                    UIFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIFactory.Place(nameLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 30f), new Vector2(280f, 34f));

                Text status = UIFactory.Label(SectorButtons[i].transform, "Status", "", 18, UIFactory.Dimmer,
                    TextAnchor.MiddleCenter, FontStyle.Normal);
                UIFactory.Place(status.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, -40f), new Vector2(280f, 30f));
                SectorStatus[i] = status;

                // the caption doubles as the sector code, tucked at the top of the tile
                SectorButtons[i].Caption.alignment = TextAnchor.UpperCenter;
                UIFactory.Place(SectorButtons[i].Caption.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(0f, 30f));
            }

            Image profile = UIFactory.Card(StageRoot.transform, "Profile");
            UIFactory.Place(profile.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -160f), new Vector2(1160f, 210f));

            Text profileTitle = UIFactory.Label(profile.transform, "Heading", "SECTOR PROFILE", 18, UIFactory.Orange,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(profileTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(30f, -20f), new Vector2(400f, 26f));

            SectorNameText = UIFactory.Label(profile.transform, "Name", "City Ruins", 32, UIFactory.Ink,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(SectorNameText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(30f, -52f), new Vector2(700f, 40f));

            SectorBlurbText = UIFactory.Label(profile.transform, "Blurb", "", 20, UIFactory.Dim,
                TextAnchor.MiddleLeft, FontStyle.Normal);
            UIFactory.Place(SectorBlurbText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(30f, -96f), new Vector2(1000f, 30f));

            SectorSpeedText = UIFactory.Label(profile.transform, "Speed", "", 18, UIFactory.Teal,
                TextAnchor.MiddleLeft, FontStyle.Normal);
            UIFactory.Place(SectorSpeedText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(30f, 26f), new Vector2(1000f, 28f));

            DeployButton = UIFactory.Button(StageRoot.transform, "DEPLOY  >", new Vector2(460f, 92f),
                new Color(0.94f, 0.47f, 0.24f, 0.95f), new Color(0.1f, 0.05f, 0.02f), null);
            UIFactory.Place(DeployButton.Rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 150f), new Vector2(460f, 92f));

            StageBackButton = UIFactory.Button(StageRoot.transform, "<  BACK", new Vector2(240f, 64f),
                new Color(1f, 1f, 1f, 0.06f), UIFactory.Ink, null);
            UIFactory.Place(StageBackButton.Rect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(60f, 60f), new Vector2(240f, 64f));
        }

        // =================================================================
        //  Base camp
        // =================================================================
        void BuildCamp(Transform canvas)
        {
            CampRoot = MakeScreen(canvas, "Screen_Camp");

            Text title = UIFactory.Label(CampRoot.transform, "Title", "BASE  CAMP", 52, UIFactory.Ink,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 400f), new Vector2(900f, 70f));

            CampaignBank(out CampCoins, out CampTokens);

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                UpgradeDef def = Upgrades.All[i];

                Image card = UIFactory.Card(CampRoot.transform, "Upgrade_" + def.Name);
                UIFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-630f + i * 420f, 130f), new Vector2(400f, 300f));

                Text nameLabel = UIFactory.Label(card.transform, "Name", def.Name.ToUpperInvariant(), 22, UIFactory.Ink,
                    TextAnchor.MiddleLeft, FontStyle.Bold);
                UIFactory.Place(nameLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                    new Vector2(24f, -24f), new Vector2(-48f, 30f));

                Text desc = UIFactory.Label(card.transform, "Desc", def.Description, 20, UIFactory.Dim,
                    TextAnchor.MiddleLeft, FontStyle.Normal);
                UIFactory.Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                    new Vector2(24f, -66f), new Vector2(-48f, 30f));

                Text pips = UIFactory.Label(card.transform, "Pips", "", 26, UIFactory.Green,
                    TextAnchor.MiddleLeft, FontStyle.Bold);
                UIFactory.Place(pips.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                    new Vector2(24f, -110f), new Vector2(-48f, 34f));
                UpgradePips[i] = pips;

                UpgradeButtons[i] = UIFactory.Button(card.transform, "BUY", new Vector2(352f, 62f),
                    new Color(1f, 1f, 1f, 0.07f), UIFactory.Ink,
                    delegate { if (OnUpgradeClicked != null) OnUpgradeClicked(index); });
                UIFactory.Place(UpgradeButtons[i].Rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 24f), new Vector2(352f, 62f));
            }

            Image tokenCard = UIFactory.Card(CampRoot.transform, "TokenCard");
            UIFactory.Place(tokenCard.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -150f), new Vector2(1660f, 140f));

            Text tokenTitle = UIFactory.Label(tokenCard.transform, "Title", "REVIVE TOKEN", 22, UIFactory.Green,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(tokenTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(28f, -22f), new Vector2(400f, 30f));

            Text tokenDesc = UIFactory.Label(tokenCard.transform, "Desc",
                "Spend one at the moment of death to get back up with 60% HP and a clear road. Max 3 held.", 19,
                UIFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
            UIFactory.Place(tokenDesc.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(28f, 26f), new Vector2(1300f, 30f));

            TokenButton = UIFactory.Button(tokenCard.transform, "250", new Vector2(240f, 72f),
                new Color(1f, 1f, 1f, 0.07f), UIFactory.Ink, delegate { if (OnTokenClicked != null) OnTokenClicked(); });
            UIFactory.Place(TokenButton.Rect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-28f, 0f), new Vector2(240f, 72f));

            CampDeployButton = UIFactory.Button(CampRoot.transform, "DEPLOY  >", new Vector2(440f, 88f),
                new Color(0.36f, 0.53f, 0.24f, 0.95f), new Color(0.06f, 0.09f, 0.05f), null);
            UIFactory.Place(CampDeployButton.Rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-240f, 70f), new Vector2(440f, 88f));

            CampBackButton = UIFactory.Button(CampRoot.transform, "<  BACK", new Vector2(240f, 88f),
                new Color(1f, 1f, 1f, 0.07f), UIFactory.Ink, null);
            UIFactory.Place(CampBackButton.Rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(250f, 70f), new Vector2(240f, 88f));
        }

        void CampaignBank(out Text coins, out Text tokens)
        {
            Image bank = UIFactory.Panel(CampRoot.transform, "Bank", new Color(0.043f, 0.071f, 0.094f, 0.82f));
            UIFactory.Place(bank.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 320f), new Vector2(700f, 70f));

            Text coinsLabel = UIFactory.Label(bank.transform, "Coins", "0  COINS", 24, UIFactory.Orange,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(coinsLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(28f, 0f), new Vector2(320f, 34f));

            Text tokensLabel = UIFactory.Label(bank.transform, "Tokens", "0  TOKENS", 24, UIFactory.Teal,
                TextAnchor.MiddleRight, FontStyle.Bold);
            UIFactory.Place(tokensLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-28f, 0f), new Vector2(320f, 34f));

            coins = coinsLabel;
            tokens = tokensLabel;
        }

        // =================================================================
        //  Post-run report
        // =================================================================
        void BuildOver(Transform canvas)
        {
            OverRoot = MakeScreen(canvas, "Screen_Over");

            Image panel = UIFactory.Card(OverRoot.transform, "Report");
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(960f, 800f));

            Text label = UIFactory.Label(panel.transform, "Label", "POST-RUN REPORT", 18, UIFactory.Dimmer,
                TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -36f), new Vector2(0f, 26f));

            Text title = UIFactory.Label(panel.transform, "Title", "MISSION FAILED", 54, UIFactory.Ink,
                TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -68f), new Vector2(0f, 64f));

            OverReason = UIFactory.Label(panel.transform, "Reason", "SURVIVOR DOWN", 20, UIFactory.Red,
                TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(OverReason.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -140f), new Vector2(0f, 28f));

            OverScore = ReportCell(panel.transform, "SCORE", -310f, 60f, UIFactory.Orange);
            OverDistance = ReportCell(panel.transform, "DISTANCE", 0f, 60f, UIFactory.Green);
            OverCoins = ReportCell(panel.transform, "COINS", 310f, 60f, UIFactory.Orange);
            OverKills = ReportCell(panel.transform, "KILLS", -310f, -70f, UIFactory.Ink);
            OverCombo = ReportCell(panel.transform, "BEST COMBO", 0f, -70f, UIFactory.Ink);
            OverBestScore = ReportCell(panel.transform, "BEST SCORE", 310f, -70f, UIFactory.Ink);

            OverBest = UIFactory.Label(panel.transform, "Best", "", 22, UIFactory.Green,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(OverBest.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -180f), new Vector2(900f, 30f));

            OverLifetime = UIFactory.Label(panel.transform, "Lifetime", "", 18, UIFactory.Dimmer,
                TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Place(OverLifetime.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -214f), new Vector2(900f, 26f));

            RetryButton = UIFactory.Button(panel.transform, "RETRY", new Vector2(280f, 80f),
                new Color(0.94f, 0.47f, 0.24f, 0.95f), new Color(0.1f, 0.05f, 0.02f), null);
            UIFactory.Place(RetryButton.Rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-150f, 100f), new Vector2(280f, 80f));

            OverCampButton = UIFactory.Button(panel.transform, "BASE CAMP", new Vector2(280f, 80f),
                new Color(1f, 1f, 1f, 0.07f), UIFactory.Ink, null);
            UIFactory.Place(OverCampButton.Rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(150f, 100f), new Vector2(280f, 80f));

            ReviveButton = UIFactory.Button(panel.transform, "REVIVE", new Vector2(590f, 74f),
                new Color(0.24f, 0.68f, 0.56f, 0.95f), new Color(0.02f, 0.13f, 0.1f), null);
            UIFactory.Place(ReviveButton.Rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 24f), new Vector2(590f, 74f));
        }

        static Text ReportCell(Transform parent, string caption, float x, float y, Color valueColor)
        {
            Image cell = UIFactory.Panel(parent, "Cell_" + caption, new Color(1f, 1f, 1f, 0.045f));
            UIFactory.Place(cell.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(x, y), new Vector2(276f, 116f));

            Text key = UIFactory.Label(cell.transform, "Key", caption, 15, UIFactory.Dimmer,
                TextAnchor.UpperCenter, FontStyle.Normal);
            UIFactory.Place(key.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -16f), new Vector2(0f, 22f));

            Text value = UIFactory.Label(cell.transform, "Value", "0", 34, valueColor,
                TextAnchor.LowerCenter, FontStyle.Bold);
            UIFactory.Place(value.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 16f), new Vector2(0f, 44f));
            return value;
        }

        // =================================================================
        //  Pause
        // =================================================================
        void BuildPause(Transform canvas)
        {
            PauseRoot = MakeScreen(canvas, "Screen_Pause");

            Image panel = UIFactory.Card(PauseRoot.transform, "Pause");
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(700f, 440f));

            Text label = UIFactory.Label(panel.transform, "Label", "SORTIE PAUSED", 18, UIFactory.Dimmer,
                TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -40f), new Vector2(0f, 24f));

            Text title = UIFactory.Label(panel.transform, "Title", "STAND BY", 50, UIFactory.Ink,
                TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -76f), new Vector2(0f, 58f));

            Text sub = UIFactory.Label(panel.transform, "Sub", "POSITION HELD", 20, UIFactory.Teal,
                TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(sub.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -146f), new Vector2(0f, 26f));

            ResumeButton = UIFactory.Button(panel.transform, "RESUME", new Vector2(280f, 88f),
                new Color(0.36f, 0.53f, 0.24f, 0.95f), new Color(0.06f, 0.09f, 0.05f), null);
            UIFactory.Place(ResumeButton.Rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-150f, 96f), new Vector2(280f, 88f));

            QuitButton = UIFactory.Button(panel.transform, "ABANDON", new Vector2(280f, 88f),
                new Color(1f, 1f, 1f, 0.07f), UIFactory.Ink, null);
            UIFactory.Place(QuitButton.Rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(150f, 96f), new Vector2(280f, 88f));

            Text hint = UIFactory.Label(panel.transform, "Hint", "ESC / P  RESUME      R  RESTART", 16, UIFactory.Dimmer,
                TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 40f), new Vector2(600f, 24f));
        }

        // =================================================================
        //  Field briefing
        // =================================================================
        void BuildInfo(Transform canvas)
        {
            InfoRoot = MakeScreen(canvas, "Screen_Info");

            Image panel = UIFactory.Card(InfoRoot.transform, "Briefing");
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1200f, 620f));

            Text label = UIFactory.Label(panel.transform, "Label", "INSTRUCTION PROTOCOL", 18, UIFactory.Orange,
                TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -42f), new Vector2(0f, 24f));

            Text title = UIFactory.Label(panel.transform, "Title", "FIELD BRIEFING", 46, UIFactory.Ink,
                TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -78f), new Vector2(0f, 54f));

            string body =
                "A / LEFT      change lane\n" +
                "W / UP        jump barriers . hop walkers\n" +
                "S / DOWN      slide under scaffolds\n" +
                "SPACE / TAP   fire (hold to auto-fire)\n\n" +
                "You cannot out-jump a wreck - read the road and pick the right verb.\n" +
                "Shooting walkers refunds ammo. Coins bank permanently at the end of a run.";

            Text bodyText = UIFactory.Label(panel.transform, "Body", body, 24, UIFactory.Dim,
                TextAnchor.UpperLeft, FontStyle.Normal);
            bodyText.lineSpacing = 1.25f;
            UIFactory.Place(bodyText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(70f, -170f), new Vector2(-140f, 300f));

            InfoCountText = UIFactory.Label(panel.transform, "Count", "DEPLOYING IN 3 ...", 22, UIFactory.Green,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(InfoCountText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 104f), new Vector2(600f, 30f));

            SkipInfoButton = UIFactory.Button(panel.transform, "SKIP BRIEFING", new Vector2(300f, 64f),
                new Color(1f, 1f, 1f, 0.07f), UIFactory.Ink, null);
            UIFactory.Place(SkipInfoButton.Rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 32f), new Vector2(300f, 64f));
        }

        // =================================================================
        //  Refresh helpers
        // =================================================================
        GameObject MakeScreen(Transform canvas, string name)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(canvas, false);
            RectTransform rt = (RectTransform)root.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image backdrop = UIFactory.SpriteImage(rt, "Backdrop", UIFactory.GradientSprite, Color.white);
            backdrop.rectTransform.anchorMin = Vector2.zero;
            backdrop.rectTransform.anchorMax = Vector2.one;
            backdrop.rectTransform.offsetMin = Vector2.zero;
            backdrop.rectTransform.offsetMax = Vector2.zero;

            root.SetActive(false);
            return root;
        }

        public void RefreshMenu()
        {
            MenuCoins.text = SaveSystem.Coins.ToString("N0");
            MenuDistance.text = SaveSystem.LifetimeDistance.ToString("N0") + " m";
            MenuBest.text = SaveSystem.BestDistance.ToString("N0") + " m";
            MenuBestScore.text = SaveSystem.BestScore.ToString("N0");
        }

        public void RefreshStage(int selected)
        {
            for (int i = 0; i < 3; i++)
            {
                SectorDef sector = GameConfig.Sectors[i];
                bool unlocked = GameConfig.SectorUnlocked(sector);
                bool isSelected = i == selected;

                SectorButtons[i].Interactable = unlocked;
                SectorButtons[i].NormalBg = isSelected
                    ? new Color(0.94f, 0.47f, 0.24f, 0.16f)
                    : new Color(0.06f, 0.09f, 0.11f, 0.9f);
                SectorButtons[i].NormalText = unlocked ? (isSelected ? UIFactory.Orange : UIFactory.Ink) : UIFactory.Dimmer;
                SectorButtons[i].Apply();

                if (!unlocked)
                {
                    SectorStatus[i].text = "LOCKED  .  " + sector.UnlockDistance.ToString("N0") + " m";
                    SectorStatus[i].color = UIFactory.Dimmer;
                }
                else
                {
                    SectorStatus[i].text = isSelected ? "SELECTED" : "READY";
                    SectorStatus[i].color = isSelected ? UIFactory.Orange : UIFactory.Green;
                }
            }

            SectorDef current = GameConfig.Sectors[Mathf.Clamp(selected, 0, 2)];
            SectorNameText.text = current.Name;
            SectorBlurbText.text = current.Blurb;
            SectorSpeedText.text = "SPEED BAND  " + Mathf.RoundToInt(11f * current.SpeedMultiplier) + " -> " +
                                   Mathf.RoundToInt(24f * current.SpeedMultiplier) + " m/s";
        }

        public void RefreshCamp()
        {
            CampCoins.text = SaveSystem.Coins.ToString("N0") + "  COINS";
            CampTokens.text = SaveSystem.Tokens + " / " + Upgrades.TokenMax + "  TOKENS";

            for (int i = 0; i < 4; i++)
            {
                int level = SaveSystem.LevelOf(i);
                int cost = SaveSystem.CostOf(i);
                bool maxed = SaveSystem.IsMax(i);
                bool affordable = !maxed && SaveSystem.Coins >= cost;

                string pips = "";
                for (int p = 0; p < 3; p++) pips += p < level ? "[] " : "__ ";
                UpgradePips[i].text = pips + "   LV " + level + "/3";

                UpgradeButtons[i].Interactable = !maxed && affordable;
                UpgradeButtons[i].SetText(maxed ? "MAXED" : cost + "  COINS");
                UpgradeButtons[i].NormalBg = affordable ? new Color(0.94f, 0.47f, 0.24f, 0.85f) : new Color(1f, 1f, 1f, 0.07f);
                UpgradeButtons[i].NormalText = affordable ? new Color(0.1f, 0.05f, 0.02f) : UIFactory.Ink;
                UpgradeButtons[i].Apply();
            }

            bool canBuyToken = SaveSystem.Tokens < Upgrades.TokenMax && SaveSystem.Coins >= Upgrades.TokenCost;
            TokenButton.Interactable = canBuyToken;
            TokenButton.SetText(SaveSystem.Tokens >= Upgrades.TokenMax ? "MAX 3" : Upgrades.TokenCost + "  COINS");
            TokenButton.NormalBg = canBuyToken ? new Color(0.24f, 0.68f, 0.56f, 0.9f) : new Color(1f, 1f, 1f, 0.07f);
            TokenButton.NormalText = canBuyToken ? new Color(0.02f, 0.13f, 0.1f) : UIFactory.Ink;
            TokenButton.Apply();
        }

        public void RefreshOver(GameManager game, bool canRevive)
        {
            OverScore.text = Mathf.RoundToInt(game.Score).ToString("N0");
            OverDistance.text = Mathf.RoundToInt(game.Distance).ToString("N0") + " m";
            OverCoins.text = game.Coins.ToString("N0");
            OverKills.text = game.Kills.ToString("N0");
            OverCombo.text = "x" + Mathf.Max(1, Mathf.FloorToInt(game.BestCombo / 3f) + 1);
            OverBestScore.text = SaveSystem.BestScore.ToString("N0");
            OverBest.text = "BEST RUN  " + SaveSystem.BestDistance.ToString("N0") + " m";
            OverLifetime.text = "BANKED  " + SaveSystem.Coins.ToString("N0") + " coins   .   " +
                                SaveSystem.LifetimeDistance.ToString("N0") + " m travelled";
            OverReason.text = game.DeathByRoadblock ? "ROADBLOCK IMPACT" : "SURVIVOR DOWN";
            if (ReviveButton != null) ReviveButton.gameObject.SetActive(canRevive);
            if (canRevive) ReviveButton.SetText("REVIVE  (" + SaveSystem.Tokens + ")");
        }

        public void SetInfoCountdown(int seconds)
        {
            InfoCountText.text = seconds > 0 ? "DEPLOYING IN " + seconds + " ..." : "GO!";
        }

        // callbacks wired by GameManager
        public System.Action<int> OnSectorClicked;
        public System.Action<int> OnUpgradeClicked;
        public System.Action OnTokenClicked;
    }
}
