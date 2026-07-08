using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(WinMediator))]
    public class WinView : View
    {
        public Button NextLevelButton { get; private set; }
        public Button QuitButton { get; private set; }
        public Text MovesText { get; private set; }
        public Text RewardText { get; private set; }
        public Text[] StarIcons { get; private set; } = new Text[3];
        public Text TitleText { get; private set; }
        private GameObject _nextBtn, _quitBtn;

        private void Awake()
        {
            var overlay = GetComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.70f);

            var card = GameUIResources.CreatePanel("Card", transform);
            GameUIResources.SetAnchors(card.GetComponent<RectTransform>(), 0.16f, 0.20f, 0.84f, 0.80f);
            card.GetComponent<Image>().color = GameUIResources.PanelColor;

            var titleGo = GameUIResources.CreateText("YOU WIN!", transform, 52, TextAnchor.MiddleCenter, GameUIResources.AccentColor);
            TitleText = titleGo.GetComponent<Text>();
            TitleText.fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(titleGo.GetComponent<RectTransform>(), 0.2f, 0.66f, 0.8f, 0.76f);

            var starRow = new GameObject("Stars", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            starRow.transform.SetParent(transform, false);
            var starRect = starRow.GetComponent<RectTransform>();
            GameUIResources.SetAnchors(starRect, 0.28f, 0.58f, 0.72f, 0.65f);
            var hlg = starRow.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            for (int i = 0; i < 3; i++)
            {
                var starGo = GameUIResources.CreateText("★", starRow.transform, 44, TextAnchor.MiddleCenter, new Color(0.35f, 0.35f, 0.40f));
                starGo.GetComponent<RectTransform>().sizeDelta = new Vector2(48, 48);
                starGo.transform.localScale = Vector3.one;
                StarIcons[i] = starGo.GetComponent<Text>();
            }

            var movesGo = GameUIResources.CreateText("", transform, 22, TextAnchor.MiddleCenter, GameUIResources.TextColor);
            GameUIResources.SetAnchors(movesGo.GetComponent<RectTransform>(), 0.2f, 0.48f, 0.8f, 0.55f);
            MovesText = movesGo.GetComponent<Text>();

            var rewardGo = GameUIResources.CreateText("", transform, 22, TextAnchor.MiddleCenter, GameUIResources.SuccessColor);
            GameUIResources.SetAnchors(rewardGo.GetComponent<RectTransform>(), 0.2f, 0.42f, 0.8f, 0.49f);
            RewardText = rewardGo.GetComponent<Text>();

            _nextBtn = GameUIResources.CreateButton("NEXT LEVEL", transform, 300, 68);
            GameUIResources.SetAnchors(_nextBtn.GetComponent<RectTransform>(), 0.28f, 0.30f, 0.72f, 0.40f);
            NextLevelButton = _nextBtn.GetComponent<Button>();

            _quitBtn = GameUIResources.CreateButton("MAIN MENU", transform, 300, 56);
            GameUIResources.SetAnchors(_quitBtn.GetComponent<RectTransform>(), 0.28f, 0.22f, 0.72f, 0.28f);
            GameUIResources.ApplyOutlineStyle(_quitBtn);
            QuitButton = _quitBtn.GetComponent<Button>();
        }

        public void ShowResults(int moves, int targetMoves, int coins, int xp, int stars)
        {
            if (MovesText != null)
            {
                MovesText.text = $"Moves: {moves}" + (targetMoves > 0 ? $" / {targetMoves}" : "");
            }
            if (RewardText != null)
            {
                RewardText.text = $"+{coins} Coins   +{xp} XP";
            }
            for (int i = 0; i < StarIcons.Length; i++)
            {
                if (StarIcons[i] == null) continue;
                StarIcons[i].color = i < stars ? GameUIResources.AccentColor : new Color(0.35f, 0.35f, 0.40f);
            }
        }

        public void Localize(ILocalizationService loc)
        {
            GameUIResources.LocalizeText(TitleText.gameObject, "game_you_win", loc);
            GameUIResources.LocalizeButtonText(_nextBtn, "game_next_level", loc);
            GameUIResources.LocalizeButtonText(_quitBtn, "menu_main_menu", loc);
        }

        public void ShowResults(int moves, int targetMoves, int coins, int xp)
        {
            ShowResults(moves, targetMoves, coins, xp, 1);
        }


    }
}
