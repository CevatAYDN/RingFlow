namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Move strategy for Glass rings. Glass rings follow standard movement rules
    /// with no special post-move behavior. This strategy exists for pattern completeness.
    /// </summary>
    public sealed class GlassMoveStrategy : IRingMoveStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Glass;

        public bool PreMoveValidation(ref MoveContext context) => true;

        public void PostMoveExecution(ref MoveContext context)
        {
            // Glass rings have no special post-move behavior.
        }
    }
}
