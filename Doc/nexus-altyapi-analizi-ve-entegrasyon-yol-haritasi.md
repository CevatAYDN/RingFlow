# Nexus Altyapı Analizi ve Entegrasyon Yol Haritası

> **Proje:** Nexus Core Platform (NCP)
> **Tarih:** 2026-07-11 (Güncelleme)
> **Hedef:** Tüm hibrit-casual oyun projelerinde standart çekirdek paket olarak kullanılacak,
>           AAA+ kalitesinde, oyun türünden bağımsız, modüler ve ölçeklenebilir Nexus altyapısı
> **Kapsam:** Her tür oyun (bulmaca, hiper-casual, casual, hibrit, simülasyon, strateji, idle, RPG, aksiyon, macera vb.)
> **Platform:** Unity 6 LTS, Android / iOS / WebGL
> **Dil:** Türkçe

---

## İçindekiler

1. [Vizyon ve Kapsam](#1-vizyon-ve-kapsam)
2. [Mevcut Sistem Envanteri](#2-mevcut-sistem-envanteri)
3. [Oyun Türünden Bağımsız Genelleştirme Stratejisi](#3-oyun-türünden-bağımsız-genelleştirme-stratejisi)
4. [Proje Entegrasyon Süreci](#4-proje-entegrasyon-süreci)
5. [Merkezi Altyapı Servisleri](#5-merkezi-altyapı-servisleri)
6. [Sürüm Yönetimi ve Dağıtım Süreci](#6-sürüm-yönetimi-ve-dağıtım-süreci)
7. [Nexus Editör Araçları](#7-nexus-editör-aracı)
8. [AAA+ Kalite Standardı Gereksinimleri](#8-aaa-kalite-standardı-gereksinimleri)
9. [Sürekli Bakım ve İyileştirme Döngüsü](#9-sürekli-bakım-ve-iyileştirme-döngüsü)
10. [Test ve Doğrulama Stratejisi](#10-test-ve-doğrulama-stratejisi)
11. [Uygulama Yol Haritası (Roadmap)](#11-uygulama-yol-haritası-roadmap)

---

## 1. Vizyon ve Kapsam

### 1.1 Platform Vizyonu

Nexus Core Platform, tüm hibrit-casual oyun projelerinde standartlaştırılacak, oyun türünden bağımsız, modüler ve ölçeklenebilir bir Unity çekirdek altyapısıdır.

**Temel prensipler:**

- **Tür bağımsızlığı:** Nexus, bulmaca, hiper-casual, casual, hibrit, simülasyon, strateji, idle, RPG, aksiyon, macera — her tür oyun projesinde kullanılabilir. Paket içinde hiçbir oyun türüne özel bağımlılık bulunmaz.
- **Her projede standart çekirdek:** Her yeni oyun projesi Nexus ile başlar. Mevcut projeler Nexus'a taşınır. Tüm projeler aynı çekirdeği kullanır.
- **Production first:** Nexus, prototip değil, üretim ortamında kanıtlanmış, AAA+ kalite standartlarını karşılayan bir altyapıdır.
- **Geliştirici deneyimi:** Nexus editör araçları, entegrasyonu saniyeler içinde tamamlar, hataları otomatik tespit eder ve geliştiriciyi yönlendirir.
- **Sürekli iyileştirme:** Tüm bağlı projelerden gelen geri bildirimlerle Nexus sürekli evrilir. Her yeni oyun, paketi güçlendirir.

### 1.2 Kapsam

| Boyut | Kapsam |
|-------|--------|
| **Oyun türleri** | Bulmaca, hiper-casual, casual, hibrit, simülasyon, strateji, idle, RPG, aksiyon, macera, kelime, kart, masaüstü, eğitici |
| **Platformlar** | Android, iOS, WebGL (Konsol/PC için genişletilebilir) |
| **Unity sürümü** | Unity 6 LTS (geriye dönük uyumluluk için minimum Unity 2022.3) |
| **Render pipeline** | URP (varsayılan), Built-in (opsiyonel), HDRP (opsiyonel) |
| **Proje büyüklüğü** | 1-100+ kişilik ekipler, küçük prototiplerden büyük AAA+ üretimlere |

---

## 2. Mevcut Sistem Envanteri

### 2.1 Nexus Core — Mevcut Çekirdek Bileşenler

#### Kategori A: Mimari Çekirdek (Architectural Core)

| Sistem | Açıklama | Oyun Türü Bağımlılığı |
|--------|----------|----------------------|
| **DI Container** | Field/property/method/constructor injection, singleton takibi, circular dep tespiti, AOT desteği | Hiçbiri — her oyunda kullanılır |
| **SignalBus** | 0-GC struct sinyaller, 4 çalışma modu, async dispatch, timeout, pooling | Hiçbiri — her oyunda kullanılır |
| **Command Pipeline** | Pool'lanabilir komutlar, ICommand<T> handler'ları, async komut desteği | Hiçbiri — her oyunda kullanılır |
| **FSM** | Async state geçişleri, OnEnterAsync/OnExitAsync/OnTick, ITickable | Hiçbiri — her oyunda kullanılır |
| **Reactive Models** | Sıfır-GC reaktif property'ler, SecureObservableProperty, IReactiveModel | Hiçbiri — her oyunda kullanılır |
| **MVCS Binding** | Mediator<TView>, ViewBinder, View-to-Mediator binding | Hiçbiri — her oyunda kullanılır |
| **Context Sistemi** | Context hiyerarşisi, fluent API binding, ScriptableObject konfigürasyonu | Hiçbiri — her oyunda kullanılır |

#### Kategori B: Altyapı Servisleri (Infrastructure Services)

| Servis | Açıklama | Oyun Türü Bağımlılığı |
|--------|----------|----------------------|
| **AudioService** | BGM/SFX, crossfade, spatial, BgmStateMultiplier, PlayerPrefs kalıcılığı | Hiçbiri — her oyunda kullanılır |
| **ObjectPoolService** | IPoolable yaşam döngüsü, Prewarm/Spawn/Despawn, DespawnAfter | Hiçbiri — her oyunda kullanılır |
| **EncryptedStorageService** | AES-128 + HMAC anti-tamper, device binding, deferred batch save | Hiçbiri — her oyunda kullanılır |
| **LocalizationService** | RTL desteği, dil değiştirme, ILocalizationTableProvider plugin modeli | Hiçbiri — her oyunda kullanılır |
| **HapticService** | 6 preset tip (Light/Medium/Heavy/Warning/Success/Selection) | Hiçbiri — her oyunda kullanılır |
| **FeedbackService** | Audio+Haptic orkestrasyonu, FeedbackPreset enum | Hiçbiri — her oyunda kullanılır |
| **TickService** | Update/FixedUpdate/LateUpdate, TimeScale, IsPaused, snapshot array | Hiçbiri — her oyunda kullanılır |
| **LoggerService** | ILoggerService, seviye bazlı filtreleme | Hiçbiri — her oyunda kullanılır |
| **EconomyService** | Long-tabanlı para birimi, float hassasiyet kaybı yok | Hiçbiri — her oyunda kullanılabilir |
| **IapService** | Taslak — arayüz hazır | Hiçbiri — gerektiğinde kullanılır |
| **AdService** | Taslak — arayüz hazır | Hiçbiri — gerektiğinde kullanılır |
| **AnalyticsService** | Taslak — sadece log yazıyor | Hiçbiri — gerektiğinde kullanılır |
| **ProgressionService** | Taslak — arayüz hazır | Hiçbiri — gerektiğinde kullanılır |
| **WindowManager** | IUIAssetProvider ile pluggable asset yükleme | Hiçbiri — her oyunda kullanılır |

#### Kategori C: Gelişmiş Altyapı (Advanced Infrastructure)

| Sistem | Açıklama | Oyun Türü Bağımlılığı |
|--------|----------|----------------------|
| **HybridQueue** | Thread-safe + NextFrame kuyruk, 0-GC QueuedSignalPool | Hiçbiri — her oyunda kullanılır |
| **Recovery** | IRecoveryStrategy, Skip/Retry/Abort/Fallback kararları | Hiçbiri — her oyunda kullanılır |
| **NetworkMonitor** | Bağlantı durumu izleme, INetworkSignal | Hiçbiri — gerektiğinde kullanılır |
| **DOTS Bridge** | Opsiyonel ECS/DOTS entegrasyonu (UNITY_COLLECTIONS koşullu) | Hiçbiri — opsiyonel |
| **CausalTracing** | Ring buffer ile production sinyal akış takibi | Hiçbiri — her oyunda kullanılır |
| **DebugHUD** | Runtime geliştirici debug paneli | Hiçbiri — her oyunda kullanılır |
| **SaveThrottler** | Zaman pencereli kayıt kısıtlama | Hiçbiri — her oyunda kullanılır |
| **PluginSystem** | INexusPlugin + PluginContext ile genişletilebilir mimari | Hiçbiri — eklenti altyapısı |

### 2.2 Mevcut Oyun Katmanı (RingFlow Referansı)

Bu bölüm, Nexus'un oyun türünden bağımsız olduğunu göstermek amacıyla **sadece referans amaçlıdır.** Aşağıdaki sistemler RingFlow oyununa aittir, Nexus paketi içinde yer almaz.

| Sistem | Tür | Nexus'a Alınacak Generic Versiyon |
|--------|-----|-----------------------------------|
| **LevelGenerator** | Puzzle-specific | → Generic `PuzzleGenerator<TState, TMove>` (opsiyonel modül) |
| **LevelSolver** | Puzzle-specific | → Generic `PuzzleSolver<TState, TMove>` (opsiyonel modül) |
| **ReplayEngine** | Puzzle-specific | → Generic `ReplayEngine<TState, TMove>` (opsiyonel modül) |
| **DailyRewardService** | Casual generic | → Nexus'a taşınacak, tüm oyun türlerine uygun |
| **PlayerProgressModel** | Casual generic | → Nexus'a taşınacak, tüm oyun türlerine uygun |
| **GameFeelConfigSO** | Casual generic | → Nexus'a taşınacak, konfigürasyon altyapısı |
| **ProceduralAudio** | Generic | → Nexus'a taşınacak, opsiyonel modül |
| **VfxPrefabRegistry** | Generic | → Nexus'a taşınacak, pooling altyapısı |
| **RingColorPalette** | Puzzle-specific | RingFlow'da kalır |
| **RingValidationStrategy** | Puzzle-specific | RingFlow'da kalır |

---

## 3. Oyun Türünden Bağımsız Genelleştirme Stratejisi

### 3.1 Generic Servis Katmanı

Nexus çekirdek paketi **hiçbir oyun türüne özel kod içermez.** Tüm servisler aşağıdaki kriterleri sağlar:

```csharp
public interface INexusService
{
    bool IsInitialized { get; }
    void Initialize(NexusContext context);
    void Dispose();
}
```

Her servis üç kullanım modundan birine sahiptir:

| Mod | Açıklama | Örnek |
|-----|----------|-------|
| **ZORUNLU** | Tüm oyunlarda aktif olmalı | `SignalBus`, `DI Container`, `TickService`, `LoggerService` |
| **OPSİYONEL** | Oyun ihtiyacına göre kaydedilir | `EconomyService`, `IapService`, `AdService`, `ProgressionService` |
| **PLUGIN** | Sadece ihtiyaç duyan oyunlar yükler | `PuzzleSolver<T>`, `DOTSBridge`, `NetworkSignalBus` |

### 3.2 Gameplay Eklenti Mimarisi

Oyun türüne özel gameplay mantığı, Nexus çekirdeği içinde değil, **Plugin System** üzerinden yüklenir.

```
Proje/
├── Packages/
│   └── com.nexus.core/               ← Çekirdek (tüm oyunlarda aynı)
│       └── Runtime/
│           ├── Core/                   ← DI, SignalBus, Command, FSM
│           ├── Services/               ← Audio, Pool, Storage, etc.
│           └── Plugins/               ← Plugin System
├── Assets/
│   └── Scripts/
│       ├── NexusGamePlugin/           ← Oyun özel plugin'leri
│       │   ├── PuzzlePlugin/          ← (Opsiyonel) Bulmaca
│       │   ├── EconomyPlugin/         ← (Opsiyonel) Ekonomi
│       │   └── SocialPlugin/          ← (Opsiyonel) Sosyal
│       └── GameSpecific/             ← Oyun özel kod
```

```csharp
public class PuzzleGamePlugin : INexusPlugin
{
    public string Id => "com.example.puzzle";
    public string Version => "1.0.0";

    public void Load(PluginContext context)
    {
        context.Bind<IPuzzleSolver, PuzzleSolver>();
        context.Bind<IPuzzleGenerator, PuzzleGenerator>();
    }
}
```

### 3.3 Tür Denetleyicileri

Nexus, her oyun projesinde hangi tür bileşenlerin çalıştığını bilmek ve validate etmek için bir tür kayıt sistemi kullanır:

```csharp
public enum GameGenre
{
    Puzzle,
    HyperCasual,
    Casual,
    Hybrid,
    Simulation,
    Strategy,
    Idle,
    RPG,
    Action,
    Adventure,
    Word,
    Card,
    Educational,
    Custom   // Kullanıcı tanımlı
}

public class GameProfile
{
    public string ProjectName { get; set; }
    public GameGenre PrimaryGenre { get; set; }
    public GameGenre[] SubGenres { get; set; }
    public string[] RequiredPlugins { get; set; }
    public string[] OptionalPlugins { get; set; }
}
```

### 3.4 Önceden Tanımlı Oyun Türü Şablonları

Nexus, yaygın oyun türleri için starter template konfigürasyonları sunar:

| Oyun Türü | Zorunlu Servisler | Opsiyonel Servisler | Plugin'ler |
|-----------|------------------|-------------------|------------|
| **Bulmaca** | DI, Signal, Command, FSM, Tick, Audio, Pool, Storage, Localization, Haptic, Logger | Economy, Analytics, WindowManager | PuzzlePlugin (solver, generator) |
| **Hiper-Casual** | DI, Signal, Tick, Audio, Pool, Storage, AdService, Logger | FSM, Haptic, Localization | AdPlugin, LevelPlugin |
| **Idle/RPG** | DI, Signal, Command, FSM, Tick, Audio, Pool, Storage, Economy, Logger | IAP, Analytics, Progression, WindowManager | ProgressionPlugin, EconomyPlugin |
| **Simülasyon** | DI, Signal, Command, FSM, Tick, Audio, Pool, Storage, Localization, Logger | Economy, IAP, Analytics, WindowManager | — |
| **Strateji** | DI, Signal, Command, FSM, Tick, Audio, Pool, Storage, Logger | Economy, Analytics, Haptic, WindowManager | PuzzlePlugin (opsiyonel) |

### 3.5 Yeniden Kullanılabilir Oyun Modülü Kataloğu

Nexus bünyesinde, oyun türünden tamamen bağımsız, her oyunda kullanılabilecek modüller:

| Modül | Açıklama | Bağımlılık |
|-------|----------|-----------|
| **DailyRewardSystem** | Günlük ödül çevrimi (streak, claim, timer) | `StorageService`, `EconomyService` (opsiyonel) |
| **PlayerProgression** | Seviye/XP/unlock sistemi | `StorageService` |
| **ChestSystem** | Sandık açma, zamanlayıcı, ödül | `StorageService` |
| **TutorialSystem** | Adım adım oyuncu eğitimi | `SignalBus`, `FSM` (opsiyonel) |
| **RateUsSystem** | Uygulama değerlendirme akışı | `StorageService` |
| **NotificationSystem** | Yerel bildirim yönetimi | Platform bağımlı |
| **RemoteConfig** | Uzaktan yapılandırma (Firebase/opsiyonel) | `StorageService` |
| **ABTestSystem** | A/B test altyapısı | `AnalyticsService` |
| **SocialLeaderboard** | Liderlik tablosu | `StorageService`, ağ (opsiyonel) |

---

## 4. Proje Entegrasyon Süreci

### 4.1 Proje Başlatma ve Entegrasyon

Her yeni oyun projesi aşağıdaki sırayla Nexus'a bağlanır:

```
Adım 1: Proje oluşturma
  └─ Unity 6 LTS ile yeni proje
  └─ GitHub repo oluşturma (template kullanılabilir)

Adım 2: Nexus çekirdek paketini ekleme
  └─ git submodule add <nexus-repo> Submodules/Nexus
  └─ Unity Package Manager -> Add package from disk
  └─ packages-lock.json güncellenir

Adım 3: GameProfile tanımlama
  └─ Oyun türü, platform, plugin'ler belirlenir
  └─ Editor aracı ile otomatik yapılandırma

Adım 4: Bootstrap/Context oluşturma
  └─ Root GameObject + Context + DI bağlamaları
  └─ Editor aracı ile otomatik oluşturma

Adım 5: Projeye özel servisleri kaydetme
  └─ PluginSystem üzerinden oyun modülleri eklenir
  └─ Zorunlu servis validasyonu yapılır

Adım 6: Doğrulama
  └─ Editor → "Nexus → Validate Integration"
  └─ Tüm bağımlılıklar çözüldü, servisler kaydedildi
```

**Entegrasyon süresi hedefi:** Tecrübeli bir geliştirici için **<10 dakika** (editör araçları ile).

### 4.2 Uyumluluk Tespiti ve Özel Ayarlamalar

Her yeni projede Nexus, projenin teknik gereksinimleri ile paket arasında uyumsuzluk olup olmadığını tespit eder:

| Uyumsuzluk Türü | Tespit Yöntemi | Çözüm |
|----------------|---------------|-------|
| Unity sürümü farkı | `#if UNITY_X` | Gerekirse compat shim |
| Render pipeline farkı | `GraphicsSettings.renderPipelineAsset` | Servis conditional initialization |
| Platform kısıtlaması | `#if UNITY_ANDROID` / `#if UNITY_IOS` | Platform spesifik implementasyon |
| Bağımlılık çakışması | Editor validasyon | CHANGELOG/MIGRATION ile yönlendirme |
| Third-party çakışması | Assembly definition analizi | Uyarı + dokümantasyon |

**Özel ayarlamalar minimumda tutulur:**
- Proje özelinde yapılan her değişiklik `GameProfile` içinde belgelenir
- Tüm projeleri etkileyecek değişiklikler ana Nexus branch'ine yapılır
- Sadece o projeye özel değişiklikler `Plugins/GameSpecific/` altında tutulur
- Hiçbir proje özel patch, Nexus çekirdek dosyalarını değiştirmez

### 4.3 Geçmiş Projelerde Adaptasyon

Mevcut projeler Nexus'a taşınırken:
1. **RingFlow örneği:** RingFlow Nexus ile geliştirilmiş olduğu için geçiş minimumdur. Sadece doğrulama adımları uygulanır.
2. **Diğer mevcut projeler:** Eski projelerde GameContainer dizini oluşturulur, Nexus submodule olarak eklenir, eski altyapı kodları teker teker Nexus çağrılarına dönüştürülür.
3. **Adaptasyon süresi:** Proje büyüklüğüne göre 1-5 gün.

### 4.4 Üretim Öncesi Doğrulama

Her oyun projesi üretim ortamına geçmeden önce Nexus doğrulama sürecinden geçer:

```
┌─────────────────────────────────────────────────────────┐
│                ÜRETİM ÖNCESİ DOĞRULAMA                    │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  1. Servis Sağlık Kontrolü                              │
│     ├─ Tüm kayıtlı servisler Initialize edildi mi?      │
│     ├─ Hata veya uyarı var mı?                          │
│     └─ Bağımlılıklar çözüldü mü?                        │
│                                                          │
│  2. Performans Validasyonu                              │
│     ├─ GC alloc: 0 byte (gameplay anı)                  │
│     ├─ Frame time: <16.6ms (60 FPS)                     │
│     ├─ Bellek: <150MB (tüm oyun)                        │
│     └─ Nexus altyapı payı: <0.5ms                      │
│                                                          │
│  3. Güvenlik Doğrulaması                                │
│     ├─ Storage: EncryptedStorageService kullanılıyor mu?│
│     ├─ Anti-tamper: HMAC checksum aktif mi?             │
│     ├─ Device binding çalışıyor mu?                     │
│     └─ GDPR/COPPA uyumu tam mı?                        │
│                                                          │
│  4. Versiyon Uyumu                                      │
│     ├─ Nexus sürümü proje ile uyumlu mu?                │
│     ├─ Save format version control ediliyor mu?         │
│     └─ Migration path tanımlı mı?                       │
│                                                          │
│  5. Nexus Editor Raporu                                 │
│     └─ Tüm testler -> PASS/FAIL (tek tık raporu)         │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

---

## 5. Merkezi Altyapı Servisleri

Tüm mevcut ve yeni oyun projelerinde ortak altyapı işlemleri **yalnızca Nexus paketi üzerinden** gerçekleştirilir.

### 5.1 Zorunlu Servisler

Aşağıdaki işlemler için Nexus dışında hiçbir kod kullanılamaz:

| İşlem | Nexus Servisi | Yedek/Alternatif |
|-------|--------------|------------------|
| **Bağımlılık enjeksiyonu** | `NexusDI` | Manuel DI yasak |
| **Sinyal/olay yönetimi** | `SignalBus` | UnityEvent, C# event, delegate yasak (gameplay için) |
| **FSM/Durum yönetimi** | `GameStateMachine` | Custom FSM yasak |
| **Update döngüsü** | `TickService` | MonoBehaviour.Update yasak (gameplay için) |
| **Nesne havuzu** | `ObjectPoolService` | Instantiate/Destroy yasak (gameplay için) |
| **Veri saklama** | `EncryptedStorageService` | PlayerPrefs, File.Write yasak |
| **Dil desteği** | `LocalizationService` | Hardcoded string yasak |
| **Günlük kaydı** | `LoggerService` | Debug.Log, Console.Write yasak |
| **Audio** | `AudioService` | Direct AudioSource yasak |
| **Hata kurtarma** | `Recovery` | Try-catch ile kurtarma yasak |
| **UI Window** | `WindowManager` | Direct Instantiate yasak |
| **Hata raporlama** | `LoggerService + CausalTracing` | Custom crash logger yasak |
| **Platform bildirimi** | `AnalyticsService` | Custom analytics yasak |

### 5.2 Opsiyonel Servisler

Oyun ihtiyacına göre kullanılan Nexus servisleri:

| İşlem | Nexus Servisi | Alternatif |
|-------|--------------|-----------|
| **Ödeme/iç satın alma** | `IapService` | Manuel IAP SDK yasak |
| **Reklam gösterimi** | `AdService` | Manuel Ad SDK yasak |
| **Oyun içi ekonomi** | `EconomyService` | Custom economy yasak |
| **Oyun ilerleme** | `ProgressionService` | Custom progression yasak |
| **Dokunsal geri bildirim** | `HapticService` | Custom haptics yasak |
| **Ağ bağlantısı** | `NetworkMonitor` | Custom network check yasak |
| **Debug HUD** | `NexusDebugHUD` | Custom debug panel yasak |

### 5.3 Servis Kullanım Politikaları

1. **Doğrudan SDK kullanımı yasaktır** — Firebase, Unity IAP, Unity Ads, AppLovin, GameCenter, Google Play Services vb. tüm üçüncü taraf SDK'lar yalnızca Nexus servisleri üzerinden erişilir.
2. **Servis soyutlamasına uyulur** — Kod içinde `new AudioSource()`, `PlayerPrefs.SetInt()`, `Debug.Log()` gibi doğrudan API çağrıları yapılamaz.
3. **Her servis kendi sorumluluk alanında çalışır** — AudioService UI'ya, StorageService gameplay'e karışmaz.
4. **Kod tekrarı sıfırlanır** — Aynı işlemi (save/load, dil değiştirme, ödül claim) farklı projelerde farklı şekilde yapmak yasaktır.

### 5.4 Servis Sağlık İzleme

Nexus, bağlı tüm projelerde servis sağlığını runtime'da izler:

```csharp
public class ServiceHealthReport
{
    public string ServiceName { get; }
    public bool IsInitialized { get; }
    public float MemoryFootprintMB { get; }
    public float LastFrameMs { get; }
    public int ErrorCount { get; }
    public float UptimeHours { get; }
    public HealthStatus Status { get; }  // Healthy, Degraded, Critical
}
```

- Her servis `INexusService` arayüzü üzerinden periyodik sağlık raporu verir
- `CausalTracing` ile servis hataları kaydedilir
- Kritik hatalarda `Recovery` devreye girer
- Editor aracı tüm projelerin servis sağlığını görüntüleyebilir (opsiyonel)
