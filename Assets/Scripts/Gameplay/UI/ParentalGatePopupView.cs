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
    public class ParentalGatePopupView : View
    {
        public Button AcceptButton;
        public Button TermsButton;
        public Button PrivacyButton;

        public Text TitleText;
        public InputField AgeInputField; // Deprecated: old math gate input. Hidden in consent-only mode.
        public Text QuestionText; // Used as consent/body text in the current flow.
        public Text ErrorText;

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
            ApplyConsentOnlyMode();
            RefreshConsentText();
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

            var titleGo = GameUIResources.CreateText("Parental Verification", card.transform, 34, TextAnchor.MiddleCenter, GameUIResources.TextColor);
            TitleText = titleGo.GetComponent<Text>();
            var titleRt = titleGo.GetComponent<RectTransform>();
            GameUIResources.SetAnchors(titleRt, 0.05f, 0.72f, 0.95f, 0.88f);
            TitleText.fontStyle = FontStyle.Bold;
            titleGo.name = "Title";

            var questionGo = GameUIResources.CreateText("", card.transform, 22, TextAnchor.MiddleCenter, GameUIResources.AccentColor);
            QuestionText = questionGo.GetComponent<Text>();
            QuestionText.fontStyle = FontStyle.Bold;
            QuestionText.name = "Question";
            var questionRt = questionGo.GetComponent<RectTransform>();
            GameUIResources.SetAnchors(questionRt, 0.08f, 0.44f, 0.92f, 0.64f);

            var errorGo = GameUIResources.CreateText("", card.transform, 18, TextAnchor.MiddleCenter, GameUIResources.DangerColor);
            ErrorText = errorGo.GetComponent<Text>();
            ErrorText.name = "Error";
            var errorRt = errorGo.GetComponent<RectTransform>();
            GameUIResources.SetAnchors(errorRt, 0.15f, 0.36f, 0.85f, 0.41f);

            var acceptBtnGo = GameUIResources.CreateButton("ACCEPT & CONTINUE", card.transform, 320, 64);
            AcceptButton = acceptBtnGo.GetComponent<Button>();
            AcceptButton.name = "Accept";
            GameUIResources.SetAnchors(acceptBtnGo.GetComponent<RectTransform>(), 0.20f, 0.22f, 0.80f, 0.34f);

            var termsBtnGo = GameUIResources.CreateButton("Terms of Service", card.transform, 180, 40);
            TermsButton = termsBtnGo.GetComponent<Button>();
            TermsButton.name = "Terms";
            GameUIResources.ApplySecondaryStyle(termsBtnGo);
            var termsText = termsBtnGo.GetComponentInChildren<Text>();
            if (termsText != null) termsText.fontSize = 14;
            GameUIResources.SetAnchors(termsBtnGo.GetComponent<RectTransform>(), 0.08f, 0.10f, 0.48f, 0.18f);

            var privacyBtnGo = GameUIResources.CreateButton("Privacy Policy", card.transform, 180, 40);
            PrivacyButton = privacyBtnGo.GetComponent<Button>();
            PrivacyButton.name = "Privacy";
            GameUIResources.ApplySecondaryStyle(privacyBtnGo);
            var privacyText = privacyBtnGo.GetComponentInChildren<Text>();
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
                if (txt.name.ToUpper().Contains("TITLE")) TitleText = txt;
                else if (txt.name.ToUpper().Contains("QUESTION")) QuestionText = txt;
                else if (txt.name.ToUpper().Contains("ERROR")) ErrorText = txt;
            }
        }
    }
}
