namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Move strategy for Magnet rings. Magnet pull behavior is handled
    /// inline in MoveRingCommand.ApplyMagnetPull. This strategy provides
    /// the registration hook for future migration.
    /// </summary>
    public sealed class MagnetMoveStrategy : IRingMoveStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Magnet;

        public bool PreMoveValidation(ref MoveContext context) => true;

        public void PostMoveExecution(ref MoveContext context)
        {
            // Magnet pulling is handled by MoveRingCommand
            // ApplyMagnetPull during ExecuteSubMoves.
        }
    }
}
