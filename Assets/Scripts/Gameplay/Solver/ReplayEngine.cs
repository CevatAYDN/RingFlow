using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// GDD §12 — Replay Engine.
    ///
    /// Captures a sequence of MoveRecords during gameplay and can replay them
    /// deterministically by re-executing the recorded commands against a fresh
    /// BoardState. Replays use 0-GC BoardState (solver struct) to stay
    /// independent of Unity objects and fully testable.
    ///
    /// Usage:
    ///   var engine = new ReplayEngine();
    ///   engine.StartCapture(levelSeed, initialBoard);
    ///   // ... play moves, engine.RecordMove(record) for each move ...
    ///   var session = engine.StopCapture();
    ///   var result = engine.Replay(session);
    /// </summary>
    public sealed class ReplayEngine
    {
        public struct ReplaySession
        {
            public int LevelIndex;
            public int LevelSeed;
            public List<MoveRecord> Moves;
            public int Version;
        }

        public struct ReplayResult
        {
            public bool IsValid;
            public int ReplayedMoves;
            public int DeterminismFailures;
            public BoardState FinalBoard;
        }

        private ReplaySession _captured;
        private bool _isCapturing;

        public bool IsCapturing => _isCapturing;

        public void StartCapture(int levelIndex, int seed, BoardState initialState)
        {
            _captured = new ReplaySession
            {
                LevelIndex = levelIndex,
                LevelSeed = seed,
                Moves = new List<MoveRecord>(128),
                Version = 1
            };
            _isCapturing = true;
        }

        public void RecordMove(MoveRecord record)
        {
            if (!_isCapturing) return;
            var clone = MoveRecordPool.Rent();
            CopyRecord(record, clone);
            _captured.Moves.Add(clone);
        }

        public ReplaySession StopCapture()
        {
            _isCapturing = false;
            return _captured;
        }

        public ReplayResult Replay(ReplaySession session)
        {
            // Rebuild initial board from seed using LevelGenerator
            var db = GameConfigDatabaseSO.Instance;
            int colorCount = db.GetColorCountForLevel(session.LevelIndex);
            int poleCount = db.GetPoleCountForLevel(session.LevelIndex);
            int maxCapacity = db.GetMaxCapacityForLevel(session.LevelIndex);
            if (poleCount < colorCount + 1) poleCount = colorCount + 1;
            if (poleCount > 12) poleCount = 12;

            var levelData = LevelGenerator.GenerateLevel(
                session.LevelIndex, session.LevelSeed, poleCount, colorCount, maxCapacity);

            if (levelData == null)
                return new ReplayResult { IsValid = false };

            var board = new BoardState();
            board.Initialize(poleCount, maxCapacity, levelData.Poles.Count);
            for (int p = 0; p < levelData.Poles.Count; p++)
            {
                var poleData = levelData.Poles[p];
                board.SetPoleLocked(p, poleData.IsLocked);
                for (int r = 0; r < poleData.Rings.Count; r++)
                {
                    board.SetRingColor(p, r, poleData.Rings[r].Color);
                    board.SetRingType(p, r, poleData.Rings[r].Type);
                    board.SetRingAdditional(p, r, poleData.Rings[r].AdditionalData);
                }
                board.SetRingCount(p, poleData.Rings.Count);
            }

            int failures = 0;
            for (int i = 0; i < session.Moves.Count; i++)
            {
                var record = session.Moves[i];
                if (!board.CanPopRing(record.FromPoleId))
                {
                    failures++;
                    NexusLog.Warn("ReplayEngine", nameof(Replay), i.ToString(),
                        $"Determinism failure at move {i}: pole {record.FromPoleId} cannot pop.");
                    continue;
                }

                var ring = board.PopRing(record.FromPoleId);
                if (ring.Color != record.Ring.Color || ring.Type != record.Ring.Type)
                {
                    failures++;
                    NexusLog.Warn("ReplayEngine", nameof(Replay), i.ToString(),
                        $"Determinism failure at move {i}: ring mismatch. Expected {record.Ring.Color}/{record.Ring.Type}, got {ring.Color}/{ring.Type}.");
                }

                board.AddRing(record.ToPoleId, ring);
            }

            return new ReplayResult
            {
                IsValid = failures == 0,
                ReplayedMoves = session.Moves.Count,
                DeterminismFailures = failures,
                FinalBoard = board
            };
        }

        private static void CopyRecord(MoveRecord from, MoveRecord to)
        {
            to.FromPoleId = from.FromPoleId;
            to.ToPoleId = from.ToPoleId;
            to.Ring = from.Ring;
            to.WasMysteryRevealedOnFrom = from.WasMysteryRevealedOnFrom;
            to.WasTargetPoleUnlocked = from.WasTargetPoleUnlocked;
            to.WasPainted = from.WasPainted;
            to.PaintedRingIndex = from.PaintedRingIndex;
            to.PaintedRingOriginalColor = from.PaintedRingOriginalColor;
            to.OriginalColor = from.OriginalColor;
            to.WasRainbowTargetConverted = from.WasRainbowTargetConverted;
            to.RainbowTargetRingIndex = from.RainbowTargetRingIndex;
            to.RainbowTargetOriginalColor = from.RainbowTargetOriginalColor;

            to.IceBrokenRingIndices.Clear();
            to.IceBrokenRingIndices.AddRange(from.IceBrokenRingIndices);

            to.BombCountersBeforeTick.Clear();
            to.BombCountersBeforeTick.AddRange(from.BombCountersBeforeTick);

            to.BombExplodedRings.Clear();
            to.BombExplodedRings.AddRange(from.BombExplodedRings);

            for (int i = 0; i < from.SubMoves.Count; i++)
            {
                var sub = MoveRecordPool.Rent();
                CopyRecord(from.SubMoves[i], sub);
                to.SubMoves.Add(sub);
            }
        }
    }
}
