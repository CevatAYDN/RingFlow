using System;
using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class InitLevelCommand : ICommand<InitLevelSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private IProgressionService _progressionService;

        public void Execute(InitLevelSignal signal)
        {
            _model.Reset();

            // İlerleme servisinden aktif seviyeyi oku
            int currentLevel = _progressionService.CurrentLevel.Value;

            // Seviyeye göre direk ve renk sayılarını belirle (GDD kurallarına uygun dinamik artış)
            int poleCount = DifficultyCurve.PoleCountForLevel(currentLevel);
            int colorCount = DifficultyCurve.ColorCountForLevel(currentLevel);
            int maxCapacity = DifficultyCurve.MaxCapacityForLevel(currentLevel);

            // Üst sınır limitlerini koru
            if (poleCount < colorCount + 1) poleCount = colorCount + 1; // En az 1 direk boş kalmalı
            if (poleCount > 10) poleCount = 10;

            // Seviyeyi otomatik olarak tohumdan (seed) üret ve çözülebilirliğini doğrula
            var levelData = LevelGenerator.GenerateLevel(currentLevel, seed: currentLevel * 12345, poleCount, colorCount, maxCapacity);

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

                // Solver'ın bulduğu optimal hamle sayısını hedef hamle olarak ata
                _model.TargetMovesCount.Value = levelData.TargetMoves;
            }
            else
            {
                // Fallback (Hata durumunda yedek basit seviye)
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

                // GDD §4 — Paint: Yerleştirildiğinde altındaki ilk halkayı boyar ve standart halkaya dönüşür
                if (ring.Type == RingType.Paint)
                {
                    if (!toPole.IsEmpty)
                    {
                        wasPainted = true;
                        var paintTarget = toPole.Rings[^1];
                        paintTarget.Color = ring.Color;
                        toPole.Rings[^1] = paintTarget;
                        _signalBus.Fire(new PaintRingSignal(signal.ToPoleId, ring.Color));
                    }
                    ring.Type = RingType.Standard;
                }
                // Paint Kontrolü 2 — Paint halka üzerine gelen halkayı boyar (fallback/çift taraflı)
                else if (!toPole.IsEmpty && toPole.TopRing.Type == RingType.Paint)
                {
                    wasPainted = true;
                    ring.Color = toPole.TopRing.Color;
                    _signalBus.Fire(new PaintRingSignal(signal.ToPoleId, ring.Color));
                }

                // GDD §4 — Rainbow: Altına yerleştiği halkanın rengini alır ve sabitlenir
                if (ring.Type == RingType.Rainbow)
                {
                    if (!toPole.IsEmpty)
                    {
                        ring.Color = toPole.TopRing.Color;
                        ring.Type = RingType.Standard;
                    }
                }
                else if (!toPole.IsEmpty && toPole.TopRing.Type == RingType.Rainbow)
                {
                    var rainbowTarget = toPole.Rings[^1];
                    rainbowTarget.Color = ring.Color;
                    rainbowTarget.Type = RingType.Standard;
                    toPole.Rings[^1] = rainbowTarget;
                }

                // Halkayı eski direkten al
                fromPole.PopRing();

                // GDD §4 — Mystery: Üzerindeki tüm halkalar kalkınca (yani yeni üst halka Mystery olunca) açığa çıkar
                if (!fromPole.IsEmpty && fromPole.TopRing.Type == RingType.Mystery)
                {
                    wasMysteryRevealed = true;
                    var mysteryRing = fromPole.Rings[^1];
                    mysteryRing.Type = RingType.Standard;
                    fromPole.Rings[^1] = mysteryRing;
                    _signalBus.Fire(new RevealMysterySignal(signal.FromPoleId, mysteryRing));
                }

                // Kilit açma kontrolü
                if (toPole.IsLocked && ring.Type == RingType.Locked)
                {
                    toPole.IsLocked = false;
                    wasTargetPoleUnlocked = true;
                    _signalBus.Fire(new UnlockPoleSignal(signal.ToPoleId));
                }

                // Halkayı hedef direğe koy
                toPole.AddRing(ring);

                // Buz kırılma kontrolü — hedef direğe aynı renkten bir halka eklendi ise alttaki Frozen kırılır
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
        [Inject] private IProgressionService _progressionService;
        [Inject] private IEconomyService _economyService;
        [Inject] private PlayerProgressModel _progress;

        public void Execute(CheckWinSignal signal)
        {
            if (_model.IsGameWon.Value) return;

            bool won = true;
            int totalRingsCount = 0;

            foreach (var pole in _model.Poles)
            {
                if (pole.IsEmpty) continue;

                totalRingsCount += pole.Rings.Count;

                // Boş olmayan her direk tam kapasite dolu olmalıdır
                if (!pole.IsFull)
                {
                    won = false;
                    break;
                }

                // Direkteki tüm halkalar aynı renkte olmalıdır
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

            if (won)
            {
                _model.IsGameWon.Value = true;

                int prevLevel = _progressionService.CurrentLevel.Value;

                // İlerlemeyi kaydet (Seviyeyi bir artır)
                _progressionService.CompleteCurrentLevel();

                int newLevel = _progressionService.CurrentLevel.Value;

                // Yeni dünya kilidini aç (GDD §5)
                int newWorldIndex = WorldConfigSO.WorldFromAbsoluteLevel(newLevel);
                if (newWorldIndex >= 0 && newWorldIndex < _progress.UnlockedWorlds.Count)
                {
                    _progress.UnlockedWorlds[newWorldIndex] = true;
                }

                // GDD §9 — Coin: Level(50-150), Boss(500)
                bool isBoss = WorldConfigSO.IsBossLevel(prevLevel);
                int coinReward = isBoss ? 500 : 50 + (prevLevel % 11) * 10; // 50..150 deterministic per level

                _economyService.Earn("Coins", coinReward, isBoss ? "Boss Win Reward" : "Level Win Reward");

                // GDD §9 — XP: Seviye tamamlandığında XP ver, Level Up kontrolü yap
                int xpEarned = isBoss ? 50 : 10;
                _progress.Xp.Value += xpEarned;

                int xpRequired = _progress.PlayerLevel.Value * 100;
                if (_progress.Xp.Value >= xpRequired)
                {
                    _progress.Xp.Value -= xpRequired;
                    _progress.PlayerLevel.Value++;
                    // Seviye atlama ödülü: 100 Coin
                    _economyService.Earn("Coins", 100, "Player Level Up Reward");
                }
            }
        }
    }

    public class UndoRequestedCommand : ICommand<UndoRequestedSignal>
    {
        [Inject] private PlayerProgressModel _progress;
        [Inject] private IEconomyService _economy;
        [Inject] private IAdService _ads;
        [Inject] private ISignalBus _signalBus;

        public void Execute(UndoRequestedSignal signal)
        {
            var context = NexusRuntime.CurrentContext;
            var model = context.Resolve<GameplayModel>();
            if (model.MoveHistory.Count == 0) return;

            // İlk 5 geri alma bu oturum için ücretsizdir
            if (_progress.FreeUndosUsedThisSession.Value < 5)
            {
                _progress.FreeUndosUsedThisSession.Value++;
                _signalBus.Fire(new UndoSignal());
            }
            else
            {
                // Sonrası 5 coin/ad
                if (_economy.CanAfford("Coins", 5))
                {
                    if (_economy.Spend("Coins", 5, "Undo"))
                    {
                        _signalBus.Fire(new UndoSignal());
                    }
                }
                else
                {
                    // Reklam gösterimi
                    if (_ads.IsRewardedAvailable("Undo"))
                    {
                        _ads.ShowRewarded("Undo", (success) =>
                        {
                            if (success)
                            {
                                _signalBus.Fire(new UndoSignal());
                            }
                        });
                    }
                }
            }
        }
    }
}



