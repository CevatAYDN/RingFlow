using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;

namespace RingFlow.Gameplay
{
    public static class GameplayHelpers
    {
        public static GameObject FindRootGameObject(string name)
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var rootObjects = activeScene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                if (rootObjects[i] != null && rootObjects[i].name == name)
                {
                    return rootObjects[i];
                }
            }
            return null;
        }

        public static PoleState GetPoleById(this List<PoleState> poles, int id)
        {
            if (poles == null) return null;
            if (id >= 0 && id < poles.Count)
            {
                var pole = poles[id];
                if (pole != null && pole.Id == id) return pole;
            }
            for (int i = 0; i < poles.Count; i++)
            {
                if (poles[i] != null && poles[i].Id == id) return poles[i];
            }
            return null;
        }

        public static string DescribeBlockReason(PoleState fromPole, PoleState toPole)
        {
            if (toPole.IsLocked) return "Locked";
            if (toPole.IsFull) return "Pole full";
            if (toPole.IsEmpty) return "Color mismatch";
            if (toPole.TopRing.Type == RingType.Stone) return "Stone blocks";
            return "Color mismatch";
        }

        public static int[] BuildPortalTargets(List<PoleState> poles)
        {
            if (poles == null || poles.Count == 0)
                return System.Array.Empty<int>();

            int poleCount = poles.Count;
            var portalTargets = new int[poleCount];
            for (int i = 0; i < poleCount; i++)
            {
                portalTargets[i] = -1;
            }

            int maxPoleCount = GameplayAssetKeys.Tuning.MaxPoleCount;
            int limit = poleCount < maxPoleCount ? poleCount : maxPoleCount;
            int portalPairCount = 0;

            for (int i = 0; i < limit; i++)
            {
                var pole = poles[i];
                if (pole == null) continue;

                if (pole.Id < 0 || pole.Id >= poleCount)
                {
                    // LOG-4: Out-of-range pole ID is a data error — surface it.
                    NexusLog.Warn("GameplayHelpers", nameof(BuildPortalTargets), pole.Id.ToString(),
                        $"Pole at list index {i} has Id={pole.Id} which is out of range [0,{poleCount}). Skipped.");
                    continue;
                }

                int partnerId = pole.PortalPartnerId;
                if (partnerId < 0 || partnerId >= poleCount || partnerId == pole.Id)
                    continue;

                portalTargets[pole.Id] = partnerId;
                portalPairCount++;
            }

#if DEVELOPMENT_BUILD
            if (portalPairCount > 0)
                NexusLog.Info("GameplayHelpers", nameof(BuildPortalTargets), "",
                    $"Built portal targets for {poleCount} poles: {portalPairCount} portal links registered.");
#endif

            return portalTargets;
        }
    }
}
