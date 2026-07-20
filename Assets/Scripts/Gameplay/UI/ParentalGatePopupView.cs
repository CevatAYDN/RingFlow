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
        public Button AcceptButton;
        public Button TermsButton;
        public Button PrivacyButton;

        public TextMeshProUGUI TitleText;
        public InputField AgeInputField; // Deprecated: old math gate input. Hidden in consent-only mode.
        public TextMeshProUGUI QuestionText; // Used as consent/body text in the current flow.
        public TextMeshProUGUI ErrorText;

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

            BindReferencesFromChildren();
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
        /// this method is kept for interface compliance (Editor UI Studio tooling).
        /// </summary>
        public void BuildUI() { }

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
                GameUIResources.AddButtonEffects(btn);
                if (btn.name.ToUpper().Contains("ACCEPT")) AcceptButton = btn;
                else if (btn.name.ToUpper().Contains("TERMS")) TermsButton = btn;
                else if (btn.name.ToUpper().Contains("PRIVACY")) PrivacyButton = btn;
            }

            AgeInputField = GetComponentInChildren<InputField>(true);

            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in texts)
            {
                if (txt.name.ToUpper().Contains("TITLE")) TitleText = txt;
                else if (txt.name.ToUpper().Contains("QUESTION")) QuestionText = txt;
                else if (txt.name.ToUpper().Contains("ERROR")) ErrorText = txt;
            }
        }
    }
}
