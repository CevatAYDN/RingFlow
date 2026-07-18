using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(HUDMediator))]
    public class HUDView : View, IAuthoredView
    {
        public Text MovesText { get; private set; }
        public Text LevelText { get; private set; }
        public Text CoinsText { get; private set; }
        public Text DiamondsText { get; private set; }
        public Text TimerText { get; private set; }
        public Button UndoButton { get; private set; }
        public Button RestartButton { get; private set; }
        public Button PauseButton { get; private set; }
        public Button HintButton { get; private set; }
        public Image MovesIcon { get; private set; }
        public CanvasGroup HudGroup { get; private set; }

        private GameObject _undoBtn, _restartBtn, _hintBtn, _pauseBtn, _topBar;

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
            return MovesText == null;
        }

        public void BuildUI()
        {
            // Transparent full-screen backdrop (allows touch passthrough)
            var bd = GetComponent<Image>();
            if (bd != null)
            {
                bd.color = Color.clear;
                bd.raycastTarget = false;
            }

            HudGroup = gameObject.AddComponent<CanvasGroup>();

            // 1. TOP BAR CONTAINER (Responsive transparent panel)
            _topBar = new GameObject("TopBar", typeof(RectTransform));
            _topBar.transform.SetParent(transform, false);
            GameUIResources.SetAnchors(_topBar.GetComponent<RectTransform>(), 0f, 0.90f, 1f, 0.98f);

            // Left Level Pill
            var lvlPill = GameUIResources.CreateCard("LevelPill", _topBar.transform);
            GameUIResources.SetAnchors(lvlPill.GetComponent<RectTransform>(), 0.04f, 0.15f, 0.32f, 0.85f);
            var lvlTextGo = GameUIResources.CreateText("Level 1", lvlPill.transform, 14, TextAnchor.MiddleCenter, GameUIResources.TextColor);
            lvlTextGo.name = "LevelText";
            LevelText = lvlTextGo.GetComponent<Text>();
            LevelText.fontStyle = FontStyle.Bold;

            // Middle Moves/Timer Pill
            var movesPill = GameUIResources.CreateCard("MovesPill", _topBar.transform);
            GameUIResources.SetAnchors(movesPill.GetComponent<RectTransform>(), 0.35f, 0.15f, 0.58f, 0.85f);
            
            var movesTextGo = GameUIResources.CreateText("Moves: 0", movesPill.transform, 13, TextAnchor.MiddleCenter, GameUIResources.TextColor);
            movesTextGo.name = "MovesText";
            MovesText = movesTextGo.GetComponent<Text>();
            MovesText.fontStyle = FontStyle.Bold;

            var timerTextGo = GameUIResources.CreateText("", movesPill.transform, 13, TextAnchor.MiddleCenter, GameUIResources.WarningColor);
            timerTextGo.name = "TimerText";
            TimerText = timerTextGo.GetComponent<Text>();
            TimerText.fontStyle = FontStyle.Bold;
            timerTextGo.SetActive(false); // Hide timer by default unless challenge mode is active

            // Right Coins Pill
            var coinsPill = GameUIResources.CreateCard("CoinsPill", _topBar.transform);
            GameUIResources.SetAnchors(coinsPill.GetComponent<RectTransform>(), 0.61f, 0.15f, 0.82f, 0.85f);
            var coinTextGo = GameUIResources.CreateText("🪙 0", coinsPill.transform, 14, TextAnchor.MiddleCenter, GameUIResources.TextColor);
            coinTextGo.name = "CoinsText";
            CoinsText = coinTextGo.GetComponent<Text>();
            CoinsText.fontStyle = FontStyle.Bold;

            // Far Right Settings/Pause Button
            _pauseBtn = GameUIResources.CreateIconButton("⚙", _topBar.transform, 36f);
            _pauseBtn.name = "Btn_PAUSE";
            PauseButton = _pauseBtn.GetComponent<Button>();
            GameUIResources.SetAnchors(_pauseBtn.GetComponent<RectTransform>(), 0.85f, 0.15f, 0.96f, 0.85f);
            _pauseBtn.GetComponent<Image>().color = GameUIResources.SurfaceColor;
            _pauseBtn.GetComponentInChildren<Text>().color = GameUIResources.TextColor;
            _pauseBtn.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.08f);
            _pauseBtn.GetComponent<Shadow>().effectDistance = new Vector2(0f, -2f);

            // 2. INSTRUCTION LABEL (Directly under top bar)
            var instructionGo = GameUIResources.CreateText("Her çubuğu tek renk yap", transform, 14, TextAnchor.MiddleCenter, GameUIResources.TextColor);
            instructionGo.name = "InstructionText";
            instructionGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(instructionGo.GetComponent<RectTransform>(), 0.05f, 0.82f, 0.95f, 0.88f);

            // 3. BOTTOM ACTION BAR (Transparent panel)
            var botBar = new GameObject("ActionBar", typeof(RectTransform));
            botBar.transform.SetParent(transform, false);
            GameUIResources.SetAnchors(botBar.GetComponent<RectTransform>(), 0f, 0.05f, 1f, 0.18f);

            // Action Buttons
            _undoBtn = CreateCircularActionButton(botBar.transform, "Btn_UNDO", "↩", "Geri Al", 48f, new Vector2(0.22f, 0.5f));
            UndoButton = _undoBtn.GetComponent<Button>();

            _hintBtn = CreateCircularActionButton(botBar.transform, "Btn_HINT", "💡", "İpucu", 48f, new Vector2(0.50f, 0.5f));
            HintButton = _hintBtn.GetComponent<Button>();

            _restartBtn = CreateCircularActionButton(botBar.transform, "Btn_RESTART", "↻", "Yeniden", 48f, new Vector2(0.78f, 0.5f));
            RestartButton = _restartBtn.GetComponent<Button>();
        }

        private GameObject CreateCircularActionButton(Transform parent, string name, string icon, string labelText, float size, Vector2 anchor)
        {
            var container = new GameObject(name, typeof(RectTransform));
            container.transform.SetParent(parent, false);
            var contRect = container.GetComponent<RectTransform>();
            contRect.anchorMin = anchor;
            contRect.anchorMax = anchor;
            contRect.anchoredPosition = Vector2.zero;
            contRect.sizeDelta = new Vector2(size + 24f, size + 36f);

            var btnGo = GameUIResources.CreateIconButton(icon, container.transform, size);
            btnGo.name = name + "_Button";
            var btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 1f);
            btnRect.anchorMax = new Vector2(0.5f, 1f);
            btnRect.anchoredPosition = new Vector2(0f, -size / 2f);
            btnRect.sizeDelta = new Vector2(size, size);

            btnGo.GetComponent<Image>().color = GameUIResources.SurfaceColor;
            btnGo.GetComponentInChildren<Text>().color = GameUIResources.TextColor;
            btnGo.GetComponentInChildren<Text>().fontSize = Mathf.RoundToInt(size * 0.45f);
            btnGo.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.08f);
            btnGo.GetComponent<Shadow>().effectDistance = new Vector2(0f, -2f);

            var lblGo = GameUIResources.CreateText(labelText, container.transform, 11, TextAnchor.UpperCenter, GameUIResources.TextColor);
            lblGo.name = "Label";
            lblGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
            var lblRect = lblGo.GetComponent<RectTransform>();
            lblRect.anchorMin = new Vector2(0f, 0f);
            lblRect.anchorMax = new Vector2(1f, 0.28f);
            lblRect.offsetMin = Vector2.zero;
            lblRect.offsetMax = Vector2.zero;

            return btnGo;
        }

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
            if (CoinsText != null) CoinsText.text = $"🪙 {coins:N0}";
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
                var text = _undoBtn.transform.parent.Find("Label")?.GetComponent<Text>();
                if (text != null) text.text = loc.GetString("game_undo", "Undo");
            }
            if (_restartBtn != null && _restartBtn.transform.parent != null)
            {
                var text = _restartBtn.transform.parent.Find("Label")?.GetComponent<Text>();
                if (text != null) text.text = loc.GetString("game_restart", "Restart");
            }
            if (_hintBtn != null && _hintBtn.transform.parent != null)
            {
                var text = _hintBtn.transform.parent.Find("Label")?.GetComponent<Text>();
                if (text != null) text.text = loc.GetString("game_hint", "Hint");
            }

            var instr = transform.Find("InstructionText")?.GetComponent<Text>();
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
            }

            var texts = GetComponentsInChildren<Text>(true);
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
