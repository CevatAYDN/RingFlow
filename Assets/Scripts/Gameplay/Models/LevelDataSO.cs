using UnityEngine;

namespace RingFlow.Gameplay
{
    [CreateAssetMenu(fileName = "NewLevelData", menuName = "RingFlow/Level Data")]
    public class LevelDataSO : ScriptableObject
    {
        public LevelData Data;
    }
}
