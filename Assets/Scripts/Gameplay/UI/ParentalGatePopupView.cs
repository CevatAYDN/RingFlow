using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// GDD §14 — GDPR / KVKK and COPPA consent popup.
    /// Consent-only flow: the player reviews Terms/Privacy and continues with a localized accept button.
    /// Self-building: when the prefab has no child objects, the entire UI is built
    /// programmatically. When the editor-generated prefab provides children, they are
    /// auto-wired via BindReferencesFromChildren.
    /// </summary>
    [Mediator(typeof(ParentalGatePopupMediator))]
    public class ParentalGatePopupView : View, IAuthoredView
    {
        [SerializeField] private Button _acceptButton;
        public Button AcceptButton => _acceptButton;
        [SerializeField] private Button _termsButton;
        public Button TermsButton => _termsButton;
        [SerializeField] private Button _privacyButton;
        public Button PrivacyButton => _privacyButton;

        [SerializeField] private TextMeshProUGUI _titleText;
        public TextMeshProUGUI TitleText => _titleText;
        [SerializeField] private InputField _ageInputField; // Deprecated: old math gate input. Hidden in consent-only mode.
        public InputField AgeInputField => _ageInputField;
        [SerializeField] private TextMeshProUGUI _questionText; // Used as consent/body text in the current flow.
        public TextMeshProUGUI QuestionText => _questionText;
        [SerializeField] private TextMeshProUGUI _errorText;
        public TextMeshProUGUI ErrorText => _errorText;

        private ILocalizationService _loc;
        private bool _initialized;

        private void Awake()
        {
            EnsureInitialized();
        }

        private bool NeedsSelfBuild()
        {
            if (transform.childCount == 0) return true;
            BindReferencesFromChildren();
            return AcceptButton == null;
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

            if (AcceptButton == null)
                NexusLog.Warn("ParentalGatePopupView", nameof(EnsureInitialized), "",
                    "AcceptButton not found in prefab hierarchy. The popup will be unclosable.");
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureInitialized();
            ApplyConsentOnlyMode();
            RefreshConsentText();
            if (ErrorText != null) ErrorText.text = "";
            if (AgeInputField != null) AgeInputField.text = "";
        }

        /// <summary>
        /// Satisfies IAuthoredView. UI hierarchy is now loaded from prefab;
        /// falls back to programmatic UI building if no children exist (e.g. in unit tests).
        /// </summary>
        public void BuildUI()
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

            var titleGo = GameUIResources.CreateText("Parental Verification", card.transform, 34, TextAnchor.MiddleCenter, GameUIResources.TextColor);
            _titleText = titleGo.GetComponent<TextMeshProUGUI>();
            var titleRt = titleGo.GetComponent<RectTransform>();
            GameUIResources.SetAnchors(titleRt, 0.05f, 0.72f, 0.95f, 0.88f);
            if (_titleText != null) _titleText.fontStyle = FontStyles.Bold;
            titleGo.name = "Title";

            var questionGo = GameUIResources.CreateText("", card.transform, 22, TextAnchor.MiddleCenter, GameUIResources.AccentColor);
            _questionText = questionGo.GetComponent<TextMeshProUGUI>();
            if (_questionText != null) _questionText.fontStyle = FontStyles.Bold;
            questionGo.name = "Question";
            var questionRt = questionGo.GetComponent<RectTransform>();
            GameUIResources.SetAnchors(questionRt, 0.08f, 0.44f, 0.92f, 0.64f);

            var errorGo = GameUIResources.CreateText("", card.transform, 18, TextAnchor.MiddleCenter, GameUIResources.DangerColor);
            _errorText = errorGo.GetComponent<TextMeshProUGUI>();
            errorGo.name = "Error";
            var errorRt = errorGo.GetComponent<RectTransform>();
            GameUIResources.SetAnchors(errorRt, 0.15f, 0.36f, 0.85f, 0.41f);

            var acceptBtnGo = GameUIResources.CreateButton("ACCEPT & CONTINUE", card.transform, 320, 64);
            _acceptButton = acceptBtnGo.GetComponent<Button>();
            _acceptButton.name = "Accept";
            GameUIResources.SetAnchors(acceptBtnGo.GetComponent<RectTransform>(), 0.20f, 0.22f, 0.80f, 0.34f);

            var termsBtnGo = GameUIResources.CreateButton("Terms of Service", card.transform, 180, 40);
            _termsButton = termsBtnGo.GetComponent<Button>();
            _termsButton.name = "Terms";
            GameUIResources.ApplySecondaryStyle(termsBtnGo);
            var termsText = termsBtnGo.GetComponentInChildren<TextMeshProUGUI>();
            if (termsText != null) termsText.fontSize = 14;
            GameUIResources.SetAnchors(termsBtnGo.GetComponent<RectTransform>(), 0.08f, 0.10f, 0.48f, 0.18f);

            var privacyBtnGo = GameUIResources.CreateButton("Privacy Policy", card.transform, 180, 40);
            _privacyButton = privacyBtnGo.GetComponent<Button>();
            _privacyButton.name = "Privacy";
            GameUIResources.ApplySecondaryStyle(privacyBtnGo);
            var privacyText = privacyBtnGo.GetComponentInChildren<TextMeshProUGUI>();
            if (privacyText != null) privacyText.fontSize = 14;
            GameUIResources.SetAnchors(privacyBtnGo.GetComponent<RectTransform>(), 0.52f, 0.10f, 0.92f, 0.18f);
        }

        private void ApplyConsentOnlyMode()
        {
            // Current product decision: no math gate. Small children can play,
            // and legal consent is a simple localized accept flow. Hide legacy
            // authored InputField if an older prefab still contains it.
            if (AgeInputField != null)
            {
                AgeInputField.text = string.Empty;
                AgeInputField.gameObject.SetActive(false);
            }

            if (ErrorText != null)
                ErrorText.text = string.Empty;
        }

        private void RefreshConsentText()
        {
            if (TitleText != null && _loc != null)
                TitleText.text = _loc.GetString("parental_title", TitleText.text);

            if (QuestionText != null)
            {
                QuestionText.text = _loc?.GetString("parental_question",
                    "Please review and accept our Terms of Service and Privacy Policy to continue.")
                    ?? "Please review and accept our Terms of Service and Privacy Policy to continue.";
            }
        }

        public bool ValidateAnswer()
        {
            // Consent-only flow: pressing the localized accept button is enough.
            // No multiplication/captcha gate; younger players must not be blocked
            // by an arithmetic challenge.
            return true;
        }

        public void Localize(ILocalizationService loc)
        {
            if (loc == null) return;
            _loc = loc;
            EnsureInitialized();

            ApplyConsentOnlyMode();
            RefreshConsentText();

            if (AcceptButton != null)
                GameUIResources.LocalizeButtonText(AcceptButton.gameObject, "parental_accept", loc);

            if (TermsButton != null)
                GameUIResources.LocalizeButtonText(TermsButton.gameObject, "parental_terms", loc);

            if (PrivacyButton != null)
                GameUIResources.LocalizeButtonText(PrivacyButton.gameObject, "parental_privacy", loc);

            if (ErrorText != null && !string.IsNullOrEmpty(ErrorText.text))
                ErrorText.text = loc.GetString("parental_error", ErrorText.text);
        }

        private void BindReferencesFromChildren()
        {
            if (_acceptButton == null || _termsButton == null || _privacyButton == null)
            {
                var buttons = GetComponentsInChildren<Button>(true);
                foreach (var btn in buttons)
                {
                    GameUIResources.AddButtonEffects(btn);
                    if (btn.name.ToUpper().Contains("ACCEPT")) _acceptButton = btn;
                    else if (btn.name.ToUpper().Contains("TERMS")) _termsButton = btn;
                    else if (btn.name.ToUpper().Contains("PRIVACY")) _privacyButton = btn;
                }
            }

            if (_ageInputField == null)
                _ageInputField = GetComponentInChildren<InputField>(true);

            if (_titleText == null || _questionText == null || _errorText == null)
            {
                var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var txt in texts)
                {
                    if (txt.name.ToUpper().Contains("TITLE")) _titleText = txt;
                    else if (txt.name.ToUpper().Contains("QUESTION")) _questionText = txt;
                    else if (txt.name.ToUpper().Contains("ERROR")) _errorText = txt;
                }
            }
        }
    }
}
