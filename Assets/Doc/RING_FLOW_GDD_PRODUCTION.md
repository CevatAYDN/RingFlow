# RING FLOW — GDD (Production Ready v1.0)

**Studio:** FeCe Studios  •  **Engine:** Unity 6 LTS  •  **Platform:** Android 10+ / iOS 16+
**Genre:** Hibrit Casual Bulmaca  •  **Orientation:** Portrait 9:16 only
**Sahibi:** Cevat Aydın  •  **Durum:** Üretime Hazır

---

## 1. One-liner
Water Sort'un evrimi. Renkli halkaları dikey direklere yerleştir, her direği tek renge tamamla. Yeni mekanikler + premium görsel kalite + uzun vadeli ilerleme.

## 2. Tasarım Direktifleri (5 kural)
1. **Öğrenmesi kolay, ustalaşması zor** — 5 sn'de anlaşılır
2. **Stressiz** — ceza yok, sınırsız undo (ilk 5 ücretsiz), restart her an
3. **Tatmin edici feedback** — her dokunuş juicy: ses + animasyon + parçacık
4. **Uzun vadeli ilerleme** — 2000 level, 11 özel mekanik, 15 dil
5. **Adil zorluk** — şans yok, her level çözülebilir (QA + solver doğrular)

## 3. Core Gameplay
- **Input:** Yalnızca tap (drag/swipe yok)
- **Pole kapasitesi:** 4 ring (event mod'da 5-6)
- **Hareket:** yalnız en üstteki ring seçilebilir, aynı anda tek ring hareket eder
- **Geçerli hamle:** hedef pole boş VEYA hedef pole dolu değil + üst ring aynı renk
- **Animasyon:** select 0.15s, move 0.30s (lift → arc → drop → bounce)
- **Win:** tüm ringler ayrılmış + her dolu pole tek renk
- **Lose:** yalnız Challenge Mode'da (hamle/süre limiti)

## 4. Özel Halka Tipleri (11 adet)
| # | Tip | İlk Görünme | Kural |
|---|---|---|---|
| 1 | Mystery | W2-L51 | Üzerindeki tüm ringler kaldırılınca rengi açığa çıkar |
| 2 | Frozen | W3-L101 | Buz kırılana kadar taşınamaz |
| 3 | Locked Pole | W4-L151 | Golden Key Ring ile açılır |
| 4 | Stone | W5-L201 | Asla taşınamaz, strateji engeli |
| 5 | Glass | W6-L251 | Şeffaf, görsel yanıltma |
| 6 | Rainbow | W7-L301 | İlk yerleştiği rengi alır, sabitlenir |
| 7 | Bomb | W8-L351 | Sayaçlı (5→0), süre biterse patlar |
| 8 | Chain | W9-L401 | İki ring birlikte hareket eder |
| 9 | Magnet | W10-L451 | 1 pole mesafeden aynı rengi çeker |
| 10 | Paint | W11-L501 | Altındaki ilk ringi boyar, tek kullanımlık |
| 11 | Ghost | W12-L551 | Seçilene kadar görünmez |

**Kural:** Aynı level'da max 4 özel mekanik. Yeni mekanik önce tek başına 20+ level öğretilir.

## 5. World / Level Yapısı
- **40 World × 50 Level = 2000 level** (tam sürüm)
- Her 50 level'da **Boss Level**
- **MVP scope:** ilk 4 World = 200 level + 5 özel mekanik

| World | Tema | Yeni Mekanik | Level |
|---|---|---|---|
| 1 | Grass Valley | — | 1-50 |
| 2 | Sunny Beach | Mystery | 51-100 |
| 3 | Snow Mountain | Frozen | 101-150 |
| 4 | Ancient Temple | Locked Pole | 151-200 |
| 5+ | Çeşitli | 6 yeni mekanik + kombinasyonlar | 201-2000 |

**Difficulty curve:** Tutorial(1-20) → Easy(21-100) → Medium(101-350) → Hard(351-600) → Expert(601-1000) → Master(1001-1500) → Legend(1501-2000)

**Color progression:** 3(1) → 4(20) → 5(80) → 6(150) → 7(300) → 8(500) → 9(800) → 10(1200+)
**Empty pole:** Easy 2 / Medium 1 / Hard 1+kasıt dizilim (asla 0 değil)
**Pole count:** 4(tutorial) → 10(legend)

## 6. Teknik Mimari (Nexus)
```
GameContainer
└─ Nexus Core (ModuleManager, ServiceLocator, EventBus, StateMachine, Lifecycle)
   ├─ Bootstrap → Save → Economy → Level → Gameplay
   ├─ UI → Audio → Ads → IAP → Analytics → LiveOps → Notification
```

- **ScriptableObject her şey** (level, tema, ring tipi, ödül, ses)
- **EventBus:** modüller arası iletişim (loose coupling)
- **Command Pattern:** MoveRing, RevealMystery, BreakIce, UnlockPole, ExplodeBomb (Undo stack)
- **State Machine:** BOOT → MAIN_MENU → LEVEL_SELECT → PLAYING → PAUSED/WIN
- **Addressables:** Core 5MB (start), Tema 10-12MB (lazy), Music streaming
- **Object Pool:** Ring 100, Pole 30, Particle 50, Confetti 30
- **Async/await:** IO ve level yüklemede zorunlu

## 7. Performans Bütçesi (Hedef / Üst Sınır)
| Metrik | Hedef | Üst Sınır |
|---|---|---|
| Frame time | <14ms | 16.67ms (60 FPS) |
| Draw calls | <80 | 120 |
| SetPass calls | <40 | 60 |
| Triangles | <100k | 150k |
| GC alloc/frame | <1KB | 4KB |
| RAM | <150MB | 220MB |
| APK/IPA | <80MB | 120MB |
| İlk açılış | <3.5s | 5s |
| Level yükleme | <0.5s | 1s |

## 8. Seviye Sistemi
- **Format:** JSON (seed + initialState + solution metadata)
- **Generator:** Reverse-from-solution + softlock detection (5 seed retry)
- **Solver:** BFS (≤4p/4c) / IDA* (5-7p/5-6c) / Beam Search (8+p/7+c) + heuristic
- **Heuristic:** Σ(yanlış pozisyondaki ring) + (incomplete pole × 2)
- **Difficulty Score:** `poleCount×2.5 + colorCount×3.0 + minMoves×0.8 + emptyPolePenalty×5.0 + specialCount×4.0 + branchFactor×1.5 − symmetry×2.0`
- **Yıldız:** 3★ = optimal, 2★ = +30%, 1★ = tamamlandı
- **Sandıklar:** Bronze 100 / Silver 250 / Gold 500 / Diamond 1000 XP
- **Kural:** Aynı seed aynı leveli üretmeli (reproducibility)

## 9. Ekonomi
| Kaynak | Elde Etme | Kullanım |
|---|---|---|
| **Coin** | Level(50-150), Boss(500), Daily, Ad, Achievement | Hint(50), Undo(5+), Tema |
| **Diamond** | IAP, Event, Achievement | Premium tema, Remove Ads |
| **XP** | Level completion, Star, Boss | Player Level Up, Sandık progress |

- **Hint:** 50 coin VEYA rewarded ad → bir sonraki doğru hamleyi gösterir (çözümü vermez)
- **Undo:** ilk 5 ücretsiz, sonrası 5 coin/ad
- **Daily:** 100→150→200→Hint→300→Tema→Diamond
- **Remove Ads:** IAP Tier 4
- **Restore Purchases:** zorunlu

## 10. Monetizasyon
- **Soft launch pazarları:** TR, BR, ID
- **AdMob + Unity Ads:**
  - Rewarded: hint, undo, double reward, daily bonus
  - Interstitial: her 3 level
  - Banner: sadece ana menü
- **IAP:** Coin paketleri, Diamond paketleri, Tema paketleri, Remove Ads
- **Hedef metrikler:** D1 retention ≥%45, D7 ≥%15, ARPDAU $0.05-0.10

## 11. UI/UX Özeti
- **Navigation:** Splash → Main Menu → Level Select → Playing → Pause/Win → Reward
- **Onboarding:** 30 sn, sadece yeni cihaz / restore sonrası
- **Localization:** 15 dil (en master), CSV tabanlı, NotoSans font, RTL/LTR otomatik
- **Accessibility:** renk körlüğü 3 mod, büyük buton (min 60dp), reduce motion, slow mode (%50), ekran okuyucu
- **Safe area:** notch + gesture bar desteği, board 280-400dp, pole spacing sabit
- **Empty/Loading/Error:** skeleton + spinner, retry CTA, PII yok

## 12. Audio
- **Müzik:** world başına farklı stil, BPM 70-130, adaptive 4 layer (base/drum/melody/intensity)
- **Mix:** menü %70 / oyun %40 / pause %20 / boss %80
- **SFX:** max 32 voice, priority-based (UI > Gameplay > Ambient)
- **Pitch varyasyon:** aynı ses art arda max 3 (random ±2 yarı ton)
- **Format:** .ogg, 44.1kHz müzik(streaming) / 22.05kHz SFX(preload)

## 13. Veri & Kayıt
- **Local:** JSON dosyası (şifreli, PlayerPrefs değil)
- **Cloud:** Firebase Cloud Save (post-MVP)
- **Kaydedilen:** level, coin, diamond, XP, açılan dünya/tema, günlük ilerleme, başarımlar, ayarlar
- **Analytics events:** level_start, level_complete, hint_use, undo_use, restart_use, rewarded_ad, session_length, retention

## 14. Güvenlik & Uyum
- **Anti-cheat:** local validation + server-side IAP receipt doğrulama
- **KVKK / GDPR / COPPA:** 8+ yaş, parental gate, 15 dilde ToS + Privacy
- **PII maskeleme:** log'larda zorunlu, release'te sadece error
- **Crash reporting:** Firebase Crashlytics

## 15. Release Planı
| Faz | Süre | İçerik |
|---|---|---|
| **Vertical Slice** | 2 hf | 4 pole, 4 renk, tap, undo, win — Antigravity ile kod iskeleti |
| **MVP** | 8 hf | 200 level, 5 mekanik, AdMob, IAP, 5 dil, Closed Beta |
| **Soft Launch** | 4 hf | TR/BR/ID, 500 level, 10 mekanik, full LiveOps |
| **Global Launch** | 4 hf | 2000 level, 40 world, 15 dil, marketing push |

## 16. Açık Sorular (Karar Ver)
1. **Backend:** Firebase mi, kendi sunucu mu? → MVP için Firebase önerilir
2. **Art pipeline:** 2D (Spine/PSD) mi, 3D mi? → 2D önerilir (performans + maliyet)
3. **Ses kaynağı:** Custom composer vs Epidemic Sound lisansı → MVP için lisans
4. **Çocuk güvenliği:** 8+ rating, family policy detayları netleştirilmeli
5. **Multiplayer/Leaderboard:** GDD'de yok, social retention için 2. fazda eklenebilir



**Versiyon:** 1.0  •  **Son güncelleme:** 2026-07-07  •  **Hedef MVP:** 2026-Q4
