using UnityEngine;
using RingFlow.Gameplay;

namespace RingFlow.Editor
{
    /// <summary>
    /// Editor-only metadata attached to VisualBuilder preview objects.
    /// Keeps scene preview/solver inspection lossless without parsing fragile names.
    /// </summary>
    public sealed class EditorPoleMetadata : MonoBehaviour
    {
        public int PoleId;
        public int Capacity;
        public bool IsLocked;
        public int PortalTargetId = -1;
    }

    public sealed class EditorRingMetadata : MonoBehaviour
    {
        public int RingIndex;
        public RingColor Color;
        public RingType Type;
        public int AdditionalData;
    }
}
