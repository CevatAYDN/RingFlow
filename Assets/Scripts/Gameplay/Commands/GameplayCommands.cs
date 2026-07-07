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

            // Chain (Zincir) halka ise hedef direkte en az 2 boş yer olmalı (eğer bağlı halka başka direkteyse)
            bool isChain = ring.Type == RingType.Chain;
            PoleState chainSourcePole = null;
            RingData chainLinkedRing = default;

            if (isChain)
            {
                // Bağlı diğer halkayı ara
                foreach (var pole in _model.Poles)
                {
                    if (pole.Id == signal.FromPoleId) continue;
                    var topR = pole.TopRing;
                    if (topR.Type == RingType.Chain && topR.AdditionalData == ring.AdditionalData)
                    {
                        chainSourcePole = pole;
                        chainLinkedRing = topR;
                        break;
                    }
                }

                if (chainSourcePole != null)
                {
                    // Eğer bağlı halka başka direkteyse ve hedef direkte 2 halkalık yer yoksa taşıma yapılamaz
                    if (toPole.Rings.Count + 2 > toPole.MaxCapacity) return;
                }
            }

            if (toPole.CanAddRing(ring))
            {
                bool wasMysteryRevealed = false;
                bool wasIceBroken = false;
                bool wasTargetPoleUnlocked = false;
                bool wasPainted = false;
                RingColor originalColor = ring.Color;

                // 1. Boyama (Paint) Kontrolü
                if (!toPole.IsEmpty && toPole.TopRing.Type == RingType.Paint)
                {
                    wasPainted = true;
                    ring.Color = toPole.TopRing.Color;
                    _signalBus.Fire(new PaintRingSignal(signal.ToPoleId, ring.Color));
                }

                // Halkayı eski direkten al
                fromPole.PopRing();

                // Kilit açma kontrolü
                if (toPole.IsLocked && ring.Type == RingType.Locked)
                {
                    toPole.IsLocked = false;
                    wasTargetPoleUnlocked = true;
                    _signalBus.Fire(new UnlockPoleSignal(signal.ToPoleId));
                }

                // Halkayı hedef direğe koy
                toPole.AddRing(ring);

                // Buz kırılma kontrolü
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

                // Gizemli halka açılma kontrolü
                if (!fromPole.IsEmpty && fromPole.TopRing.Type == RingType.Mystery)
                {
                    var topM = fromPole.TopRing;
                    fromPole.Rings[^1] = new RingData(topM.Color, RingType.Standard);
                    wasMysteryRevealed = true;
                    _signalBus.Fire(new RevealMysterySignal(signal.FromPoleId, fromPole.TopRing));
                }

                var mainRecord = new MoveRecord(signal.FromPoleId, signal.ToPoleId, ring,
                    wasMysteryRevealed, wasIceBroken, wasTargetPoleUnlocked, wasPainted, originalColor);

                // 2. Zincirli Halka Otomatik Taşıması
                if (isChain && chainSourcePole != null)
                {
                    chainSourcePole.PopRing();
                    toPole.AddRing(chainLinkedRing);

                    mainRecord.SubMoves ??= new List<MoveRecord>();
                    mainRecord.SubMoves.Add(new MoveRecord(chainSourcePole.Id, toPole.Id, chainLinkedRing));
                }

                // 3. Mıknatıs (Magnet) Çekim Kontrolü
                if (ring.Type == RingType.Magnet)
                {
                    // Diğer direklerin üstündeki aynı renkli halkaları çek
                    foreach (var p in _model.Poles)
                    {
                        if (p.Id == toPole.Id) continue;
                        if (toPole.IsFull) break;

                        if (p.CanPopRing() && p.TopRing.Color == ring.Color)
                        {
                            var pulledRing = p.PopRing();
                            toPole.AddRing(pulledRing);

                            mainRecord.SubMoves ??= new List<MoveRecord>();
                            mainRecord.SubMoves.Add(new MoveRecord(p.Id, toPole.Id, pulledRing));
                        }
                    }
                }

                // Hamleyi kaydet
                _model.MoveHistory.Push(mainRecord);
                _model.MovesCount.Value++;

                // 4. Bomba (Bomb) Sayaçlarını Azaltma
                bool bombExploded = false;
                foreach (var pole in _model.Poles)
                {
                    for (int i = 0; i < pole.Rings.Count; i++)
                    {
                        var r = pole.Rings[i];
                        if (r.Type == RingType.Bomb)
                        {
                            int newCounter = r.AdditionalData - 1;
                            pole.Rings[i] = new RingData(r.Color, RingType.Bomb, newCounter);
                            _signalBus.Fire(new BombTickSignal(pole.Id, newCounter));

                            if (newCounter <= 0)
                            {
                                bombExploded = true;
                                _signalBus.Fire(new BombExplodedSignal(pole.Id));
                            }
                        }
                    }
                }

                if (bombExploded)
                {
                    _model.IsGameWon.Value = false; // Oyunu kaybet
                }
                else
                {
                    _signalBus.Fire(new CheckWinSignal());
                }
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
