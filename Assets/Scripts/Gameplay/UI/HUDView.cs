using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(HUDMediator))]
    public class HUDView : View
    {
        public Text MovesText { get; private set; }
        public Text LevelText { get; private set; }
        public Text CoinsText { get; private set; }
        public Text DiamondsText { get; private set; }
        public Button UndoButton { get; private set; }
        public Button RestartButton { get; private set; }
        public Button PauseButton { get; private set; }
        public Button HintButton { get; private set; }
        private GameObject _undoBtn, _restartBtn, _hintBtn;

        private void Awake()
        {
            BindReferencesFromChildren();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NexusLog.Info("HUDView", nameof(Awake), "",
                $"HUD bound. MovesText={MovesText != null}, LevelText={LevelText != null}, " +
                $"CoinsText={CoinsText != null}, UndoBtn={UndoButton != null}, " +
                $"HintBtn={HintButton != null}, PauseBtn={PauseButton != null}.");
#endif
        }

        public void UpdateMoves(int moves, ILocalizationService loc = null)
        {
            if (MovesText == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NexusLog.Warn("HUDView", nameof(UpdateMoves), moves.ToString(), "MovesText is null — HUD will not show move count.");
#endif
                return;
            }
            string format = loc != null ? loc.GetString("format_moves", "Moves: {0}") : "Moves: {0}";
            MovesText.text = string.Format(format, moves);
        }

        public void UpdateLevel(int level, ILocalizationService loc = null)
        {
            if (LevelText == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NexusLog.Warn("HUDView", nameof(UpdateLevel), level.ToString(), "LevelText is null — HUD will not show level number.");
#endif
                return;
            }
            string format = loc != null ? loc.GetString("format_level", "Level {0}") : "Level {0}";
            LevelText.text = string.Format(format, level);
        }

        public void UpdateCoins(int coins)
        {
            if (CoinsText != null) CoinsText.text = coins.ToString();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            else NexusLog.Warn("HUDView", nameof(UpdateCoins), coins.ToString(), "CoinsText is null — coin display missing.");
#endif
        }

        public void UpdateDiamonds(int diamonds)
        {
            if (DiamondsText != null) DiamondsText.text = diamonds.ToString();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            else NexusLog.Warn("HUDView", nameof(UpdateDiamonds), diamonds.ToString(), "DiamondsText is null — diamond display missing.");
#endif
        }

        public void Localize(ILocalizationService loc)
        {
            GameUIResources.LocalizeButtonText(_undoBtn, "game_undo", loc);
            GameUIResources.LocalizeButtonText(_restartBtn, "game_restart", loc);
            GameUIResources.LocalizeButtonText(_hintBtn, "game_hint", loc);
        }

        private void BindReferencesFromChildren()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn.name.ToUpper().Contains("UNDO")) { _undoBtn = btn.gameObject; UndoButton = btn; }
                else if (btn.name.ToUpper().Contains("RESTART")) { _restartBtn = btn.gameObject; RestartButton = btn; }
                else if (btn.name.ToUpper().Contains("HINT")) { _hintBtn = btn.gameObject; HintButton = btn; }
                else if (btn.name.ToUpper().Contains("PAUSE") || btn.name == "II") PauseButton = btn;
            }

            var texts = GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                if (txt.name.ToUpper().Contains("MOVE") || txt.text.Contains("Moves")) MovesText = txt;
                else if (txt.name.ToUpper().Contains("LEVEL") || txt.text.Contains("Level")) LevelText = txt;
                else if (txt.name.ToUpper().Contains("COIN") || txt.transform.parent?.name.Contains("Coin") == true) CoinsText = txt;
                else if (txt.name.ToUpper().Contains("DIAMOND") || txt.name.ToUpper().Contains("DIA") || txt.transform.parent?.name.Contains("Dia") == true) DiamondsText = txt;
            }
        }
    }
}
