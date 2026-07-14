namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Move strategy for Chain rings. Chain partner following is handled
    /// inline in MoveRingCommand.ApplyChainSubMove. This strategy provides
    /// the registration hook for future migration.
    /// </summary>
    public sealed class ChainMoveStrategy : IRingMoveStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Chain;

        public bool PreMoveValidation(ref MoveContext context) => true;

        public void PostMoveExecution(ref MoveContext context)
        {
            // Chain partner follow is handled by MoveRingCommand
            // ApplyChainSubMove during ExecuteSubMoves.
        }
    }
}
