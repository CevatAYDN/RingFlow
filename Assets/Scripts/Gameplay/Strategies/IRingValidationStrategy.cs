namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Strategy interface for ring placement and movement validation rules.
    /// Follows Strategy pattern to separate validation logic from PoleState,
    /// enabling Open/Closed Principle compliance for new ring types.
    /// </summary>
    public interface IRingValidationStrategy
    {
        /// <summary>
        /// Determines if this strategy should handle the given ring type.
        /// </summary>
        bool CanHandle(RingType ringType);

        /// <summary>
        /// Validates if a ring can be added to a pole.
        /// </summary>
        bool CanAddRing(RingData ring, RingData topRing, bool isPoleFull, bool isPoleLocked);

        /// <summary>
        /// Validates if a ring can be removed from a pole.
        /// </summary>
        bool CanPopRing(RingData topRing, bool isPoleLocked);
    }

    /// <summary>
    /// Context for validation operations, providing all necessary state
    /// for strategy execution without exposing internal PoleState structure.
    /// </summary>
    public readonly struct ValidationContext
    {
        public readonly RingData RingToValidate;
        public readonly RingData TopRing;
        public readonly bool IsPoleFull;
        public readonly bool IsPoleLocked;
        public readonly bool IsPoleEmpty;

        public ValidationContext(RingData ringToValidate, RingData topRing, bool isPoleFull, bool isPoleLocked, bool isPoleEmpty)
        {
            RingToValidate = ringToValidate;
            TopRing = topRing;
            IsPoleFull = isPoleFull;
            IsPoleLocked = isPoleLocked;
            IsPoleEmpty = isPoleEmpty;
        }
    }
}
