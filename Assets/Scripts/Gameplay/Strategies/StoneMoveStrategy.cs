namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Move strategy for Stone rings. Stone rings cannot move once placed
    /// (handled by validation). This strategy exists for pattern completeness.
    /// </summary>
    public sealed class StoneMoveStrategy : IRingMoveStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Stone;

        public bool PreMoveValidation(ref MoveContext context) => true;

        public void PostMoveExecution(ref MoveContext context)
        {
            // Stone rings have no post-move special behavior.
        }
    }
}
