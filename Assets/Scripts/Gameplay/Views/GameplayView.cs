using Nexus.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay
{
    [Mediator(typeof(GameplayMediator))]
    public class GameplayView : View
    {
        [SerializeField] private Text _movesText;
        [SerializeField] private GameObject _winPanel;
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _restartButton;

        public System.Action OnUndoClicked;
        public System.Action OnRestartClicked;

        protected virtual void Awake()
        {
            if (_undoButton != null) _undoButton.onClick.AddListener(() => OnUndoClicked?.Invoke());
            if (_restartButton != null) _restartButton.onClick.AddListener(() => OnRestartClicked?.Invoke());
        }

        public void UpdateMoves(int moves)
        {
            if (_movesText != null) _movesText.text = $"Moves: {moves}";
        }

        public void ShowWinPanel(bool show)
        {
            if (_winPanel != null) _winPanel.SetActive(show);
        }
    }
}
