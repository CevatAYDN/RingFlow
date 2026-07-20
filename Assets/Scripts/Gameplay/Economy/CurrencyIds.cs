namespace RingFlow.Gameplay
{
    /// <summary>
    /// Constants for currency identifiers used throughout the economy system.
    /// Centralized so a currency rename requires only one edit.
    /// </summary>
    public static class CurrencyIds
    {
        public const string Coins = "Coins";
        public const string Diamonds = "Diamonds";
        public const string Hint = "Hint";
        public const string Theme = "Theme";

        /// <summary>All currency IDs in iteration order.</summary>
        public static readonly string[] All = { Coins, Diamonds, Hint, Theme };
    }
}
