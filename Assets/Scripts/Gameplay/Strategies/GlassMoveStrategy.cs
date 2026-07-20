namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Move strategy for Glass rings. Glass rings are transparent — they can be
    /// placed on any pole regardless of top color. No special post-move effects.
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
