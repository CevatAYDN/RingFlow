namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Move strategy for Locked (Key) rings. The pole-unlock logic is handled
    /// inline in MoveRingCommand.ExecuteCoreMove. This strategy provides
    /// the registration hook for future migration.
    /// </summary>
    public sealed class LockedRingMoveStrategy : IRingMoveStrategy
    {
        public bool CanHandle(RingType ringType) => ringType.IsLockedKey();

        public bool PreMoveValidation(ref MoveContext context) => true;

        public void PostMoveExecution(ref MoveContext context)
        {
            // Locked ring pole-unlock is handled by MoveRingCommand
            // ExecuteCoreMove when placed on a locked pole.
        }
    }
}
