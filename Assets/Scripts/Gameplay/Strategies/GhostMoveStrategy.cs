namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Move strategy for Ghost rings. Ghost reveal (Ghost -> Standard)
    /// is handled by SelectPoleCommand before the move executes.
    /// This strategy exists for pattern completeness.
    /// </summary>
    public sealed class GhostMoveStrategy : IRingMoveStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Ghost;

        public bool PreMoveValidation(ref MoveContext context) => true;

        public void PostMoveExecution(ref MoveContext context)
        {
            // Ghost rings are converted to Standard by SelectPoleCommand
            // before MoveRingCommand runs. No additional behavior needed.
        }
    }
}
