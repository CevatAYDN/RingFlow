namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// Unified interface for views that can build their own UI hierarchy programmatically,
    /// allowing the editor UI Studio to generate prefabs using the exact same code path
    /// as runtime self-building.
    /// </summary>
    public interface IAuthoredView
    {
        void BuildUI();
    }
}
