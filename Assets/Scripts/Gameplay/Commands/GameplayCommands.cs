using System;
using System.Collections.Generic;
using Nexus.Core;

namespace RingFlow.Gameplay
{
    public class InitLevelCommand : ICommand<InitLevelSignal>
    {
        [Inject] private GameplayModel _model;

        public void Execute(InitLevelSignal signal)
        {
            _model.Reset();

            // Create Pole 0 with Red/Blue/Red/Blue
            var p0 = new PoleState { Id = 0 };
            p0.AddRing(new RingData(RingColor.Red));
            p0.AddRing(new RingData(RingColor.Blue));
            p0.AddRing(new RingData(RingColor.Red));
            p0.AddRing(new RingData(RingColor.Blue));

            // Create Pole 1 with Blue/Red/Blue/Red
            var p1 = new PoleState { Id = 1 };
            p1.AddRing(new RingData(RingColor.Blue));
            p1.AddRing(new RingData(RingColor.Red));
            p1.AddRing(new RingData(RingColor.Blue));
            p1.AddRing(new RingData(RingColor.Red));

            // Create Pole 2 (Empty)
            var p2 = new PoleState { Id = 2 };

            _model.Poles.Add(p0);
            _model.Poles.Add(p1);
            _model.Poles.Add(p2);
        }
    }

    public class SelectPoleCommand : ICommand<SelectPoleSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private ISignalBus _signalBus;

        public void Execute(SelectPoleSignal signal)
        {
            if (_model.IsGameWon.Value) return;

            int currentSelected = _model.SelectedPoleId.Value;

            if (currentSelected == -1)
            {
                // Seçilen direkten halka alınabiliyorsa seçimi aktifleştir
                var pole = _model.Poles.Find(p => p.Id == signal.PoleId);
                if (pole != null && pole.CanPopRing())
                {
                    _model.SelectedPoleId.Value = signal.PoleId;
                }
            }
            else
            {
                if (currentSelected == signal.PoleId)
                {
                    // Aynı direğe tekrar basıldıysa seçimi iptal et
                    _model.SelectedPoleId.Value = -1;
                }
                else
                {
                    int fromId = currentSelected;
                    int toId = signal.PoleId;
                    
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

            if (fromPole == null || toPole == null || !fromPole.CanPopRing()) return;

            var ring = fromPole.TopRing;
            if (toPole.CanAddRing(ring))
            {
                bool wasMysteryRevealed = false;
                bool wasIceBroken = false;
                bool wasTargetPoleUnlocked = false;

                // Halkayı eski direkten al
                fromPole.PopRing();

                // 1. Kilit Açılma Kontrolü: Hedef kilitliyse ve taşınan Altın Anahtar ise
                if (toPole.IsLocked && ring.Type == RingType.Locked)
                {
                    toPole.IsLocked = false;
                    wasTargetPoleUnlocked = true;
                    _signalBus.Fire(new UnlockPoleSignal(signal.ToPoleId));
                }

                // Halkayı hedef direğe yerleştir
                toPole.AddRing(ring);

                // 2. Buz Kırılma Kontrolü: Hedefte yerleşen halkanın hemen altında donmuş bir halka varsa ve renkleri aynıysa
                if (toPole.Rings.Count >= 2)
                {
                    var belowRing = toPole.Rings[^2];
                    if (belowRing.Type == RingType.Frozen && belowRing.Color == ring.Color)
                    {
                        toPole.Rings[^2] = new RingData(belowRing.Color, RingType.Standard);
                        wasIceBroken = true;
                        _signalBus.Fire(new BreakIceSignal(signal.ToPoleId));
                    }
                }

                // 3. Gizem Açılma Kontrolü: Eski direğin en üstünde gizemli halka kaldıysa, onu standart halkaya çevir
                if (!fromPole.IsEmpty && fromPole.TopRing.Type == RingType.Mystery)
                {
                    var topM = fromPole.TopRing;
                    fromPole.Rings[^1] = new RingData(topM.Color, RingType.Standard);
                    wasMysteryRevealed = true;
                    _signalBus.Fire(new RevealMysterySignal(signal.FromPoleId, fromPole.TopRing));
                }

                // Hamle geçmişine tüm özel durum bayraklarıyla kaydet
                _model.MoveHistory.Push(new MoveRecord(signal.FromPoleId, signal.ToPoleId, ring, 
                    wasMysteryRevealed, wasIceBroken, wasTargetPoleUnlocked));

                _model.MovesCount.Value++;
                _signalBus.Fire(new CheckWinSignal());
            }
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

                if (fromPole != null && toPole != null && toPole.TopRing.Color == lastMove.Ring.Color)
                {
                    // 1. Kilit durumunu geri al
                    if (lastMove.WasTargetPoleUnlocked)
                    {
                        toPole.IsLocked = true;
                    }

                    // 2. Kırılan buzu geri dondur
                    if (lastMove.WasIceBrokenOnTarget && toPole.Rings.Count >= 2)
                    {
                        var belowRing = toPole.Rings[^2];
                        toPole.Rings[^2] = new RingData(belowRing.Color, RingType.Frozen);
                    }

                    // 3. Açılan gizemli halkayı geri gizle
                    if (lastMove.WasMysteryRevealedOnFrom && !fromPole.IsEmpty)
                    {
                        var topM = fromPole.TopRing;
                        fromPole.Rings[^1] = new RingData(topM.Color, RingType.Mystery);
                    }

                    // Taşınan halkayı eski yerine koy
                    toPole.PopRing();
                    fromPole.AddRing(lastMove.Ring);

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

        public void Execute(CheckWinSignal signal)
        {
            bool won = true;
            int totalRingsCount = 0;

            foreach (var pole in _model.Poles)
            {
                if (pole.IsEmpty) continue;

                totalRingsCount += pole.Rings.Count;

                // A non-empty pole must be full to be complete (capacity = 4)
                if (!pole.IsFull)
                {
                    won = false;
                    break;
                }

                // All rings in the pole must be of the same color
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

            if (totalRingsCount == 0) won = false;

            _model.IsGameWon.Value = won;
        }
    }
}
