using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(WinMediator))]
    public class WinView : View, IAuthoredView
    {
        public Button NextLevelButton { get; private set; }
        public Button QuitButton { get; private set; }
        public Text MovesText { get; private set; }
        public Text RewardText { get; private set; }
        public Text[] StarIcons { get; private set; } = new Text[3];
        public Text TitleText { get; private set; }
        public Text LevelText { get; private set; }
        public Text BestScoreText { get; private set; }
        public GameObject[] Stars { get; private set; } = new GameObject[3];
        public CanvasGroup CardGroup { get; private set; }

        private GameObject _nextBtn, _quitBtn;
        private ILocalizationService _loc;
        [Inject] private IAudioService _audio;

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
            return TitleText == null || NextLevelButton == null;
        }

        public void BuildUI()
        {
            // Overlay
            var overlay = GetComponent<Image>();
            if (overlay != null)
            {
                overlay.color = GameUIResources.OverlayMedium;
                overlay.raycastTarget = true;
            }

            // Card
            var cardGo = GameUIResources.CreateCard("Card", transform, GameUIResources.SurfaceDark);
            GameUIResources.SetAnchors(cardGo.GetComponent<RectTransform>(), 0.08f, 0.12f, 0.92f, 0.88f);
            CardGroup = cardGo.GetComponent<CanvasGroup>();

            // Success accent bar at top
            var accentBar = new GameObject("AccentBar", typeof(RectTransform), typeof(Image));
            accentBar.transform.SetParent(cardGo.transform, false);
            GameUIResources.SetAnchors(accentBar.GetComponent<RectTransform>(), 0.10f, 0.82f, 0.90f, 0.84f);
            accentBar.GetComponent<Image>().color = GameUIResources.SuccessColor;

            // Title
            var titleGo = GameUIResources.CreateDisplayText("YOU WIN!", cardGo.transform, 48, GameUIResources.SuccessColor);
            titleGo.name = "Title";
            TitleText = titleGo.GetComponent<Text>();
            GameUIResources.SetAnchors(titleGo.GetComponent<RectTransform>(), 0.12f, 0.68f, 0.88f, 0.80f);

            // Level
            var levelGo = GameUIResources.CreateDisplayText("LEVEL 1", cardGo.transform, 72, GameUIResources.AccentColor);
            levelGo.name = "LevelText";
            LevelText = levelGo.GetComponent<Text>();
            GameUIResources.SetAnchors(levelGo.GetComponent<RectTransform>(), 0.08f, 0.54f, 0.92f, 0.66f);

            // Stars
            var starRow = new GameObject("Stars", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            starRow.transform.SetParent(cardGo.transform, false);
            var starRect = starRow.GetComponent<RectTransform>();
            GameUIResources.SetAnchors(starRect, 0.18f, 0.38f, 0.82f, 0.50f);
            var hlg = starRow.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            for (int i = 0; i < 3; i++)
            {
                var starGo = GameUIResources.CreateDisplayText("★", starRow.transform, 52, GameUIResources.StarEmpty);
                starGo.name = $"Star{i + 1}";
                Stars[i] = starGo;
                StarIcons[i] = starGo.GetComponent<Text>();
                starGo.transform.localScale = Vector3.zero;
            }

            // Moves
            var movesGo = GameUIResources.CreateText("", cardGo.transform, 20, TextAnchor.MiddleCenter, GameUIResources.MutedTextDark);
            movesGo.name = "MovesText";
            MovesText = movesGo.GetComponent<Text>();
            GameUIResources.SetAnchors(movesGo.GetComponent<RectTransform>(), 0.12f, 0.30f, 0.88f, 0.36f);

            // Best score
            var bestGo = GameUIResources.CreateText("", cardGo.transform, 16, TextAnchor.MiddleCenter, GameUIResources.MutedTextDark);
            bestGo.name = "BestScoreText";
            BestScoreText = bestGo.GetComponent<Text>();
            GameUIResources.SetAnchors(bestGo.GetComponent<RectTransform>(), 0.12f, 0.26f, 0.88f, 0.30f);

            // Reward
            var rewardGo = GameUIResources.CreateText("", cardGo.transform, 18, TextAnchor.MiddleCenter, GameUIResources.CoinColor);
            rewardGo.name = "RewardText";
            RewardText = rewardGo.GetComponent<Text>();
            GameUIResources.SetAnchors(rewardGo.GetComponent<RectTransform>(), 0.12f, 0.20f, 0.88f, 0.26f);

            // Next Level button
            _nextBtn = GameUIResources.CreateButton("NEXT LEVEL", cardGo.transform, 300, 60);
            _nextBtn.name = "Btn_NEXT LEVEL";
            GameUIResources.ApplySuccessStyle(_nextBtn);
            NextLevelButton = _nextBtn.GetComponent<Button>();
            GameUIResources.SetAnchors(_nextBtn.GetComponent<RectTransform>(), 0.20f, 0.10f, 0.80f, 0.18f);

            // Quit button
            _quitBtn = GameUIResources.CreateButton("MAIN MENU", cardGo.transform, 300, 46);
            _quitBtn.name = "Btn_MAIN MENU";
            GameUIResources.ApplyTextButtonStyle(_quitBtn);
            QuitButton = _quitBtn.GetComponent<Button>();
            GameUIResources.SetAnchors(_quitBtn.GetComponent<RectTransform>(), 0.20f, 0.04f, 0.80f, 0.09f);
        }

        public void SetLevel(int level, ILocalizationService loc = null)
        {
            if (LevelText == null || level <= 0) return;
            string format = loc != null ? loc.GetString("format_level", "LEVEL {0}") : "LEVEL {0}";
            LevelText.text = string.Format(format, level);

            DOTween.Kill(LevelText.transform);
            LevelText.transform.localScale = Vector3.one * 0.6f;
            LevelText.transform.DOScale(1f, 0.45f).SetEase(DG.Tweening.Ease.OutBack).SetAutoKill(true);
        }

        public void ShowResults(int moves, int targetMoves, int coins, int xp, int stars, int bestMoves = 0)
        {
            if (MovesText != null)
            {
                string format = _loc?.GetString("win_moves_format", "Moves: {0}{1}") ?? "Moves: {0}{1}";
                string targetPart = targetMoves > 0 ? $" / {targetMoves}" : string.Empty;
                MovesText.text = string.Format(format, moves, targetPart);
            }

            if (BestScoreText != null && bestMoves > 0 && moves < bestMoves)
            {
                string fmt = _loc?.GetString("win_best_format", "Best: {0} 🎉") ?? "Best: {0} 🎉";
                BestScoreText.text = string.Format(fmt, bestMoves);
                BestScoreText.color = GameUIResources.SuccessColor;
            }
            else if (BestScoreText != null)
            {
                BestScoreText.text = "";
            }

            if (RewardText != null)
            {
                string format = _loc?.GetString("win_reward_format", "+{0} 🪙  +{1} ⚡") ?? "+{0} 🪙  +{1} ⚡";
                RewardText.text = string.Format(format, coins, xp);
            }

            // Animate stars
            for (int i = 0; i < Stars.Length; i++)
            {
                if (Stars[i] == null) continue;
                DOTween.Kill(Stars[i].transform);
                Stars[i].transform.localScale = Vector3.zero;
            }

            for (int i = 0; i < Stars.Length; i++)
            {
                if (Stars[i] == null) continue;
                int index = i;
                bool isEarned = index < stars;

                Stars[index].transform.DOScale(1f, 0.35f)
                    .SetDelay(index * 0.22f)
                    .SetEase(DG.Tweening.Ease.OutBack)
                    .SetAutoKill(true)
                    .OnStart(() =>
                    {
                        if (!isEarned) return;
                        var starText = StarIcons[index];
                        if (starText != null)
                        {
                            starText.color = GameUIResources.StarEarned;
                            var outline = starText.GetComponent<Outline>();
                            if (outline != null) outline.effectColor = new Color(0.8f, 0.6f, 0f, 0.6f);
                        }
                        if (_audio != null)
                        {
                            var chime = ProceduralAudio.GetOrCreateMoveClip();
                            _audio.PlaySfx(chime, 1.0f, 1.0f + index * 0.12f, 1.0f + index * 0.12f);
                        }
                    });
            }
        }

        public void ShowResults(int moves, int targetMoves, int coins, int xp)
        {
            ShowResults(moves, targetMoves, coins, xp, 1);
        }

        public void Localize(ILocalizationService loc)
        {
            if (loc == null) return;
            _loc = loc;
            if (TitleText != null) GameUIResources.LocalizeText(TitleText.gameObject, "game_you_win", loc);
            if (_nextBtn != null) GameUIResources.LocalizeButtonText(_nextBtn, "game_next_level", loc);
            if (_quitBtn != null) GameUIResources.LocalizeButtonText(_quitBtn, "menu_main_menu", loc);
        }

        private void BindReferencesFromChildren()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                GameUIResources.AddButtonEffects(btn);
                var upper = btn.name.ToUpperInvariant();
                if (upper.Contains("NEXT") || upper.Contains("NEXT LEVEL")) { _nextBtn = btn.gameObject; NextLevelButton = btn; }
                else if (upper.Contains("MAIN MENU") || upper.Contains("QUIT")) { _quitBtn = btn.gameObject; QuitButton = btn; }
            }

            var texts = GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                var upper = txt.name.ToUpperInvariant();
                if (upper.Contains("TITLE") || upper.Contains("WIN")) TitleText = txt;
                else if (upper.Contains("LEVEL") && txt.fontSize >= 60) LevelText = txt;
                else if (upper.Contains("MOVE")) MovesText = txt;
                else if (upper.Contains("REWARD")) RewardText = txt;
                else if (upper.Contains("BEST") || upper.Contains("SCORE")) BestScoreText = txt;
            }

            var starTexts = GetComponentsInChildren<Text>(true);
            int starIdx = 0;
            foreach (var txt in starTexts)
            {
                if (txt.text == "★" && starIdx < StarIcons.Length)
                {
                    StarIcons[starIdx] = txt;
                    Stars[starIdx] = txt.gameObject;
                    starIdx++;
                }
            }
        }
    }
}
