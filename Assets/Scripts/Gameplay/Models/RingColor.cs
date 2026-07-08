using System;

namespace RingFlow.Gameplay
{
    public enum RingColor
    {
        None = 0,
        Red,
        Blue,
        Green,
        Yellow,
        Orange,
        Purple,
        Cyan,
        Magenta,
        [Obsolete("Use RingType.Key instead")]
        Key = 9,
        [Obsolete("Use RingType.Stone instead")]
        Stone = 10,
        [Obsolete("Use RingType.Rainbow instead")]
        Rainbow = 11
    }
}
