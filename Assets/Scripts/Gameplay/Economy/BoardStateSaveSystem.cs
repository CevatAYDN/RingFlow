using System;
using System.Collections.Generic;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Handles saving and loading of current level board state for crash recovery.
    /// Enables players to resume their level progress if the app is killed during gameplay.
    /// </summary>
    public static class BoardStateSaveSystem
    {
        private const string KeyBoardStateVersion = "BoardState_Version";
        private const string KeyCurrentLevelIndex = "BoardState_CurrentLevel";
        private const string KeyMovesCount = "BoardState_MovesCount";
        private const string KeySelectedPoleId = "BoardState_SelectedPole";
        private const string KeyIsGameWon = "BoardState_IsGameWon";
        private const string KeyIsChallengeMode = "BoardState_IsChallenge";
        private const string KeyChallengeMoveLimit = "BoardState_ChallengeMoveLimit";
        private const string KeyChallengeTimeLimit = "BoardState_ChallengeTimeLimit";
        private const string KeyLevelStartUtc = "BoardState_LevelStartUtc";
        private const string KeyHasChallengeFailed = "BoardState_ChallengeFailed";
        private const string KeyPolesData = "BoardState_Poles";
        private const string KeyMoveHistory = "BoardState_MoveHistory";
        
        private const int CurrentVersion = 1;

        /// <summary>
        /// Save the current board state to persistent storage.
        /// Called during autosave triggers: level start, move complete, pause, app suspend, etc.
        /// </summary>
        public static void Save(IPlayerPrefsService prefs, GameplayModel model, int currentLevelIndex)
        {
            if (prefs == null || model == null) return;

            try
            {
                // Save version for migration support
                prefs.SetInt(KeyBoardStateVersion, CurrentVersion);
                
                // Save basic state
                prefs.SetInt(KeyCurrentLevelIndex, currentLevelIndex);
                prefs.SetInt(KeyMovesCount, model.MovesCount.Value);
                prefs.SetInt(KeySelectedPoleId, model.SelectedPoleId.Value);
                prefs.SetBool(KeyIsGameWon, model.IsGameWon.Value);
                prefs.SetBool(KeyIsChallengeMode, model.IsChallengeMode.Value);
                prefs.SetInt(KeyChallengeMoveLimit, model.ChallengeMoveLimit.Value);
                prefs.SetInt(KeyChallengeTimeLimit, model.ChallengeTimeLimitSeconds.Value);
                prefs.SetString(KeyLevelStartUtc, model.LevelStartUtcTicks.Value.ToString());
                prefs.SetBool(KeyHasChallengeFailed, model.HasChallengeFailed.Value);
                
                // Save poles state
                var polesData = SerializePoles(model.Poles);
                prefs.SetString(KeyPolesData, polesData);
                
                // Save move history (for undo functionality)
                var moveHistoryData = SerializeMoveHistory(model.MoveHistory);
                prefs.SetString(KeyMoveHistory, moveHistoryData);
                
                NexusLog.Info("BoardStateSaveSystem", nameof(Save), "", 
                    $"Board state saved for level {currentLevelIndex} with {model.MovesCount.Value} moves");
            }
            catch (Exception ex)
            {
                NexusLog.Error("BoardStateSaveSystem", nameof(Save), "", 
                    $"Failed to save board state: {ex.Message}");
            }
        }

        /// <summary>
        /// Load the saved board state from persistent storage.
        /// Returns true if a valid saved state was found and loaded.
        /// </summary>
        public static bool Load(IPlayerPrefsService prefs, GameplayModel model, out int loadedLevelIndex)
        {
            loadedLevelIndex = -1;
            
            if (prefs == null || model == null) return false;

            try
            {
                // Check if there's a saved state
                var version = prefs.GetInt(KeyBoardStateVersion, -1);
                if (version <= 0)
                {
                    NexusLog.Info("BoardStateSaveSystem", nameof(Load), "", 
                        "No saved board state found");
                    return false;
                }

                // Load basic state
                loadedLevelIndex = prefs.GetInt(KeyCurrentLevelIndex, 1);
                model.MovesCount.Value = prefs.GetInt(KeyMovesCount, 0);
                model.SelectedPoleId.Value = prefs.GetInt(KeySelectedPoleId, -1);
                model.IsGameWon.Value = prefs.GetBool(KeyIsGameWon, false);
                model.IsChallengeMode.Value = prefs.GetBool(KeyIsChallengeMode, false);
                model.ChallengeMoveLimit.Value = prefs.GetInt(KeyChallengeMoveLimit, 0);
                model.ChallengeTimeLimitSeconds.Value = prefs.GetInt(KeyChallengeTimeLimit, 0);
                
                var levelStartUtcStr = prefs.GetString(KeyLevelStartUtc, "0");
                if (long.TryParse(levelStartUtcStr, out long levelStartUtc))
                {
                    model.LevelStartUtcTicks.Value = levelStartUtc;
                }
                
                model.HasChallengeFailed.Value = prefs.GetBool(KeyHasChallengeFailed, false);
                
                // Load poles state
                var polesData = prefs.GetString(KeyPolesData, string.Empty);
                DeserializePoles(polesData, model.Poles);
                
                // Load move history
                var moveHistoryData = prefs.GetString(KeyMoveHistory, string.Empty);
                DeserializeMoveHistory(moveHistoryData, model.MoveHistory);
                
                NexusLog.Info("BoardStateSaveSystem", nameof(Load), "", 
                    $"Board state loaded for level {loadedLevelIndex} with {model.MovesCount.Value} moves");
                
                return true;
            }
            catch (Exception ex)
            {
                NexusLog.Error("BoardStateSaveSystem", nameof(Load), "", 
                    $"Failed to load board state: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clear the saved board state (call when level is completed or player restarts).
        /// </summary>
        public static void Clear(IPlayerPrefsService prefs)
        {
            if (prefs == null) return;

            try
            {
                prefs.DeleteKey(KeyBoardStateVersion);
                prefs.DeleteKey(KeyCurrentLevelIndex);
                prefs.DeleteKey(KeyMovesCount);
                prefs.DeleteKey(KeySelectedPoleId);
                prefs.DeleteKey(KeyIsGameWon);
                prefs.DeleteKey(KeyIsChallengeMode);
                prefs.DeleteKey(KeyChallengeMoveLimit);
                prefs.DeleteKey(KeyChallengeTimeLimit);
                prefs.DeleteKey(KeyLevelStartUtc);
                prefs.DeleteKey(KeyHasChallengeFailed);
                prefs.DeleteKey(KeyPolesData);
                prefs.DeleteKey(KeyMoveHistory);
                
                NexusLog.Info("BoardStateSaveSystem", nameof(Clear), "", 
                    "Board state cleared");
            }
            catch (Exception ex)
            {
                NexusLog.Error("BoardStateSaveSystem", nameof(Clear), "", 
                    $"Failed to clear board state: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if there's a saved board state available.
        /// </summary>
        public static bool HasSavedState(IPlayerPrefsService prefs)
        {
            if (prefs == null) return false;
            try
            {
                return prefs.GetInt(KeyBoardStateVersion, -1) > 0;
            }
            catch
            {
                return false;
            }
        }

        private static string SerializePoles(List<PoleState> poles)
        {
            // Simple serialization format: poleCount|pole1Data|pole2Data|...
            // Each pole: id,capacity,locked,portalId|ring1,ring2,ring3|...
            var result = new System.Text.StringBuilder();
            
            result.Append(poles.Count);
            
            for (int i = 0; i < poles.Count; i++)
            {
                var pole = poles[i];
                result.Append('|');
                result.Append(pole.Id);
                result.Append(',');
                result.Append(pole.RingCapacity);
                result.Append(',');
                result.Append(pole.IsLocked ? "1" : "0");
                result.Append(',');
                result.Append(pole.PortalPartnerId);
                result.Append('|');
                
                // Serialize rings
                for (int j = 0; j < pole.Rings.Count; j++)
                {
                    var ring = pole.Rings[j];
                    result.Append(SerializeRingData(ring));
                    if (j < pole.Rings.Count - 1)
                    {
                        result.Append(';');
                    }
                }
            }
            
            return result.ToString();
        }

        private static void DeserializePoles(string data, List<PoleState> poles)
        {
            poles.Clear();
            
            if (string.IsNullOrEmpty(data)) return;
            
            var parts = data.Split('|');
            if (parts.Length < 1) return;
            
            if (!int.TryParse(parts[0], out int poleCount)) return;
            
            int partIndex = 1;
            for (int i = 0; i < poleCount && partIndex + 1 < parts.Length; i++)
            {
                var poleData = parts[partIndex].Split(',');
                if (poleData.Length < 4) continue;
                
                var pole = new PoleState();
                if (int.TryParse(poleData[0], out int poleId)) pole.Id = poleId;
                if (int.TryParse(poleData[1], out int capacity)) pole.RingCapacity = capacity;
                pole.IsLocked = poleData[2] == "1";
                if (int.TryParse(poleData[3], out int portalId)) pole.PortalPartnerId = portalId;
                
                // Deserialize rings
                var ringsData = parts[partIndex + 1].Split(';');
                for (int j = 0; j < ringsData.Length; j++)
                {
                    if (!string.IsNullOrEmpty(ringsData[j]))
                    {
                        var ring = DeserializeRingData(ringsData[j]);
                        pole.Rings.Add(ring);
                    }
                }
                
                poles.Add(pole);
                partIndex += 2;
            }
        }

        private static string SerializeRingData(RingData ring)
        {
            // Format: color,type,additionalData
            return $"{(int)ring.Color},{(int)ring.Type},{ring.AdditionalData}";
        }

        private static RingData DeserializeRingData(string data)
        {
            var parts = data.Split(',');
            var ring = new RingData();
            
            if (parts.Length > 0 && int.TryParse(parts[0], out int color))
                ring.Color = (RingColor)color;
            if (parts.Length > 1 && int.TryParse(parts[1], out int type))
                ring.Type = (RingType)type;
            if (parts.Length > 2 && int.TryParse(parts[2], out int additionalData))
                ring.AdditionalData = additionalData;
            
            return ring;
        }

        private static string SerializeMoveHistory(UndoStack<MoveRecord> moveHistory)
        {
            // For simplicity, we only save the count of moves, not the full undo history
            // Full undo history would be complex to serialize and may not be necessary for crash recovery
            return moveHistory.Count.ToString();
        }

        private static void DeserializeMoveHistory(string data, UndoStack<MoveRecord> moveHistory)
        {
            // Clear current history - we don't restore the full undo history on crash recovery
            // The player can still continue playing, but undo history is lost
            while (moveHistory.Count > 0)
            {
                MoveRecordPool.Return(moveHistory.Pop());
            }
            
            // This is a trade-off: we prioritize crash recovery over undo preservation
            // In the future, this could be enhanced to serialize/deserialize full move records
        }
    }
}