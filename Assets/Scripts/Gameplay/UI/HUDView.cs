using Nexus.Core;
using Nexus.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(HUDMediator))]
    public class HUDView : View, IAuthoredView
    {
        [SerializeField] private TextMeshProUGUI _movesText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _coinsText;
        [SerializeField] private TextMeshProUGUI _diamondsText;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _capacityText;
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _hintButton;
        [SerializeField] private Button _guideButton;
        [SerializeField] private Image _movesIcon;
        [SerializeField] private CanvasGroup _hudGroup;
        public TextMeshProUGUI MovesText => _movesText;
        public TextMeshProUGUI LevelText => _levelText;
        public TextMeshProUGUI CoinsText => _coinsText;
        public TextMeshProUGUI DiamondsText => _diamondsText;
        public TextMeshProUGUI TimerText => _timerText;
        public TextMeshProUGUI CapacityText => _capacityText;
        public Button UndoButton => _undoButton;
        public Button RestartButton => _restartButton;
        public Button PauseButton => _pauseButton;
        public Button HintButton => _hintButton;
        public Button GuideButton => _guideButton;
        public Image MovesIcon => _movesIcon;
        public CanvasGroup HudGroup => _hudGroup;

        private GameObject _undoBtn, _restartBtn, _hintBtn, _pauseBtn;

        private void Awake()
        {
            // Always run BindReferencesFromChildren to populate text/button references
            // from the UI hierarchy, even when some [SerializeField] fields have been
            // manually assigned in the prefab. This guarantees _movesText, _levelText,
            // _coinsText, etc. are never left null.
            BindReferencesFromChildren();
        }

        /// <summary>
        /// Satisfies IAuthoredView. UI hierarchy is now loaded from prefab;
        /// this method is kept for interface compliance (Editor UI Studio tooling).
        /// </summary>
        public void BuildUI() { }

        public void UpdateMoves(int moves, ILocalizationService loc = null)
        {
            if (MovesText == null)
            {
                NexusLog.Warn("HUDView", nameof(UpdateMoves), "",
                    "MovesText is NULL! BindReferencesFromChildren may have failed.");
                return;
            }
            string format = loc != null ? loc.GetString("format_moves", "Moves: {0}") : "Moves: {0}";
            string text = string.Format(format, moves);
            MovesText.text = text;
#if DEVELOPMENT_BUILD
            NexusLog.Info("HUDView", nameof(UpdateMoves), moves.ToString(),
                $"MovesText set to \"{text}\".");
#endif
            DOTween.Kill(MovesText.transform);
            MovesText.transform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 3, 0.5f).SetAutoKill(true);
        }

        public void UpdateLevel(int level, ILocalizationService loc = null)
        {
            if (LevelText == null) return;
            string format = loc != null ? loc.GetString("format_level", "Level {0}") : "Level {0}";
            LevelText.text = string.Format(format, level);
        }

        public void UpdateCoins(int coins)
        {
            if (CoinsText != null) CoinsText.text = $"{coins:N0}";
        }

        public void UpdateDiamonds(int diamonds)
        {
            if (DiamondsText != null) DiamondsText.text = $"{diamonds:N0}";
        }

        public void UpdateTimer(float remainingSecs)
        {
            if (TimerText == null) return;
            if (remainingSecs > 0)
            {
                int mins = Mathf.FloorToInt(remainingSecs / 60);
                int secs = Mathf.FloorToInt(remainingSecs % 60);
                TimerText.text = $"{mins}:{secs:D2}";
                TimerText.color = remainingSecs < 30f ? GameUIResources.DangerColor : GameUIResources.WarningColor;
            }
            else
            {
                TimerText.text = "";
            }
        }

        public void Localize(ILocalizationService loc)
        {
            if (loc == null) return;
            
            // Localize the labels under the circular buttons
            if (_undoBtn != null && _undoBtn.transform.parent != null)
            {
                var text = _undoBtn.transform.parent.Find("Label")?.GetComponent<TextMeshProUGUI>();
                if (text != null) text.text = loc.GetString("game_undo", "Undo");
            }
            if (_restartBtn != null && _restartBtn.transform.parent != null)
            {
                var text = _restartBtn.transform.parent.Find("Label")?.GetComponent<TextMeshProUGUI>();
                if (text != null) text.text = loc.GetString("game_restart", "Restart");
            }
            if (_hintBtn != null && _hintBtn.transform.parent != null)
            {
                var text = _hintBtn.transform.parent.Find("Label")?.GetComponent<TextMeshProUGUI>();
                if (text != null) text.text = loc.GetString("game_hint", "Hint");
            }

            var instr = transform.Find("InstructionText")?.GetComponent<TextMeshProUGUI>();
            if (instr != null) instr.text = loc.GetString("hud_instruction", "Make each rod a single color");
        }

        private void BindReferencesFromChildren()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                GameUIResources.AddButtonEffects(btn);
                if (btn.name.ToUpper().Contains("UNDO")) { _undoBtn = btn.gameObject; _undoButton = btn; }
                else if (btn.name.ToUpper().Contains("RESTART")) { _restartBtn = btn.gameObject; _restartButton = btn; }
                else if (btn.name.ToUpper().Contains("HINT")) { _hintBtn = btn.gameObject; _hintButton = btn; }
                else if (btn.name.ToUpper().Contains("PAUSE")) { _pauseBtn = btn.gameObject; _pauseButton = btn; }
                else if (btn.name.ToUpper().Contains("GUIDE")) { _guideButton = btn; }
            }

            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in texts)
            {
                var upper = txt.name.ToUpperInvariant();
                if (upper.Contains("MOVE")) _movesText = txt;
                else if (upper.Contains("LEVEL")) _levelText = txt;
                else if (upper.Contains("COIN")) _coinsText = txt;
                else if (upper.Contains("DIAMOND") || upper.Contains("DIA") || upper.Contains("GEM")) _diamondsText = txt;
                else if (upper.Contains("TIMER")) _timerText = txt;
                else if (upper.Contains("CAPACITY")) _capacityText = txt;
            }

            var images = GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                var upper = img.name.ToUpperInvariant();
                if (upper.Contains("MOVE") && _movesIcon == null) _movesIcon = img;
            }

            if (_hudGroup == null)
            {
                var group = GetComponent<CanvasGroup>();
                if (group == null) group = GetComponentInChildren<CanvasGroup>(true);
                _hudGroup = group;
            }
        }
    }
}
