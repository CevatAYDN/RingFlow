using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(WinMediator))]
    public class WinView : View, IAuthoredView
    {
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private TextMeshProUGUI _movesText;
        [SerializeField] private TextMeshProUGUI _rewardText;
        [SerializeField] private Image[] _starImages = new Image[3];
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _bestScoreText;
        [SerializeField] private CanvasGroup _cardGroup;
        public Button NextLevelButton => _nextLevelButton;
        public Button QuitButton => _quitButton;
        public TextMeshProUGUI MovesText => _movesText;
        public TextMeshProUGUI RewardText => _rewardText;
        public Image[] StarImages => _starImages;
        public TextMeshProUGUI TitleText => _titleText;
        public TextMeshProUGUI LevelText => _levelText;
        public TextMeshProUGUI BestScoreText => _bestScoreText;
        public CanvasGroup CardGroup => _cardGroup;
        private TextMeshProUGUI _coinsValueText;
        private TextMeshProUGUI _xpValueText;

        private GameObject _nextBtn, _quitBtn;
        private ILocalizationService _loc;
        [Inject] private IAudioService _audio;
        [Inject] private IProceduralAudioService _proceduralAudio;

        private void Awake()
        {
            BindReferencesFromChildren();
        }

        /// <summary>
        /// Satisfies IAuthoredView. UI hierarchy is now loaded from prefab;
        /// this method is kept for interface compliance (Editor UI Studio tooling).
        /// </summary>
        public void BuildUI() { }

        public void SetLevel(int level, ILocalizationService loc = null)
        {
            if (LevelText == null || level < 0) return;
            string format = loc != null ? loc.GetString("format_level", "Level {0}") : "Level {0}";
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
                string fmt = _loc?.GetString("win_best_format", "New Best: {0}!") ?? "New Best: {0}!";
                BestScoreText.text = string.Format(fmt, bestMoves);
                BestScoreText.color = GameUIResources.SuccessColor;
            }
            else if (BestScoreText != null)
            {
                BestScoreText.text = "";
            }

            // Update sprite-based reward counters
            if (_coinsValueText != null) _coinsValueText.text = $"+{coins}";
            if (_xpValueText != null) _xpValueText.text = $"+{xp}";

            // Animate stars
            for (int i = 0; i < StarImages.Length; i++)
            {
                if (StarImages[i] == null) continue;
                DOTween.Kill(StarImages[i].transform);
                StarImages[i].transform.localScale = Vector3.zero;
            }

            for (int i = 0; i < StarImages.Length; i++)
            {
                if (StarImages[i] == null) continue;
                int index = i;
                bool isEarned = index < stars;

                StarImages[index].transform.DOScale(1f, 0.35f)
                    .SetDelay(index * 0.22f)
                    .SetEase(DG.Tweening.Ease.OutBack)
                    .SetAutoKill(true)
                    .OnStart(() =>
                    {
                        // Swap star sprite to filled when earned
                        if (isEarned && StarImages[index] != null)
                        {
                            StarImages[index].sprite = GameUIResources.GetSprite("star_filled");
                            StarImages[index].transform.DOPunchScale(Vector3.one * 0.3f, 0.25f, 3, 0.5f).SetAutoKill(true);
                        }
                        if (_audio != null)
                        {
                            var chime = _proceduralAudio.GetOrCreateMoveClip();
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
                if (upper.Contains("NEXT") || upper.Contains("NEXT LEVEL")) { _nextBtn = btn.gameObject; _nextLevelButton = btn; }
                else if (upper.Contains("MAIN MENU") || upper.Contains("QUIT")) { _quitBtn = btn.gameObject; _quitButton = btn; }
            }

            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in texts)
            {
                var upper = txt.name.ToUpperInvariant();
                if (upper.Contains("TITLE") || upper.Contains("WIN")) _titleText = txt;
                else if (upper.Contains("LEVEL") && txt.fontSize >= 60) _levelText = txt;
                else if (upper.Contains("MOVE")) _movesText = txt;
                else if (upper.Contains("REWARD")) _rewardText = txt;
                else if (upper.Contains("BEST") || upper.Contains("SCORE")) _bestScoreText = txt;
            }

            var allImages = GetComponentsInChildren<Image>(true);
            int starIdx = 0;
            foreach (var img in allImages)
            {
                if ((img.name == "Star1" || img.name == "Star2" || img.name == "Star3") && starIdx < _starImages.Length)
                {
                    _starImages[starIdx] = img;
                    starIdx++;
                }
            }
            // Reward text sub-items
            foreach (var txt in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (txt.name == "CoinsValue") _coinsValueText = txt;
                else if (txt.name == "XPValue") _xpValueText = txt;
            }

            if (_cardGroup == null)
            {
                var group = GetComponent<CanvasGroup>();
                if (group == null) group = GetComponentInChildren<CanvasGroup>(true);
                _cardGroup = group;
            }
        }
    }
}
