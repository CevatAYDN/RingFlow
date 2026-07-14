namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Move strategy for Bomb rings. Bomb ticking and explosion logic is handled
    /// inline in MoveRingCommand.TickAllBombsAndCapture. This strategy provides
    /// the registration hook for future migration of that logic.
    /// </summary>
    public sealed class BombMoveStrategy : IRingMoveStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Bomb;

        public bool PreMoveValidation(ref MoveContext context) => true;

        public void PostMoveExecution(ref MoveContext context)
        {
            // Bomb countdown ticking is handled by MoveRingCommand
            // TickAllBombsAndCapture after the main move completes.
        }
    }
}
