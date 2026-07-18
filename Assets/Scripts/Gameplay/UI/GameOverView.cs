using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(GameOverMediator))]
    public class GameOverView : View, IAuthoredView
    {
        public Button RestartButton { get; private set; }
        public Button QuitButton { get; private set; }
        public Text TitleText { get; private set; }
        public Text MessageText { get; private set; }
        public Text LevelText { get; private set; }
        public Text ProgressText { get; private set; }
        public CanvasGroup CardGroup { get; private set; }

        private GameObject _restartBtn, _quitBtn;

        private void Awake()
        {
            if (transform.childCount == 0 || NeedsSelfBuild())
                BuildUI();
            else
                BindReferencesFromChildren();
        }

        private bool NeedsSelfBuild()
        {
            BindReferencesFromChildren();
            return TitleText == null || RestartButton == null;
        }

        public void BuildUI()
        {
            // Overlay
            var overlay = GetComponent<Image>();
            if (overlay != null)
            {
                overlay.color = GameUIResources.OverlayHeavy;
                overlay.raycastTarget = true;
            }

            // Card
            var cardGo = GameUIResources.CreateCard("Card", transform, GameUIResources.SurfaceDark);
            GameUIResources.SetAnchors(cardGo.GetComponent<RectTransform>(), 0.10f, 0.16f, 0.90f, 0.84f);
            CardGroup = cardGo.GetComponent<CanvasGroup>();

            // Danger accent
            var accent = new GameObject("AccentBar", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(cardGo.transform, false);
            GameUIResources.SetAnchors(accent.GetComponent<RectTransform>(), 0.12f, 0.76f, 0.88f, 0.78f);
            accent.GetComponent<Image>().color = GameUIResources.DangerColor;

            // Title
            var titleGo = GameUIResources.CreateDisplayText("GAME OVER", cardGo.transform, 52, GameUIResources.DangerColor);
            titleGo.name = "Title";
            TitleText = titleGo.GetComponent<Text>();
            GameUIResources.SetAnchors(titleGo.GetComponent<RectTransform>(), 0.12f, 0.62f, 0.88f, 0.74f);

            // Level
            var lvlGo = GameUIResources.CreateText("", cardGo.transform, 20, TextAnchor.MiddleCenter, GameUIResources.MutedTextDark);
            lvlGo.name = "LevelText";
            LevelText = lvlGo.GetComponent<Text>();
            GameUIResources.SetAnchors(lvlGo.GetComponent<RectTransform>(), 0.12f, 0.54f, 0.88f, 0.60f);

            // Message
            var msgGo = GameUIResources.CreateText("Keep trying — you'll get it!", cardGo.transform, 18, TextAnchor.MiddleCenter, GameUIResources.TextOnDark);
            msgGo.name = "Message";
            MessageText = msgGo.GetComponent<Text>();
            GameUIResources.SetAnchors(msgGo.GetComponent<RectTransform>(), 0.12f, 0.42f, 0.88f, 0.50f);

            // Progress encouragement
            var progGo = GameUIResources.CreateText("", cardGo.transform, 16, TextAnchor.MiddleCenter, GameUIResources.MutedTextDark);
            progGo.name = "ProgressText";
            ProgressText = progGo.GetComponent<Text>();
            GameUIResources.SetAnchors(progGo.GetComponent<RectTransform>(), 0.12f, 0.36f, 0.88f, 0.40f);

            float btnW = 280f;

            // Restart
            _restartBtn = GameUIResources.CreateButton("TRY AGAIN", cardGo.transform, btnW, 60);
            _restartBtn.name = "Btn_RESTART";
            GameUIResources.ApplyPrimaryStyle(_restartBtn);
            RestartButton = _restartBtn.GetComponent<Button>();
            GameUIResources.SetAnchors(_restartBtn.GetComponent<RectTransform>(), 0.22f, 0.22f, 0.78f, 0.32f);

            // Quit
            _quitBtn = GameUIResources.CreateButton("MAIN MENU", cardGo.transform, btnW, 48);
            _quitBtn.name = "Btn_MAIN MENU";
            GameUIResources.ApplyTextButtonStyle(_quitBtn);
            QuitButton = _quitBtn.GetComponent<Button>();
            GameUIResources.SetAnchors(_quitBtn.GetComponent<RectTransform>(), 0.22f, 0.10f, 0.78f, 0.18f);
        }

        public void SetLevel(int level, string reason = "")
        {
            if (LevelText != null) LevelText.text = $"Level {level}";
            if (ProgressText != null)
            {
                ProgressText.text = string.IsNullOrEmpty(reason)
                    ? "Don't give up!"
                    : reason;
            }
        }

        public void Localize(ILocalizationService loc)
        {
            if (loc == null) return;
            if (TitleText != null) GameUIResources.LocalizeText(TitleText.gameObject, "game_over_title", loc);
            if (MessageText != null) GameUIResources.LocalizeText(MessageText.gameObject, "game_over_message", loc);
            if (_restartBtn != null) GameUIResources.LocalizeButtonText(_restartBtn, "game_restart", loc);
            if (_quitBtn != null) GameUIResources.LocalizeButtonText(_quitBtn, "menu_main_menu", loc);
        }

        private void BindReferencesFromChildren()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                GameUIResources.AddButtonEffects(btn);
                var upper = btn.name.ToUpperInvariant();
                if (upper.Contains("RESTART") || upper.Contains("TRY")) { _restartBtn = btn.gameObject; RestartButton = btn; }
                else if (upper.Contains("MAIN MENU") || upper.Contains("QUIT")) { _quitBtn = btn.gameObject; QuitButton = btn; }
            }

            var texts = GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                var upper = txt.name.ToUpperInvariant();
                if (upper.Contains("TITLE") || upper.Contains("GAME OVER")) TitleText = txt;
                else if (upper.Contains("MESSAGE") || upper.Contains("FAILED")) MessageText = txt;
                else if (upper.Contains("LEVEL")) LevelText = txt;
                else if (upper.Contains("PROGRESS")) ProgressText = txt;
            }
        }
    }
}
