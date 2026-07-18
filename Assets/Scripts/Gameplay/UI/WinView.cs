using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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
        public Text LevelText { get; private set; }
        private GameObject _nextBtn, _quitBtn;
        private ILocalizationService _loc;
        [Inject] private IAudioService _audio;

        private void Awake()
        {
            var overlay = GetComponent<Image>();
            if (overlay != null && overlay.color == Color.white)
            {
                overlay.color = new Color(0.02f, 0.03f, 0.05f, 0.72f);
            }

            var card = transform.Find("Card")?.gameObject;
            if (card == null)
            {
                card = GameUIResources.CreatePanel("Card", transform);
                GameUIResources.SetAnchors(card.GetComponent<RectTransform>(), 0.12f, 0.16f, 0.88f, 0.82f);
                card.GetComponent<Image>().color = GameUIResources.SurfaceColor;
            }

            var accent = new GameObject("AccentBar", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(card.transform, false);
            GameUIResources.SetAnchors(accent.GetComponent<RectTransform>(), 0.18f, 0.74f, 0.82f, 0.76f);
            accent.GetComponent<Image>().color = GameUIResources.SuccessColor;

            if (TitleText == null)
            {
                var existingTitle = transform.Find("Text");
                if (existingTitle != null)
                {
                    TitleText = existingTitle.GetComponent<Text>();
                }
            }

            if (TitleText == null)
            {
                var titleGo = GameUIResources.CreateText("YOU WIN!", card.transform, 50, TextAnchor.MiddleCenter, GameUIResources.TextColor);
                TitleText = titleGo.GetComponent<Text>();
                TitleText.fontStyle = FontStyle.Bold;
                GameUIResources.SetAnchors(titleGo.GetComponent<RectTransform>(), 0.16f, 0.62f, 0.84f, 0.72f);
            }

            if (LevelText == null)
            {
                var levelGo = GameUIResources.CreateText("LEVEL 1", card.transform, 86, TextAnchor.MiddleCenter, GameUIResources.AccentColor);
                LevelText = levelGo.GetComponent<Text>();
                var levelOutline = levelGo.GetComponent<Outline>();
                if (levelOutline == null) levelOutline = levelGo.AddComponent<Outline>();
                levelOutline.effectColor = new Color(0f, 0f, 0f, 0.55f);
                levelOutline.effectDistance = new Vector2(3f, -3f);
                GameUIResources.SetAnchors(levelGo.GetComponent<RectTransform>(), 0.12f, 0.48f, 0.88f, 0.60f);
            }

            var starRow = new GameObject("Stars", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            starRow.transform.SetParent(card.transform, false);
            var starRect = starRow.GetComponent<RectTransform>();
            GameUIResources.SetAnchors(starRect, 0.24f, 0.36f, 0.76f, 0.44f);
            var hlg = starRow.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 14;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            for (int i = 0; i < 3; i++)
            {
                var starGo = GameUIResources.CreateText("★", starRow.transform, 46, TextAnchor.MiddleCenter, new Color(0.32f, 0.34f, 0.38f));
                starGo.GetComponent<RectTransform>().sizeDelta = new Vector2(52, 52);
                starGo.transform.localScale = Vector3.one;
                StarIcons[i] = starGo.GetComponent<Text>();
            }

            var movesGo = GameUIResources.CreateText("", card.transform, 22, TextAnchor.MiddleCenter, GameUIResources.TextColor);
            GameUIResources.SetAnchors(movesGo.GetComponent<RectTransform>(), 0.16f, 0.28f, 0.84f, 0.34f);
            MovesText = movesGo.GetComponent<Text>();

            var rewardGo = GameUIResources.CreateText("", card.transform, 22, TextAnchor.MiddleCenter, GameUIResources.SuccessColor);
            GameUIResources.SetAnchors(rewardGo.GetComponent<RectTransform>(), 0.16f, 0.22f, 0.84f, 0.28f);
            RewardText = rewardGo.GetComponent<Text>();

            _nextBtn = transform.Find("Btn_NEXT LEVEL")?.gameObject;
            if (_nextBtn == null)
            {
                _nextBtn = GameUIResources.CreateButton("NEXT LEVEL", card.transform, 312, 68);
                GameUIResources.SetAnchors(_nextBtn.GetComponent<RectTransform>(), 0.24f, 0.12f, 0.76f, 0.20f);
            }
            NextLevelButton = _nextBtn.GetComponent<Button>();

            _quitBtn = transform.Find("Btn_MAIN MENU")?.gameObject;
            if (_quitBtn == null)
            {
                _quitBtn = GameUIResources.CreateButton("MAIN MENU", card.transform, 312, 58);
                GameUIResources.SetAnchors(_quitBtn.GetComponent<RectTransform>(), 0.24f, 0.04f, 0.76f, 0.10f);
                GameUIResources.ApplyOutlineStyle(_quitBtn);
            }
            QuitButton = _quitBtn.GetComponent<Button>();
        }

        public void SetLevel(int level, ILocalizationService loc = null)
        {
            if (LevelText == null || level <= 0)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NexusLog.Warn("WinView", nameof(SetLevel), level.ToString(),
                    $"SetLevel skipped. LevelText={(LevelText == null ? "null" : "ok")}, level={level}.");
#endif
                return;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NexusLog.Info("WinView", nameof(SetLevel), level.ToString(), $"Displaying level {level} on Win screen.");
#endif
            string format = loc != null ? loc.GetString("format_level", "LEVEL {0}") : "LEVEL {0}";
            LevelText.text = string.Format(format, level);

            DOTween.Kill(LevelText.transform);
            LevelText.transform.localScale = Vector3.one * 0.6f;
            LevelText.transform.DOScale(1f, 0.45f).SetEase(Ease.OutBack).SetAutoKill(true);
        }

        public void ShowResults(int moves, int targetMoves, int coins, int xp, int stars)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NexusLog.Info("WinView", nameof(ShowResults), "",
                $"ShowResults: moves={moves}/{targetMoves}, coins={coins}, xp={xp}, stars={stars}.");
#endif
            if (MovesText != null)
            {
                string format = _loc != null
                    ? _loc.GetString("win_moves_format", "Moves: {0}{1}")
                    : "Moves: {0}{1}";
                string targetPart = targetMoves > 0 ? $" / {targetMoves}" : string.Empty;
                MovesText.text = string.Format(format, moves, targetPart);
            }
            if (RewardText != null)
            {
                string format = _loc != null
                    ? _loc.GetString("win_reward_format", "+{0} Coins   +{1} XP")
                    : "+{0} Coins   +{1} XP";
                RewardText.text = string.Format(format, coins, xp);
            }

            for (int i = 0; i < StarIcons.Length; i++)
            {
                if (StarIcons[i] == null) continue;
                DOTween.Kill(StarIcons[i].transform);
                StarIcons[i].color = new Color(0.35f, 0.35f, 0.40f);
                StarIcons[i].transform.localScale = Vector3.zero;
            }

            for (int i = 0; i < StarIcons.Length; i++)
            {
                if (StarIcons[i] == null) continue;

                int index = i;
                bool isEarned = index < stars;

                StarIcons[index].transform
                    .DOScale(1f, 0.35f)
                    .SetDelay(index * 0.2f)
                    .SetEase(Ease.OutBack)
                    .SetAutoKill(true)
                    .OnStart(() =>
                    {
                        if (!isEarned) return;
                        StarIcons[index].color = GameUIResources.AccentColor;
                        if (_audio != null)
                        {
                            var chime = ProceduralAudio.GetOrCreateMoveClip();
                            _audio.PlaySfx(chime, 1.0f, 1.0f + index * 0.1f, 1.0f + index * 0.1f);
                        }
                    });
            }
        }

        public void Localize(ILocalizationService loc)
        {
            if (loc == null) return;
            _loc = loc;

            if (TitleText != null)
            {
                GameUIResources.LocalizeText(TitleText.gameObject, "game_you_win", loc);
            }

            if (_nextBtn != null)
            {
                GameUIResources.LocalizeButtonText(_nextBtn, "game_next_level", loc);
            }

            if (_quitBtn != null)
            {
                GameUIResources.LocalizeButtonText(_quitBtn, "menu_main_menu", loc);
            }

            if (TitleText == null || _nextBtn == null || _quitBtn == null)
            {
                NexusLog.Warn("WinView", nameof(Localize), "",
                    "One or more WinView UI references were missing during localization.");
            }
        }

        public void ShowResults(int moves, int targetMoves, int coins, int xp)
        {
            ShowResults(moves, targetMoves, coins, xp, 1);
        }


    }
}
