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
        public TextMeshProUGUI MovesText { get; private set; }
        public TextMeshProUGUI LevelText { get; private set; }
        public TextMeshProUGUI CoinsText { get; private set; }
        public TextMeshProUGUI DiamondsText { get; private set; }
        public TextMeshProUGUI TimerText { get; private set; }
        public Button UndoButton { get; private set; }
        public Button RestartButton { get; private set; }
        public Button PauseButton { get; private set; }
        public Button HintButton { get; private set; }
        public Button GuideButton { get; private set; }
        public Image MovesIcon { get; private set; }
        public CanvasGroup HudGroup { get; private set; }

        private GameObject _undoBtn, _restartBtn, _hintBtn, _pauseBtn;

        private void Awake()
        {
            BindReferencesFromChildren();
        }

        /// <summary>
        /// Satisfies IAuthoredView. UI hierarchy is now loaded from prefab;
        /// this method is kept for interface compliance (Editor UI Studio tooling).
        /// </summary>
        public void BuildUI() { }

        public void UpdateMoves(int moves, ILocalizationService loc = null)
        {
            if (MovesText == null) return;
            string format = loc != null ? loc.GetString("format_moves", "Moves: {0}") : "Moves: {0}";
            MovesText.text = string.Format(format, moves);
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
            if (DiamondsText != null) DiamondsText.text = $"💎 {diamonds:N0}";
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
                if (btn.name.ToUpper().Contains("UNDO")) { _undoBtn = btn.gameObject; UndoButton = btn; }
                else if (btn.name.ToUpper().Contains("RESTART")) { _restartBtn = btn.gameObject; RestartButton = btn; }
                else if (btn.name.ToUpper().Contains("HINT")) { _hintBtn = btn.gameObject; HintButton = btn; }
                else if (btn.name.ToUpper().Contains("PAUSE")) { _pauseBtn = btn.gameObject; PauseButton = btn; }
                else if (btn.name.ToUpper().Contains("GUIDE")) { GuideButton = btn; }
            }

            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in texts)
            {
                var upper = txt.name.ToUpperInvariant();
                if (upper.Contains("MOVE")) MovesText = txt;
                else if (upper.Contains("LEVEL")) LevelText = txt;
                else if (upper.Contains("COIN")) CoinsText = txt;
                else if (upper.Contains("DIAMOND") || upper.Contains("DIA") || upper.Contains("GEM")) DiamondsText = txt;
                else if (upper.Contains("TIMER")) TimerText = txt;
            }
        }
    }
}
