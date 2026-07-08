using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class InitLevelCommand : ICommand<InitLevelSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private IProgressionService _progressionService;
        [Inject] private ISignalBus _signalBus;

        public void Execute(InitLevelSignal signal)
        {
            if (_model == null)
            {
                NexusLog.Error("InitLevelCommand", "Execute", signal.LevelIndex.ToString(),
                    "GameplayModel is null — cannot initialize level.");
                return;
            }

            _model.Reset();

            int currentLevel = signal.LevelIndex > 0
                ? signal.LevelIndex
                : _progressionService?.CurrentLevel.Value ?? 1;

            int poleCount = DifficultyCurve.PoleCountForLevel(currentLevel);
            int colorCount = DifficultyCurve.ColorCountForLevel(currentLevel);
            int maxCapacity = DifficultyCurve.MaxCapacityForLevel(currentLevel);

            if (poleCount < colorCount + 1) poleCount = colorCount + 1;
            if (poleCount > 12) poleCount = 12;

            var levelData = LevelGenerator.GenerateLevel(
                currentLevel, currentLevel * 12345, poleCount, colorCount, maxCapacity);

            if (levelData != null)
            {
                for (int i = 0; i < levelData.Poles.Count; i++)
                {
                    var pData = levelData.Poles[i];
                    var poleState = new PoleState
                    {
                        Id = i,
                        MaxCapacity = pData.MaxCapacity,
                        IsLocked = pData.IsLocked
                    };

                    for (int r = 0; r < pData.Rings.Count; r++)
                    {
                        poleState.AddRing(pData.Rings[r]);
                    }

                    _model.Poles.Add(poleState);
                }

                _model.TargetMovesCount.Value = levelData.TargetMoves;
            }
            else
            {
                var p0 = new PoleState { Id = 0 };
                p0.AddRing(new RingData(RingColor.Red));
                p0.AddRing(new RingData(RingColor.Blue));
                var p1 = new PoleState { Id = 1 };
                p1.AddRing(new RingData(RingColor.Blue));
                p1.AddRing(new RingData(RingColor.Red));
                var p2 = new PoleState { Id = 2 };

                _model.Poles.Add(p0);
                _model.Poles.Add(p1);
                _model.Poles.Add(p2);
                _model.TargetMovesCount.Value = 2;
            }

            NexusLog.Info("InitLevelCommand", "Execute", currentLevel.ToString(),
                $"Initialized level {currentLevel} with {_model.Poles.Count} poles. Target moves: {_model.TargetMovesCount.Value}.");

            int worldIndex = WorldConfigSO.WorldFromAbsoluteLevel(currentLevel);
            AnalyticsEvents.LevelStart(currentLevel, worldIndex);

            // Fire AFTER the model is populated so subscribers (e.g. BoardMediator)
            // see a consistent state. This avoids the subscriber-before-command
            // ordering problem in Nexus's SignalBus.
            _signalBus?.Fire(new LevelLoadedSignal(currentLevel));
        }
    }

    public class SelectPoleCommand : ICommand<SelectPoleSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private ISignalBus _signalBus;

        public void Execute(SelectPoleSignal signal)
        {
            NexusLog.Info("SelectPoleCommand", "Execute", signal.PoleId.ToString(), $"Start. Selected={_model.SelectedPoleId.Value}, Won={_model.IsGameWon.Value}, Poles={_model.Poles.Count}");

            if (_model.IsGameWon.Value) return;

            int currentSelected = _model.SelectedPoleId.Value;

            if (currentSelected == -1)
            {
                // Seçilen direkten halka alınabiliyorsa seçimi aktifleştir
                var pole = _model.Poles.Find(p => p.Id == signal.PoleId);
                if (pole != null && pole.CanPopRing())
                {
                    _model.SelectedPoleId.Value = signal.PoleId;

                    // GDD §4 — Ghost: Seçildiği an görünür hale gelir (sabitlenir)
                    if (pole.TopRing.Type == RingType.Ghost)
                    {
                        var ghostCopy = pole.Rings[^1];
                        ghostCopy.Type = RingType.Standard;
                        pole.Rings[^1] = ghostCopy;
                    }
                }
            }
            else
            {
                if (currentSelected == signal.PoleId)
                {
                    // Aynı direğe tekrar basıldıysa seçimi iptal et
                    NexusLog.Info("SelectPoleCommand", "Execute", signal.PoleId.ToString(), "Deselecting same pole.");
                    _model.SelectedPoleId.Value = -1;
                }
                else
                {
                    int fromId = currentSelected;
                    int toId = signal.PoleId;
                    NexusLog.Info("SelectPoleCommand", "Execute", signal.PoleId.ToString(), $"Moving from {fromId} to {toId}.");
                    _model.SelectedPoleId.Value = -1;
                    _signalBus.Fire(new MoveRingSignal(fromId, toId));
                }
            }
        }
    }

    public class MoveRingCommand : ICommand<MoveRingSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private ISignalBus _signalBus;

        public void Execute(MoveRingSignal signal)
        {
            var fromPole = _model.Poles.Find(p => p.Id == signal.FromPoleId);
            var toPole = _model.Poles.Find(p => p.Id == signal.ToPoleId);

            if (fromPole == null || toPole == null || !fromPole.CanPopRing())
            {
                NexusLog.Warn("MoveRingCommand", "Execute", $"{signal.FromPoleId}->{signal.ToPoleId}",
                    $"Blocked. fromPoleNull={fromPole == null}, toPoleNull={toPole == null}, canPop={fromPole?.CanPopRing()}");
                return;
            }

            var ring = fromPole.TopRing;

            if (!TryReserveChainCapacity(ref ring, fromPole, toPole)) return;
            if (!toPole.CanAddRing(ring)) return;

            var state = new MoveContext
            {
                Ring = ring,
                OriginalColor = ring.Color,
                FromPole = fromPole,
                ToPole = toPole,
                ToPoleId = signal.ToPoleId,
                FromPoleId = signal.FromPoleId
            };

            ApplyPaintPreMove(ref state);
            ApplyRainbowPreMove(ref state);

            fromPole.PopRing();
            RevealMysteryOnFrom(ref state);

            if (state.ToPole.IsLocked && state.Ring.Type == RingType.Locked)
            {
                state.ToPole.IsLocked = false;
                state.WasTargetPoleUnlocked = true;
                _signalBus.Fire(new UnlockPoleSignal(state.ToPoleId));
            }

            state.ToPole.AddRing(state.Ring);
            TryBreakIceOnTarget(ref state);

            var mainRecord = state.ToRecord();
            mainRecord.SubMoves ??= new List<MoveRecord>();

            ApplyChainSubMove(ref state, mainRecord);
            ApplyMagnetPull(ref state, mainRecord);

            _model.MoveHistory.Push(mainRecord);
            _model.MovesCount.Value++;

            bool bombExploded = TickAllBombs();
            if (bombExploded)
            {
                _model.IsGameWon.Value = false;
            }
            else
            {
                _signalBus.Fire(new CheckWinSignal());
            }
        }

        private bool TryReserveChainCapacity(ref RingData ring, PoleState fromPole, PoleState toPole)
        {
            if (ring.Type != RingType.Chain) return true;

            foreach (var pole in _model.Poles)
            {
                if (pole.Id == fromPole.Id) continue;
                var topR = pole.TopRing;
                if (topR.Type == RingType.Chain && topR.AdditionalData == ring.AdditionalData)
                {
                    if (toPole.Rings.Count + 2 > toPole.MaxCapacity) return false;
                    break;
                }
            }
            return true;
        }

        private void ApplyPaintPreMove(ref MoveContext state)
        {
            if (state.Ring.Type == RingType.Paint)
            {
                if (!state.ToPole.IsEmpty)
                {
                    state.WasPainted = true;
                    state.PaintedRingIndex = state.ToPole.Rings.Count - 1;
                    state.PaintedRingOriginalColor = state.ToPole.Rings[state.PaintedRingIndex].Color;
                    var paintTarget = state.ToPole.Rings[state.PaintedRingIndex];
                    paintTarget.Color = state.Ring.Color;
                    state.ToPole.Rings[state.PaintedRingIndex] = paintTarget;
                    _signalBus.Fire(new PaintRingSignal(state.ToPoleId, state.Ring.Color));
                }
                state.Ring.Type = RingType.Standard;
            }
            else if (!state.ToPole.IsEmpty && state.ToPole.TopRing.Type == RingType.Paint)
            {
                state.WasPainted = true;
                state.Ring.Color = state.ToPole.TopRing.Color;

                state.PaintedRingIndex = state.ToPole.Rings.Count - 1;
                state.PaintedRingOriginalColor = state.ToPole.Rings[state.PaintedRingIndex].Color;
                var paintTarget = state.ToPole.Rings[state.PaintedRingIndex];
                paintTarget.Type = RingType.Standard;
                state.ToPole.Rings[state.PaintedRingIndex] = paintTarget;

                _signalBus.Fire(new PaintRingSignal(state.ToPoleId, state.Ring.Color));
            }
        }

        private void ApplyRainbowPreMove(ref MoveContext state)
        {
            if (state.Ring.Type == RingType.Rainbow)
            {
                if (!state.ToPole.IsEmpty)
                {
                    state.Ring.Color = state.ToPole.TopRing.Color;
                    state.Ring.Type = RingType.Standard;
                }
            }
            else if (!state.ToPole.IsEmpty && state.ToPole.TopRing.Type == RingType.Rainbow)
            {
                var rainbowTarget = state.ToPole.Rings[^1];
                rainbowTarget.Color = state.Ring.Color;
                rainbowTarget.Type = RingType.Standard;
                state.ToPole.Rings[^1] = rainbowTarget;
            }
        }

        private void RevealMysteryOnFrom(ref MoveContext state)
        {
            if (!state.FromPole.IsEmpty && state.FromPole.TopRing.Type == RingType.Mystery)
            {
                state.WasMysteryRevealed = true;
                var mysteryRing = state.FromPole.Rings[^1];
                mysteryRing.Type = RingType.Standard;
                state.FromPole.Rings[^1] = mysteryRing;
                _signalBus.Fire(new RevealMysterySignal(state.FromPoleId, mysteryRing));
            }
        }

        private void TryBreakIceOnTarget(ref MoveContext state)
        {
            if (state.ToPole.Rings.Count < 2) return;

            var belowRing = state.ToPole.Rings[^2];
            if (belowRing.Type == RingType.Frozen && belowRing.Color == state.Ring.Color)
            {
                state.ToPole.Rings[^2] = new RingData(belowRing.Color, RingType.Standard);
                state.WasIceBroken = true;
                _signalBus.Fire(new BreakIceSignal(state.ToPoleId));
            }
        }

        private void ApplyChainSubMove(ref MoveContext state, MoveRecord mainRecord)
        {
            if (state.Ring.Type != RingType.Chain) return;

            foreach (var pole in _model.Poles)
            {
                if (pole.Id == state.FromPole.Id) continue;
                var topR = pole.TopRing;
                if (topR.Type != RingType.Chain || topR.AdditionalData != state.Ring.AdditionalData) continue;

                pole.PopRing();
                state.ToPole.AddRing(topR);
                mainRecord.SubMoves.Add(new MoveRecord(pole.Id, state.ToPole.Id, topR));
                return;
            }
        }

        private void ApplyMagnetPull(ref MoveContext state, MoveRecord mainRecord)
        {
            if (state.Ring.Type != RingType.Magnet) return;

            foreach (var p in _model.Poles)
            {
                if (p.Id == state.ToPole.Id) continue;
                if (state.ToPole.IsFull) break;
                if (!p.CanPopRing() || p.TopRing.Color != state.Ring.Color) continue;

                var pulled = p.PopRing();
                state.ToPole.AddRing(pulled);
                mainRecord.SubMoves.Add(new MoveRecord(p.Id, state.ToPole.Id, pulled));
            }
        }

        private bool TickAllBombs()
        {
            bool exploded = false;
            foreach (var pole in _model.Poles)
            {
                for (int i = 0; i < pole.Rings.Count; i++)
                {
                    var r = pole.Rings[i];
                    if (r.Type != RingType.Bomb) continue;

                    int newCounter = r.AdditionalData - 1;
                    pole.Rings[i] = new RingData(r.Color, RingType.Bomb, newCounter);
                    _signalBus.Fire(new BombTickSignal(pole.Id, newCounter));

                    if (newCounter <= 0)
                    {
                        exploded = true;
                        _signalBus.Fire(new BombExplodedSignal(pole.Id));
                    }
                }
            }
            return exploded;
        }

        private struct MoveContext
        {
            public RingData Ring;
            public RingColor OriginalColor;
            public PoleState FromPole;
            public PoleState ToPole;
            public int FromPoleId;
            public int ToPoleId;
            public bool WasMysteryRevealed;
            public bool WasIceBroken;
            public bool WasTargetPoleUnlocked;
            public bool WasPainted;
            public int PaintedRingIndex;
            public RingColor PaintedRingOriginalColor;

            public MoveRecord ToRecord() => new MoveRecord(
                FromPoleId, ToPoleId, Ring,
                WasMysteryRevealed, WasIceBroken, WasTargetPoleUnlocked, WasPainted,
                PaintedRingIndex, PaintedRingOriginalColor, OriginalColor);
        }
    }

    public class UndoCommand : ICommand<UndoSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private ISignalBus _signalBus;

        public void Execute(UndoSignal signal)
        {
            if (_model.MoveHistory.Count > 0)
            {
                var lastMove = _model.MoveHistory.Pop();
                
                var fromPole = _model.Poles.Find(p => p.Id == lastMove.FromPoleId);
                var toPole = _model.Poles.Find(p => p.Id == lastMove.ToPoleId);

                if (fromPole != null && toPole != null)
                {
                    // 1. Bomba (Bomb) Sayaçlarını Geri Yükle (Artır)
                    foreach (var pole in _model.Poles)
                    {
                        for (int i = 0; i < pole.Rings.Count; i++)
                        {
                            var r = pole.Rings[i];
                            if (r.Type == RingType.Bomb)
                            {
                                int newCounter = r.AdditionalData + 1;
                                pole.Rings[i] = new RingData(r.Color, RingType.Bomb, newCounter);
                                _signalBus.Fire(new BombTickSignal(pole.Id, newCounter));
                            }
                        }
                    }

                    // 2. Alt hamleleri (SubMoves - Mıknatıs ve Zincir) tersten geri al
                    if (lastMove.SubMoves != null)
                    {
                        for (int i = lastMove.SubMoves.Count - 1; i >= 0; i--)
                        {
                            var sub = lastMove.SubMoves[i];
                            var subFrom = _model.Poles.Find(p => p.Id == sub.FromPoleId);
                            var subTo = _model.Poles.Find(p => p.Id == sub.ToPoleId);

                            if (subFrom != null && subTo != null)
                            {
                                subTo.PopRing();
                                subFrom.AddRing(sub.Ring);
                            }
                        }
                    }

                    // 3. Özel durumları geri al
                    if (lastMove.WasTargetPoleUnlocked)
                    {
                        toPole.IsLocked = true;
                    }

                    if (lastMove.WasIceBrokenOnTarget && toPole.Rings.Count >= 2)
                    {
                        var belowRing = toPole.Rings[^2];
                        toPole.Rings[^2] = new RingData(belowRing.Color, RingType.Frozen);
                    }

                    if (lastMove.WasMysteryRevealedOnFrom && !fromPole.IsEmpty)
                    {
                        var topM = fromPole.TopRing;
                        fromPole.Rings[^1] = new RingData(topM.Color, RingType.Mystery);
                    }

                    // Ana halkayı geri taşı
                    var movedRing = toPole.PopRing();

                    // Boyama geri yüklemesi
                    if (lastMove.WasPainted)
                    {
                        movedRing.Color = lastMove.OriginalColor;

                        if (lastMove.PaintedRingIndex >= 0
                            && lastMove.PaintedRingIndex < toPole.Rings.Count)
                        {
                            var painted = toPole.Rings[lastMove.PaintedRingIndex];
                            painted.Color = lastMove.PaintedRingOriginalColor;
                            toPole.Rings[lastMove.PaintedRingIndex] = painted;
                        }
                    }

                    fromPole.AddRing(movedRing);

                    if (_model.MovesCount.Value > 0)
                    {
                        _model.MovesCount.Value--;
                    }

                    _model.SelectedPoleId.Value = -1;
                    _signalBus.Fire(new CheckWinSignal());
                }
            }
        }
    }

    public class CheckWinCommand : ICommand<CheckWinSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private IProgressionService _progressionService;
        [Inject] private IEconomyService _economyService;
        [Inject] private PlayerProgressModel _progress;
        [Inject] private IGameStateMachine _fsm;

        public void Execute(CheckWinSignal signal)
        {
            if (_model.IsGameWon.Value) return;

            bool won = true;
            int nonEmptyPoleCount = 0;

            foreach (var pole in _model.Poles)
            {
                if (pole.IsEmpty) continue;

                nonEmptyPoleCount++;

                // Non-empty poles must be full — a partial stack means rings are still scattered
                if (!pole.IsFull)
                {
                    won = false;
                    break;
                }

                // All rings on a non-empty pole must share the same color (Tower-of-Hanoi-style sort).
                var firstRing = pole.Rings[0];
                for (int i = 1; i < pole.Rings.Count; i++)
                {
                    if (pole.Rings[i].Color != firstRing.Color)
                    {
                        won = false;
                        break;
                    }
                }

                if (!won) break;
            }

            if (won)
            {
                _model.IsGameWon.Value = true;

                int prevLevel = _progressionService.CurrentLevel.Value;
                int prevMoves = _model.MovesCount.Value;
                int prevTarget = _model.TargetMovesCount.Value;

                // 3-star rating (GDD §8): 3★ = optimal, 2★ = +30%, 1★ = completed
                int stars = 1;
                if (prevTarget > 0)
                {
                    if (prevMoves <= prevTarget) stars = 3;
                    else if (prevMoves <= prevTarget * 1.3) stars = 2;
                }

                // Persist progression (advance to next level)
                _progressionService.CompleteCurrentLevel();

                int newLevel = _progressionService.CurrentLevel.Value;

                int newWorldIndex = WorldConfigSO.WorldFromAbsoluteLevel(newLevel);
                if (newWorldIndex >= 0 && newWorldIndex < _progress.UnlockedWorlds.Count)
                {
                    _progress.UnlockedWorlds[newWorldIndex] = true;
                }

                bool isBoss = WorldConfigSO.IsBossLevel(prevLevel);
                int coinReward = isBoss ? 500 : 50 + (prevLevel % 11) * 10;
                _economyService.Earn("Coins", coinReward, isBoss ? "Boss Win Reward" : "Level Win Reward");

                int xpEarned = isBoss ? 50 : 10;
                _progress.Xp.Value += xpEarned;

                int xpRequired = _progress.XpToNextLevel(_progress.PlayerLevel.Value);
                if (_progress.Xp.Value >= xpRequired)
                {
                    _progress.Xp.Value -= xpRequired;
                    _progress.PlayerLevel.Value++;
                    _economyService.Earn("Coins", 100, "Player Level Up Reward");
                }

                _model.LastReward.Value = WinReward.From(prevMoves, prevTarget, coinReward, xpEarned, stars);

                AnalyticsEvents.LevelComplete(prevLevel, prevMoves, stars);

                _ = _fsm?.ChangeStateAsync<WinState>();
            }
        }
    }

    public class UndoRequestedCommand : ICommand<UndoRequestedSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private PlayerProgressModel _progress;
        [Inject] private IEconomyService _economy;
        [Inject] private IAdService _ads;
        [Inject] private ISignalBus _signalBus;
        [Inject] private IProgressionService _progressionService;

        public void Execute(UndoRequestedSignal signal)
        {
            if (_model.MoveHistory.Count == 0) return;

            int level = _progressionService?.CurrentLevel.Value ?? 0;

            if (_progress.FreeUndosUsedThisSession.Value < 5)
            {
                _progress.FreeUndosUsedThisSession.Value++;
                AnalyticsEvents.UndoUse(level, wasFree: true);
                _signalBus.Fire(new UndoSignal());
            }
            else if (_economy.CanAfford("Coins", 5))
            {
                if (_economy.Spend("Coins", 5, "Undo"))
                {
                    AnalyticsEvents.UndoUse(level, wasFree: false);
                    _signalBus.Fire(new UndoSignal());
                }
            }
            else if (_ads != null && _ads.IsRewardedAvailable("Undo"))
            {
                _ads.ShowRewarded("Undo", success =>
                {
                    if (success)
                    {
                        AnalyticsEvents.UndoUse(level, wasFree: false);
                        AnalyticsEvents.RewardedAd("Undo", true);
                        _signalBus.Fire(new UndoSignal());
                    }
                });
            }
        }
    }
}



