namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Move strategy for Frozen rings. Ice breaking logic is handled
    /// inline in MoveRingCommand.TryBreakIceOnTarget. This strategy provides
    /// the registration hook for future migration.
    /// </summary>
    public sealed class FrozenMoveStrategy : IRingMoveStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Frozen;

        public bool PreMoveValidation(ref MoveContext context) => true;

        public void PostMoveExecution(ref MoveContext context)
        {
            // Ice breaking is handled by MoveRingCommand
            // TryBreakIceOnTarget during ExecuteSubMoves.
        }
    }
}
