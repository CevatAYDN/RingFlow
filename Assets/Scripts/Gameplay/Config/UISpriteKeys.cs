namespace RingFlow.Gameplay
{
    /// <summary>
    /// GDD §UI — Canonical string keys for all UI sprites.
    /// Use these constants when calling GameUIResources.GetSprite(key)
    /// to avoid magic strings and typos.
    ///
    /// All keys map to the file paths under Assets/Resources/UI/Sprites/
    /// e.g. UISpriteKeys.IconCoin → "Icons/icon_coin"
    /// </summary>
    public static class UISpriteKeys
    {
        // ── Buttons ──────────────────────────────────────────
        public const string ButtonPrimary        = "Buttons/btn_primary";
        public const string ButtonPrimaryPressed = "Buttons/btn_primary_pressed";
        public const string ButtonSecondary      = "Buttons/btn_secondary";
        public const string ButtonDanger         = "Buttons/btn_danger";
        public const string ButtonSuccess        = "Buttons/btn_success";
        public const string ButtonIcon           = "Buttons/btn_icon";
        public const string ButtonBack           = "Buttons/btn_back";
        public const string ButtonClose          = "Buttons/btn_close";

        // ── Panels ───────────────────────────────────────────
        public const string PanelCard            = "Panels/panel_card";
        public const string PanelPopup           = "Panels/panel_popup";
        public const string PanelHUD             = "Panels/panel_hud";
        public const string PanelTooltip         = "Panels/panel_tooltip";

        // ── Icons — Currency & Rewards ───────────────────────
        public const string IconCoin             = "Icons/icon_coin";
        public const string IconGem              = "Icons/icon_gem";
        public const string IconXP               = "Icons/icon_xp";
        public const string IconStar             = "Icons/icon_star";
        public const string IconStarEmpty        = "Icons/icon_star_empty";
        public const string IconChest            = "Icons/icon_chest";
        public const string IconTrophy           = "Icons/icon_trophy";
        public const string IconDaily            = "Icons/icon_daily";

        // ── Icons — UI Controls ──────────────────────────────
        public const string IconSettings         = "Icons/icon_settings";
        public const string IconPause            = "Icons/icon_pause";
        public const string IconUndo             = "Icons/icon_undo";
        public const string IconHint             = "Icons/icon_hint";
        public const string IconPlay             = "Icons/icon_play";
        public const string IconLock             = "Icons/icon_lock";
        public const string IconCheck            = "Icons/icon_check";
        public const string IconClose            = "Icons/icon_close";
        public const string IconHome             = "Icons/icon_home";

        // ── Icons — Audio & Accessibility ────────────────────
        public const string IconSoundOn          = "Icons/icon_sound_on";
        public const string IconSoundOff         = "Icons/icon_sound_off";
        public const string IconMusicOn          = "Icons/icon_music_on";
        public const string IconMusicOff         = "Icons/icon_music_off";
        public const string IconVibration        = "Icons/icon_vibration";
        public const string IconColorblind       = "Icons/icon_colorblind";
        public const string IconWorld            = "Icons/icon_world";

        // ── HUD ──────────────────────────────────────────────
        public const string HUDTopBar            = "HUD/hud_top_bar";
        public const string HUDCurrencyPill      = "HUD/hud_currency_pill";
        public const string HUDProgressFill      = "HUD/hud_progress_fill";
        public const string HUDProgressBackground = "HUD/hud_progress_bg";

        // ── Backgrounds ──────────────────────────────────────
        public const string BackgroundMain       = "Backgrounds/bg_main";
        public const string BackgroundGameplay   = "Backgrounds/bg_gameplay";
        public const string BackgroundBlurOverlay = "Backgrounds/bg_blur_overlay";

        // ── Rewards ──────────────────────────────────────────
        public const string RewardStar1          = "Rewards/reward_star_1";
        public const string RewardStar2          = "Rewards/reward_star_2";
        public const string RewardStar3          = "Rewards/reward_star_3";
        public const string RewardChestCommon    = "Rewards/reward_chest_common";
        public const string RewardChestRare      = "Rewards/reward_chest_rare";
        public const string RewardChestEpic      = "Rewards/reward_chest_epic";
        public const string RewardCoinBurst      = "Rewards/reward_coin_burst";

        // ── Badges ───────────────────────────────────────────
        public const string BadgeNotification    = "Badges/badge_notification";
        public const string BadgeNew             = "Badges/badge_new";
        public const string BadgeHot             = "Badges/badge_hot";
        public const string BadgeSale            = "Badges/badge_sale";
    }
}
