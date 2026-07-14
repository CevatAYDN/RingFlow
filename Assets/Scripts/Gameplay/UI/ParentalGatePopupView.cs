using System;
using UnityEngine;
using UnityEngine.UI;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// GDD §14 — GDPR / KVKK and COPPA Parental Gate Popup.
    /// Requires verification (e.g., multiplication check) before accepting terms.
    /// Self-building: when the prefab has no child objects, the entire UI is built
    /// programmatically. When the editor-generated prefab provides children, they are
    /// auto-wired via BindReferencesFromChildren.
    /// </summary>
    [Mediator(typeof(ParentalGatePopupMediator))]
    public class ParentalGatePopupView : View
    {
        public Button AcceptButton;
        public Button TermsButton;
        public Button PrivacyButton;

        public InputField AgeInputField;
        public Text QuestionText;
        public Text ErrorText;

        private int _num1;
        private int _num2;
        private int _correctAnswer;
        private ILocalizationService _loc;
        private bool _initialized;

        private void Awake()
        {
            EnsureInitialized();
        }

        /// <summary>
        /// Idempotently wires view references. Mediators can call this before adding listeners;
        /// this protects authored/inactive prefab flows where mediator binding may happen
        /// before the view Awake path has populated public fields.
        /// </summary>
        public void EnsureInitialized()
        {
            if (_initialized && AcceptButton != null) return;

            if (NeedsSelfBuild())
            {
                BuildUI();
            }
            else
            {
                BindReferencesFromChildren();
            }

            _initialized = true;

            // Validate critical references; self-build guarantees non-null but
            // authored prefabs might have missing children.
            if (AcceptButton == null)
                NexusLog.Warn("ParentalGatePopupView", nameof(EnsureInitialized), "",
                    "AcceptButton not found in prefab hierarchy. The popup will be unclosable.");
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureInitialized();
            GenerateQuestion();
            if (ErrorText != null) ErrorText.text = "";
            if (AgeInputField != null) AgeInputField.text = "";
        }

        private bool NeedsSelfBuild()
        {
            // Self-build if there are no child objects (only components on root)
            // or if the required buttons don't exist after BindReferencesFromChildren.
            if (transform.childCount == 0) return true;

            BindReferencesFromChildren();
            bool missingCritical = AcceptButton == null;
            return missingCritical;
        }

        private void BuildUI()
        {
            // ── Dimmed background overlay ──
            var overlay = GetComponent<Image>();
            if (overlay != null)
            {
                overlay.color = new Color(0, 0, 0, 0.80f);
                overlay.raycastTarget = true;
            }

            // ── Centered panel card ──
            var card = GameUIResources.CreatePanel("Card", transform);
            GameUIResources.SetAnchors(card.GetComponent<RectTransform>(), 0.10f, 0.20f, 0.90f, 0.80f);
            card.GetComponent<Image>().color = GameUIResources.PanelColor;
            card.GetComponent<Image>().raycastTarget = true;

            // ── Title ──
            var titleGo = GameUIResources.CreateText("Parental Verification", card.transform, 36, TextAnchor.MiddleCenter, GameUIResources.TextColor);
            var titleRt = titleGo.GetComponent<RectTransform>();
            GameUIResources.SetAnchors(titleRt, 0.05f, 0.72f, 0.95f, 0.88f);
            titleGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
            titleGo.name = "Title";

            // ── Question text ──
            var questionGo = GameUIResources.CreateText("", card.transform, 28, TextAnchor.MiddleCenter, GameUIResources.AccentColor);
            QuestionText = questionGo.GetComponent<Text>();
            QuestionText.fontStyle = FontStyle.Bold;
            QuestionText.name = "Question";
            var questionRt = questionGo.GetComponent<RectTransform>();
            GameUIResources.SetAnchors(questionRt, 0.05f, 0.56f, 0.95f, 0.68f);

            // ── Age InputField ──
            var inputBgGo = new GameObject("AnswerInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputBgGo.transform.SetParent(card.transform, false);
            var inputBgRt = inputBgGo.GetComponent<RectTransform>();
            GameUIResources.SetAnchors(inputBgRt, 0.28f, 0.44f, 0.72f, 0.53f);
            inputBgGo.GetComponent<Image>().color = GameUIResources.SurfaceColor;

            // Placeholder text
            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeholderGo.transform.SetParent(inputBgGo.transform, false);
            var placeholderRt = placeholderGo.GetComponent<RectTransform>();
            placeholderRt.anchorMin = Vector2.zero;
            placeholderRt.anchorMax = Vector2.one;
            placeholderRt.offsetMin = new Vector2(10, 0);
            placeholderRt.offsetMax = new Vector2(-10, 0);
            var placeholderText = placeholderGo.GetComponent<Text>();
            placeholderText.text = "Enter answer...";
            placeholderText.font = GameUIResources.GetFont();
            placeholderText.fontSize = 22;
            placeholderText.alignment = TextAnchor.MiddleLeft;
            placeholderText.color = GameUIResources.MutedText;
            placeholderText.fontStyle = FontStyle.Italic;

            // Text display
            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(inputBgGo.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10, 0);
            textRt.offsetMax = new Vector2(-10, 0);
            var inputText = textGo.GetComponent<Text>();
            inputText.font = GameUIResources.GetFont();
            inputText.fontSize = 22;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.color = GameUIResources.TextColor;

            var inputField = inputBgGo.GetComponent<InputField>();
            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;
            inputField.characterLimit = 5;
            inputField.contentType = InputField.ContentType.IntegerNumber;
            AgeInputField = inputField;

            // ── Error text ──
            var errorGo = GameUIResources.CreateText("", card.transform, 18, TextAnchor.MiddleCenter, GameUIResources.DangerColor);
            ErrorText = errorGo.GetComponent<Text>();
            ErrorText.name = "Error";
            var errorRt = errorGo.GetComponent<RectTransform>();
            GameUIResources.SetAnchors(errorRt, 0.15f, 0.38f, 0.85f, 0.43f);

            // ── Accept button (primary) ──
            var acceptBtnGo = GameUIResources.CreateButton("ACCEPT & CONTINUE", card.transform, 320, 64);
            AcceptButton = acceptBtnGo.GetComponent<Button>();
            AcceptButton.name = "Accept";
            GameUIResources.SetAnchors(acceptBtnGo.GetComponent<RectTransform>(), 0.20f, 0.24f, 0.80f, 0.34f);

            // ── Terms button (outline) ──
            var termsBtnGo = GameUIResources.CreateButton("Terms of Service", card.transform, 180, 40);
            TermsButton = termsBtnGo.GetComponent<Button>();
            TermsButton.name = "Terms";
            GameUIResources.ApplySecondaryStyle(termsBtnGo);
            var termsText = termsBtnGo.GetComponentInChildren<Text>();
            if (termsText != null) termsText.fontSize = 14;
            GameUIResources.SetAnchors(termsBtnGo.GetComponent<RectTransform>(), 0.08f, 0.10f, 0.48f, 0.18f);

            // ── Privacy button (outline) ──
            var privacyBtnGo = GameUIResources.CreateButton("Privacy Policy", card.transform, 180, 40);
            PrivacyButton = privacyBtnGo.GetComponent<Button>();
            PrivacyButton.name = "Privacy";
            GameUIResources.ApplySecondaryStyle(privacyBtnGo);
            var privacyText = privacyBtnGo.GetComponentInChildren<Text>();
            if (privacyText != null) privacyText.fontSize = 14;
            GameUIResources.SetAnchors(privacyBtnGo.GetComponent<RectTransform>(), 0.52f, 0.10f, 0.92f, 0.18f);
        }

        private void GenerateQuestion()
        {
            var rand = new System.Random();
            _num1 = rand.Next(3, 9);
            _num2 = rand.Next(4, 9);
            _correctAnswer = _num1 * _num2;

            if (QuestionText != null)
            {
                string format = _loc?.GetString("parental_question", "Parental Verification: {0} x {1} = ?")
                    ?? "Parental Verification: {0} x {1} = ?";
                QuestionText.text = string.Format(format, _num1, _num2);
            }
        }

        public bool ValidateAnswer()
        {
            if (AgeInputField == null) return true;

            if (int.TryParse(AgeInputField.text, out int answer))
            {
                if (answer == _correctAnswer)
                {
                    return true;
                }
            }

            if (ErrorText != null)
            {
                ErrorText.text = _loc?.GetString("parental_error", "Incorrect answer. Verification failed.")
                    ?? "Incorrect answer. Verification failed.";
            }
            GenerateQuestion();
            return false;
        }

        public void Localize(ILocalizationService loc)
        {
            if (loc == null) return;
            _loc = loc;
            EnsureInitialized();

            if (QuestionText != null)
                GenerateQuestion();

            if (AcceptButton != null)
                GameUIResources.LocalizeButtonText(AcceptButton.gameObject, "parental_accept", loc);

            if (TermsButton != null)
                GameUIResources.LocalizeButtonText(TermsButton.gameObject, "parental_terms", loc);

            if (PrivacyButton != null)
                GameUIResources.LocalizeButtonText(PrivacyButton.gameObject, "parental_privacy", loc);

            if (ErrorText != null && !string.IsNullOrEmpty(ErrorText.text))
                ErrorText.text = loc.GetString("parental_error", ErrorText.text);

            if (AgeInputField != null && AgeInputField.placeholder is Text ph)
                ph.text = loc.GetString("parental_placeholder", "Enter answer...");
        }

        private void BindReferencesFromChildren()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn.name.ToUpper().Contains("ACCEPT")) AcceptButton = btn;
                else if (btn.name.ToUpper().Contains("TERMS")) TermsButton = btn;
                else if (btn.name.ToUpper().Contains("PRIVACY")) PrivacyButton = btn;
            }

            AgeInputField = GetComponentInChildren<InputField>(true);

            var texts = GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                if (txt.name.ToUpper().Contains("QUESTION")) QuestionText = txt;
                else if (txt.name.ToUpper().Contains("ERROR")) ErrorText = txt;
            }
        }
    }
}
