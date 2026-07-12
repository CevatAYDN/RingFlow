namespace RingFlow.Editor
{
    /// <summary>
    /// Single source of truth for every EditorPrefs key the dashboard reads
    /// or writes. Group by section so duplicates and typos are easy to spot.
    /// </summary>
    internal static class EditorPrefsKeys
    {
        // ── Section foldout state ──
        public const string FoldGenerator    = "RingFlow.Foldout.Generator";
        public const string FoldBuilder      = "RingFlow.Foldout.Builder";
        public const string FoldRuntime      = "RingFlow.Foldout.Runtime";
        public const string FoldSettings     = "RingFlow.Foldout.Settings";
        public const string FoldAdTester     = "RingFlow.Foldout.AdTester";
        public const string FoldDiagnostics  = "RingFlow.Foldout.Diagnostics";
        public const string FoldDatabase     = "RingFlow.Foldout.Database";
        public const string FoldGameFeel     = "RingFlow.Foldout.GameFeel";

        // ── GameConfigDatabaseSO editor section foldout state ──
        public const string FoldDbGeneral     = "RingFlow.Foldout.Db.General";
        public const string FoldDbBands       = "RingFlow.Foldout.Db.Bands";
        public const string FoldDbColorCurve  = "RingFlow.Foldout.Db.ColorCurve";
        public const string FoldDbWorlds      = "RingFlow.Foldout.Db.Worlds";
        public const string FoldDbBalance     = "RingFlow.Foldout.Db.Balance";
        public const string FoldDbLevelGen    = "RingFlow.Foldout.Db.LevelGen";
        public const string FoldDbLookup      = "RingFlow.Foldout.Db.Lookup";

        // ── Generator state ──
        public const string LevelIndex       = "RingFlow.LevelIndex";
        public const string Seed             = "RingFlow.Seed";
        public const string BatchStartLevel  = "RingFlow.BatchStartLevel";
        public const string BatchEndLevel    = "RingFlow.BatchEndLevel";
        public const string AutoSave         = "RingFlow.AutoSave";

        // ── Window state ──
        public const string SelectedTab      = "RingFlow.SelectedTab";

        // ── Ad tester (transient user input) ──
        public const string AdPlacement      = "RingFlow.AdPlacement";
        public const string FoldConfigAssets = "RingFlow.Foldout.ConfigAssets";
    }
}
