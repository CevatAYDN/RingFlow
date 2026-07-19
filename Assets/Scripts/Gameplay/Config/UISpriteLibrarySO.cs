using UnityEngine;
using System.Collections.Generic;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// GDD §UI — ScriptableObject sprite library.
    /// All UI sprite references are stored here so that artists can swap sprites
    /// by simply reassigning fields in the Unity Inspector — no C# changes required.
    ///
    /// Usage:
    ///   - Create one asset via  Assets → Create → RingFlow → UI Sprite Library
    ///   - Assign it to UIThemeConfigSO.SpriteLibrary
    ///   - Access via GameUIResources.GetSprite(UISpriteKeys.*)
    ///
    /// All sprites are loaded from  Assets/Resources/UI/Sprites/ by default.
    /// The sprite library takes priority over Resources.Load; if a field is null
    /// the system falls back to Resources.Load automatically.
    /// </summary>
    [CreateAssetMenu(fileName = "UISpriteLibrary", menuName = "RingFlow/UI Sprite Library", order = 53)]
    public class UISpriteLibrarySO : ScriptableObject
    {
        // ── Buttons ──────────────────────────────────────────────────────────
        [Header("🔘 Buttons")]
        public Sprite ButtonPrimary;
        public Sprite ButtonPrimaryPressed;
        public Sprite ButtonSecondary;
        public Sprite ButtonDanger;
        public Sprite ButtonSuccess;
        public Sprite ButtonIcon;
        public Sprite ButtonBack;
        public Sprite ButtonClose;

        // ── Panels ───────────────────────────────────────────────────────────
        [Header("🗂 Panels")]
        public Sprite PanelCard;
        public Sprite PanelPopup;
        public Sprite PanelHUD;
        public Sprite PanelTooltip;

        // ── Icons — Currency & Rewards ───────────────────────────────────────
        [Header("💰 Icons — Currency & Rewards")]
        public Sprite IconCoin;
        public Sprite IconGem;
        public Sprite IconXP;
        public Sprite IconStar;
        public Sprite IconStarEmpty;
        public Sprite IconChest;
        public Sprite IconTrophy;
        public Sprite IconDaily;

        // ── Icons — UI Controls ──────────────────────────────────────────────
        [Header("🎮 Icons — UI Controls")]
        public Sprite IconSettings;
        public Sprite IconPause;
        public Sprite IconUndo;
        public Sprite IconHint;
        public Sprite IconPlay;
        public Sprite IconLock;
        public Sprite IconCheck;
        public Sprite IconClose;
        public Sprite IconHome;

        // ── Icons — Audio & Accessibility ────────────────────────────────────
        [Header("🔊 Icons — Audio & Accessibility")]
        public Sprite IconSoundOn;
        public Sprite IconSoundOff;
        public Sprite IconMusicOn;
        public Sprite IconMusicOff;
        public Sprite IconVibration;
        public Sprite IconColorblind;
        public Sprite IconWorld;

        // ── HUD ──────────────────────────────────────────────────────────────
        [Header("🖥 HUD")]
        public Sprite HUDTopBar;
        public Sprite HUDCurrencyPill;
        public Sprite HUDProgressFill;
        public Sprite HUDProgressBackground;

        // ── Backgrounds ──────────────────────────────────────────────────────
        [Header("🌄 Backgrounds")]
        public Sprite BackgroundMain;
        public Sprite BackgroundGameplay;
        public Sprite BackgroundBlurOverlay;

        // ── Rewards ──────────────────────────────────────────────────────────
        [Header("🏆 Rewards")]
        public Sprite RewardStar1;
        public Sprite RewardStar2;
        public Sprite RewardStar3;
        public Sprite RewardChestCommon;
        public Sprite RewardChestRare;
        public Sprite RewardChestEpic;
        public Sprite RewardCoinBurst;

        // ── Badges ───────────────────────────────────────────────────────────
        [Header("🔴 Badges")]
        public Sprite BadgeNotification;
        public Sprite BadgeNew;
        public Sprite BadgeHot;
        public Sprite BadgeSale;

        // ── Runtime lookup ───────────────────────────────────────────────────

        private Dictionary<string, Sprite> _lookup;

        /// <summary>
        /// Returns a sprite by its canonical name key (see UISpriteKeys).
        /// Returns null if the sprite is not assigned; caller falls back to Resources.Load.
        /// </summary>
        public Sprite GetSprite(string key)
        {
            BuildLookupIfNeeded();
            _lookup.TryGetValue(key, out var sprite);
            return sprite;
        }

        private void BuildLookupIfNeeded()
        {
            if (_lookup != null) return;

            _lookup = new Dictionary<string, Sprite>(64)
            {
                // Buttons
                { UISpriteKeys.ButtonPrimary,        ButtonPrimary },
                { UISpriteKeys.ButtonPrimaryPressed,  ButtonPrimaryPressed },
                { UISpriteKeys.ButtonSecondary,       ButtonSecondary },
                { UISpriteKeys.ButtonDanger,          ButtonDanger },
                { UISpriteKeys.ButtonSuccess,         ButtonSuccess },
                { UISpriteKeys.ButtonIcon,            ButtonIcon },
                { UISpriteKeys.ButtonBack,            ButtonBack },
                { UISpriteKeys.ButtonClose,           ButtonClose },

                // Panels
                { UISpriteKeys.PanelCard,             PanelCard },
                { UISpriteKeys.PanelPopup,            PanelPopup },
                { UISpriteKeys.PanelHUD,              PanelHUD },
                { UISpriteKeys.PanelTooltip,          PanelTooltip },

                // Icons — currency
                { UISpriteKeys.IconCoin,              IconCoin },
                { UISpriteKeys.IconGem,               IconGem },
                { UISpriteKeys.IconXP,                IconXP },
                { UISpriteKeys.IconStar,              IconStar },
                { UISpriteKeys.IconStarEmpty,         IconStarEmpty },
                { UISpriteKeys.IconChest,             IconChest },
                { UISpriteKeys.IconTrophy,            IconTrophy },
                { UISpriteKeys.IconDaily,             IconDaily },

                // Icons — controls
                { UISpriteKeys.IconSettings,          IconSettings },
                { UISpriteKeys.IconPause,             IconPause },
                { UISpriteKeys.IconUndo,              IconUndo },
                { UISpriteKeys.IconHint,              IconHint },
                { UISpriteKeys.IconPlay,              IconPlay },
                { UISpriteKeys.IconLock,              IconLock },
                { UISpriteKeys.IconCheck,             IconCheck },
                { UISpriteKeys.IconClose,             IconClose },
                { UISpriteKeys.IconHome,              IconHome },

                // Icons — audio
                { UISpriteKeys.IconSoundOn,           IconSoundOn },
                { UISpriteKeys.IconSoundOff,          IconSoundOff },
                { UISpriteKeys.IconMusicOn,           IconMusicOn },
                { UISpriteKeys.IconMusicOff,          IconMusicOff },
                { UISpriteKeys.IconVibration,         IconVibration },
                { UISpriteKeys.IconColorblind,        IconColorblind },
                { UISpriteKeys.IconWorld,             IconWorld },

                // HUD
                { UISpriteKeys.HUDTopBar,             HUDTopBar },
                { UISpriteKeys.HUDCurrencyPill,       HUDCurrencyPill },
                { UISpriteKeys.HUDProgressFill,       HUDProgressFill },
                { UISpriteKeys.HUDProgressBackground, HUDProgressBackground },

                // Backgrounds
                { UISpriteKeys.BackgroundMain,        BackgroundMain },
                { UISpriteKeys.BackgroundGameplay,    BackgroundGameplay },
                { UISpriteKeys.BackgroundBlurOverlay, BackgroundBlurOverlay },

                // Rewards
                { UISpriteKeys.RewardStar1,           RewardStar1 },
                { UISpriteKeys.RewardStar2,           RewardStar2 },
                { UISpriteKeys.RewardStar3,           RewardStar3 },
                { UISpriteKeys.RewardChestCommon,     RewardChestCommon },
                { UISpriteKeys.RewardChestRare,       RewardChestRare },
                { UISpriteKeys.RewardChestEpic,       RewardChestEpic },
                { UISpriteKeys.RewardCoinBurst,       RewardCoinBurst },

                // Badges
                { UISpriteKeys.BadgeNotification,     BadgeNotification },
                { UISpriteKeys.BadgeNew,              BadgeNew },
                { UISpriteKeys.BadgeHot,              BadgeHot },
                { UISpriteKeys.BadgeSale,             BadgeSale },
            };
        }

        private void OnValidate()
        {
            // Invalidate lookup cache when values change in Inspector
            _lookup = null;
        }
    }
}
