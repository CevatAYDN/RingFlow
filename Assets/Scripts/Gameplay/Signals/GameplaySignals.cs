namespace RingFlow.Gameplay
{
    public readonly struct InitLevelSignal
    {
        public readonly int LevelIndex;
        public InitLevelSignal(int levelIndex) => LevelIndex = levelIndex;
    }

    public readonly struct SelectPoleSignal
    {
        public readonly int PoleId;
        public SelectPoleSignal(int poleId) => PoleId = poleId;
    }

    public readonly struct MoveRingSignal
    {
        public readonly int FromPoleId;
        public readonly int ToPoleId;
        public MoveRingSignal(int fromPoleId, int toPoleId)
        {
            FromPoleId = fromPoleId;
            ToPoleId = toPoleId;
        }
    }

    public readonly struct UndoSignal {}

    public readonly struct CheckWinSignal {}

    public readonly struct RevealMysterySignal
    {
        public readonly int PoleId;
        public readonly RingData RevealedRing;
        public RevealMysterySignal(int poleId, RingData revealedRing)
        {
            PoleId = poleId;
            RevealedRing = revealedRing;
        }
    }

    public readonly struct BreakIceSignal
    {
        public readonly int PoleId;
        public BreakIceSignal(int poleId) => PoleId = poleId;
    }

    public readonly struct UnlockPoleSignal
    {
        public readonly int PoleId;
        public UnlockPoleSignal(int poleId) => PoleId = poleId;
    }

    public readonly struct BombTickSignal
    {
        public readonly int PoleId;
        public readonly int Counter;
        public BombTickSignal(int poleId, int counter)
        {
            PoleId = poleId;
            Counter = counter;
        }
    }

    public readonly struct BombExplodedSignal
    {
        public readonly int PoleId;
        public BombExplodedSignal(int poleId) => PoleId = poleId;
    }

    public readonly struct PaintRingSignal
    {
        public readonly int PoleId;
        public readonly RingColor NewColor;
        public PaintRingSignal(int poleId, RingColor newColor)
        {
            PoleId = poleId;
            NewColor = newColor;
        }
    }
}
