# RingFlow — Data-Driven Mimari ve Editör Araçları Uygulama Planı

## Özet

Bu plan; projedeki hardcoded değerlerin kaldırılarak veri odaklı bir mimariye geçirilmesini, eksik ScriptableObject config varlıklarının oluşturulmasını, mevcut veri merkezlerinin editör araçlarıyla tam kontrol edilebilir hale getirilmesini ve editör araçlarının kullanıcı dostu tek bir merkeze konulmasını ele alır. GDD'nin `RING_FLOW_GDD_PRODUCTION.md` ve Nexus MVCS kuralları gözetilerek hazırlanmıştır.

---

## BÖLÜM 1 — Hardcoded Değerlerin Temizlenmesi (Runtime & Editor)

### 1.1 BGM Çarpanlarını AudioConfigSO'ye Taşıma

**Sorun:** `PlayingState.cs:39,62` — `0.40f` ve `0.80f` BGM çarpanları hardcoded. GDD §79 "Audio/MusicMix merkezi veri seti" der.

**Yapılacaklar:**
1. `AudioConfigSO.cs` içindeki `AudioBgmConfig` struct'ına iki yeni alan eklenir:
   ```csharp
   public float StateMultiplierNormal; // default 0.40f
   public float StateMultiplierBoss;    // default 0.80f
   ```
2. `AudioConfigSO` sınıfının `Bgm` alan başlatıcısında bu iki değere defaults atanır.
3. `PlayingState.cs:39` ve `:62`'deki hardcoded `0.40f`/`0.80f` sabitleri, enjekte edilen `AudioConfigSO` üzerinden okunan değerlerle değiştirilir. `PlayingState` zaten `_dbConfig` enjekte ediyor; yeni bir `_audioConfig` alanı eklenir (DI ile).
4. `AudioConfigSOEditor.cs` içindeki BGM bölüm bu iki yeni alanı editörde gösterir.

### 1.2 IAP Ürün Kataloğunu ScriptableObject'e Taşıma (Yeni SO)

**Sorun:** `GameplayLifecycle.cs:308-312` — `remove_ads`, `coins_100`, `diamonds_50` ürün kimlikleri ve fiyatları hardcoded. GDD §67 "Economy/Store veri seti" ister.

**Yapılacaklar:**
1. Yeni dosya: `Assets/Scripts/Gameplay/Economy/StoreCatalogSO.cs`
   - `[CreateAssetMenu(fileName = "StoreCatalog", menuName = "RingFlow/Store Catalog")]`
   - `List<StoreProductEntry>` barındırır; her entry: `string Id`, `ProductType Type`, `string PriceString`, `string DisplayNameKey` (localization), `string DescriptionKey` (opsiyonel).
2. Yeni editör inspector: `Assets/Editor/StoreCatalogSOEditor.cs` — ürün ekle/sil/sırala, fiyat alanı, tür dropdown.
3. `GameplayLifecycle.cs:303-314` `RegisterIapProducts` metodu, `Resources.Load<StoreCatalogSO>("StoreCatalog")` ile yükleyip listeden `iap.RegisterProducts(...)` çağırır. Asset bulunamazsa fail-loud (`InvalidOperationException`).
4. `EditorPaths.cs` içine `StoreCatalogKey = "StoreCatalog"` ve `StoreCatalogPath = "Assets/Resources/" + StoreCatalogKey + ".asset"` eklenir.
5. `GameplayAssetKeys.cs` içine `public const string StoreCatalog = "StoreCatalog";` eklenir.
6. `ConfigSection.cs`'a yeni bir `DrawRow<StoreCatalogSO>` eklenir.

### 1.3 Dil Listesini ScriptableObject'e Taşıma (Yeni SO)

**Sorun:** `GameplayLifecycle.cs:224` ve `SettingsSection.cs:13-18` — 15 dil kodu hardcoded iki yerde.

**Yapılacaklar:**
1. Yeni dosya: `Assets/Scripts/Gameplay/Localization/LocalizationConfigSO.cs`
   - `List<LanguageEntry>` (`string Code`, `string DisplayName`, `bool IsRTL`).
   - `CreateAssetMenu` attribute.
2. Yeni editör inspector: `Assets/Editor/LocalizationConfigSOEditor.cs`.
3. `GameplayLifecycle.cs:222-231` döngüsü SO'dan gelen listeyle değiştirilir; SO `Resources.Load<LocalizationConfigSO>("LocalizationConfig")` ile yüklenir ve DI'a bind edilir.
4. `SettingsSection.cs` editörünü de SO'dan okur hale getirir.
5. `EditorPaths` ve `GameplayAssetKeys` içine `LocalizationConfig` anahtarı eklenir.

### 1.4 GameplayAssetKeys.Tuning Sabitlerini GameConfigDatabaseSO'ye Taşıma

**Sorun:** `GameplayAssetKeys.cs:70-104` — 11 adet `const` tuning değeri. Bir kısmı zaten `GameConfigDatabaseSO` içinde config olarak mevcut (`PoleCountClamp`, `MaxCapacity` band değeri, `BombCountdown`, `BaseGenerationSeedMultiplier`, `RetrySeedMultipliers`).

**Yapılacaklar:**
1. Hâlâ sadece `Tuning` içinde kalan ve bir SO'da karşılığı olmayan değerler `GameConfigDatabaseSO` veya `LevelGenConfig`'e eklenir: `MaxPoleCount` (zaten `PoleCountClamp` ile aynıysa dokunmaya gerek yok), `SentinelMinRings`, `ColorIndexMax`, `ColorIndexFallback`, `TweenCapacityDefault`, `SequenceCapacityDefault`.
2. Runtime kullanım yerleri güncellenir:
   - `GameplayModel.cs:11` `new(12)` → `new(GameConfigDatabaseSO.LevelGen.PoleCountClamp)`.
   - `LevelData.cs:65` `GameplayAssetKeys.Tuning.MaxCapacity` → DB değerinden.
3. `GameplayAssetKeys.Tuning` içindeki sabitler ya kaldırılır (DB'den okunur) ya da `*Default` ismiyle yalnızca test/editor için yedek olarak bırakılır; runtime *hiçbir yere* doğrudan `Tuning.X` refer vermemeli.

### 1.5 Runtime Yedek Hardcoded'lerini Kaldırma

**Sorun:** `InitLevelCommand.cs:76,87-89`, `HintCommand.cs:78,118`, `PlayerProgressModel.cs:62,78` — DB injection başarısız olduğunda hardcoded yedek değerlere düşülüyor.

**Yapılacaklar:**
1. `InitLevelCommand`: `baseSeedMultiplier` ve `RetrySeedMultipliers` için hardcoded yedekler kaldırılır; DB null ise `NexusLog.Error` + `InvalidOperationException` (fail-loud prensibi, AGENTS.md).
2. `HintCommand`: `_dbConfig != null ? _dbConfig.BalanceConfig.HintCoinCost : 50` yedeği kaldırılır; null ise fail-loud.
3. `PlayerProgressModel`: `if (db == null) return 100;` → fail-loud.
4. Tüm bu dosyalarda `using` ve _dbConfig injection'ı doğrulanır; mevcut enjeksiyonlar korunsun.

### 1.6 Editör Bölümlerindeki Hardcoded'lere SO Değerlerini Kullandırma

**Sorun:** `DatabaseSection.cs:294-298` hardcoded limit zinciri; `GeneratorSection.cs:255,330` `poleCount > 12` hardcoded.

**Yapılacaklar:**
1. `DatabaseSection.RunBatchValidation`: harded if-else zinciri `GameConfigDatabaseSO.LevelGen.SolverLimitBuckets` listesi üzerinde döngüye dönüştürülür.
2. `GeneratorSection.Generate()` ve `GenerateAllLevels()`: `12` sabiti `GameConfigDatabaseSO.LevelGen.PoleCountClamp` değerinden okunur.

---

## BÖLÜM 2 — Eksik ScriptableObject Veri Merkezleri (GDD'ye Göre)

### 2.1 Ring/Mechanic Veri Tabloları (GDD §30-41)

**Sorun:** Her özel mekanik için görsel kimlik, etkinleştirme dünyası, davranış parametreleri gömülü kod olarak yaşıyor.

**Yapılacaklar:**
1. Yeni dosya: `Assets/Scripts/Gameplay/Strategies/RingMechanicDataSO.cs`
   - `[CreateAssetMenu]` ile SO sınıfı.
   - Tek bir SO, her mekanik için bir liste: `List<MechanicEntry>` — `WorldMechanicType Type`, `string DisplayNameKey` (loc), `int FirstAppearanceWorldIndex` (GDD her mekanik için ayrı), `Sprite Icon`, `bool IsMovementRestricting` (Stone gibi), `List<RingType> AffectedRingTypes`.
2. `GameConfigDatabaseSO`'ye `List<MechanicEntry> MechanicUnlocks` eklenir (mekanik → ilk etkinleşme index). Böylece `DifficultyBandData.AllowedMechanics` ile birlikte her mekanik için hangi dünyada "ilk göründüğü" düzenlenebilir olur.
3. `LevelGenerator`: mekanik seçerken `FirstAppearanceWorldIndex` filtresini uygular (GDD §47 "her yeni mekanik önce tek başına tanıtılır").
4. Editör paneli: `DatabaseSection` içine yeni bir "Mekanik Açılış Tablosu" alt bölümü eklenir (reorderable list ile).

### 2.2 Theme/Skin Veri Tabloları (GDD §USP.04, §67)

**Sorun:** `UIThemeConfigSO` yalnızca UI renklerini yönetir; oyun dünyalarına özel tema/skin veri tablosu yok.

**Yapılacaklar:**
1. Yeni dosya: `Assets/Scripts/Gameplay/Views/ThemeSkinDatabaseSO.cs`
   - `List<ThemeSkinEntry>` — `int WorldIndex`, `string ThemeName` (loc key), `Color BackgroundColor`, `Color PoleColor`, `Color FloorColor`, `Material PoleMaterial`, `Material FloorMaterial`, `Sprite BgSprite` (opsiyonel), `AudioBgmConfig BgmConfigOverride`.
2. `GameConfigDatabaseSO.Worlds` listesindeki `WorldConfigData.Theme` string alanı korunur fakat tema verileri artık `ThemeSkinDatabaseSO`'den okunur.
3. `BoardView`, `PoleView`, `AmbientBackground` runtime'da `ThemeSkinDatabaseSO.GetForWorld(worldIdx)` ile konfigürasyon uygular.
4. Editör inspector: `Assets/Editor/ThemeSkinDatabaseSOEditor.cs` — dünya bazlı önizleme + renk seçici + material atama.
5. `GameplayLifecycle`'a `ThemeSkinDatabaseSO` yüklemesi ve DI bind'i eklenir.

### 2.3 Offer Rules (GDD §67) ve Monetization Targets (GDD §77)

**Not:** GDD bu veri setlerinden bahseder fakat detaylı spesifikasyon vermez. İkinci fazaya bırakılır; bu planın kapsamı dışında tutulur.

---

## BÖLÜM 3 — Mevcut Editör Araçlarının Kullanıcı Dostu Hale Getirilmesi

### 3.1 ConfigSection'a Eksik Varlıkları Ekleme

**Sorun:** `ConfigSection` yalnızca mevcut 5 SO'yi listeler; yeni oluşturulacak `StoreCatalogSO`, `LocalizationConfigSO`, `RingMechanicDataSO`, `ThemeSkinDatabaseSO` eklenmemiş.

**Yapılacaklar:**
1. `ConfigSection.cs` `OnGUI`'ya her yeni SO için `DrawRow<T>` çağrısı eklenir.
2. `EditorPaths`'a ilgili `*Key` ve `*Path` sabitleri eklenir.
3. Her yeni SO'nun `CreateAssetMenu` menü yolu `RingFlow/...` kapsamında çakışmasız tanımlanır.

### 3.2 GameConfigDatabaseSOEditor Kapsamını Dashboard ile Uyumlandırma

**Sorun:** `DatabaseSection.cs` daha az alanı düzenlerken `GameConfigDatabaseSOEditor.cs` `BalanceConfig` + `LevelGenConfig`'in tamamını düzenler. İki panel paralel, kullanıcı hangisini açacağını bilemez.

**Yapılacaklar:**
1. `DatabaseSection` panelini şu alanları içerecek şekilde genişlet:
   - `GameBalanceConfig`'in tamamı (Undo, Hint, Rewards, Stars, Chest, XP, Daily, Interstitial).
   - `LevelGenConfig`'in tamamı (SolverLimitBuckets, MechanicPriorityOrder, vb.).
2. Inspector (`GameConfigDatabaseSOEditor`) varlığını korusun; ama dashboard "Seviye Denetleyici" bölümünde her şey düzenlenebilsin.
3. "Açık Editöre Git" düğmesi: kullanıcı dilerse tam inspector'a geçebilir — `DatabaseSection`'ın altında "Inspector'ı Aç" butonu eklenir.

### 3.3 Tab Yapısını Genişletme

**Mevcut:** 4 tab — "Ana Sayfa", "Seviyeler", "Arayüz Stüdyosu", "Ayarlar & Araçlar".

**Yapılacaklar:**
1. "Ayarlar & Araçlar" tabı içinde yeni foldable bölümler eklenir.
2. İçerisine eklenen yeni foldout'lar:
   - "Mağaza Kataloğu (Store Catalog)" → `StoreCatalogSection`.
   - "Yerelleştirme Yapılandırması" → `LocalizationSection`.
   - "Mekanik Açılış Tablosu" → `RingMechanicDataSO`'nin editor paneli.
   - "Tema & Skin Veri Tabloları" → `ThemeSkinDatabaseSOEditor`.
3. `RingFlowEditorWindow.OnEnable`'da yeni section field'ları oluşturulur; `DrawToolsTab()` içine `DrawFoldableSection(...)` çağrıları eklenir.
4. `EditorPrefsKeys` içine yeni foldout anahtarları eklenir.

### 3.4 Hızlı Yardım & Tooltips

1. Her yeni foldout bölümüne `EditorGUILayout.HelpBox` ile GDD referansı (örn. "GDD §25 — Economy/HintCost") eklenir.
2. Her yeni editör alanı için `Tooltip` attribute veya `GUIContent(tooltip=...)` kullanılır.
3. Mevcut panellerde tooltip eksikleri yalnızca yeni eklenenlerle sınırlı tamamlanır (broad refactor değil).

### 3.5 "Data-Driven Denetimi" Validasyonu

1. `DiagnosticsSection`'a yeni bir "Data-Driven Denetimi" butonu eklenir.
2. Bu buton şu kontrolleri yapar:
   - `.asset` dosyaları mevcut mu (5+ yeni SO dahil).
   - Runtime hiçbir yere `GameplayAssetKeys.Tuning.X` hardcoded referansı kalmış mı (regex tabanlı kaynak taraması).
   - `GameplayLifecycle.RegisterIapProducts` hâlâ hardcoded liste içermiyor mu.
   - `PlayingState.cs` içinde `0.40f`/`0.80f` hardcoded sabit kalmış mı.
3. Kullanıcıya PASS/FAIL tek tık raporu gösterir.

---

## BÖLÜM 4 — Nexus MVCS Uyumu ve Entegrasyon

### 4.1 Yeni SO'lerin DI'a Bind Edilmesi

1. `GameplayLifecycle.cs` `OnConfigure`'a her yeni SO için:
   ```csharp
   var store = Resources.Load<StoreCatalogSO>(GameplayAssetKeys.StoreCatalog);
   if (store == null) throw new InvalidOperationException("StoreCatalog.asset not found!");
   builder.BindInstance<StoreCatalogSO>(store);
   ```
   aynı kalıp `LocalizationConfigSO`, `RingMechanicDataSO`, `ThemeSkinDatabaseSO` için.
2. `AOTPreserveAttributes.cs` `PreserveServices`/`PreserveCommands` içine yeni tipler eklenir (gerekirse).
3. `NexusGeneratedBinder.g.cs`'e manuel güncelleme gerekirse `// PRESERVE MANUAL ENTRIES` sentinel altında.

### 4.2 Performans & Memory Kuralları

- Yeni SO'ler `VersionedScriptableObject`'i miras alır (GDD §79 "Save format versioned" prensibiyle uyumlu).
- `Resources.Load` yalnızca boot sırasında yapılır; gameplay sırasında yok.
- Yeni SO'lerde LINQ kullanılmaz, string concatenation yok, foreach yalnızca `List<T>.ForEach` değil döngü ile.

### 4.3 Test Güncellemeleri

1. `GameplayStartupTests.cs` içine yeni SO'lerin yüklendiğini doğrulayan assertion'lar eklenir.
2. `SettingsAndLocalizationTests.cs` `LocalizationConfigSO`'den gelen dil listesi testini ekler.
3. `EconomyAndProgressionTests.cs` yeni `StoreCatalogSO` ile IAP ürün listesi testini ekler.
4. Hardcoded yedek kaldırma doğrulaması için yeni test `NoHardcodedFallbacksTests.cs`:
   - `PlayingState` BGM çarpanları SO'dan geliyor mu.
   - `InitLevelCommand` `12345` hardcoded'ine düşmüyor mu (fail-loud kontrol).

---

## BÖLÜM 5 — Doğrulama & Test Planı

### 5.1 Editör Pratik Dağılımı Testi

1. Unity'yi aç ve `Ring Flow > Dashboard` menüsü ile `RingFlowEditorWindow`'u aç.
2. "Ayarlar & Araçlar" tab'inde tüm foldout'ları aç; her bölümün görünür ve düzgün render olduğundan emin ol.
3. "Yapılandırma Varlıkları" bölümünden her bir yeni SO'yi "Oluştur" ile `.asset` dosyası yarat; `Assets/Resources/` altında oluştuğunu doğrula.
4. Her SO inspector'ı aç; tüm alanların doğru defaults dolduğunu doğrula.

### 5.2 Runtime Boot Testi

1. Play'e bas; `InvalidOperationException` fırlatılmadığını doğrula (eski hardcoded 5 hata noktası).
2. Bir oyuna gir; BGM çarpanının AudioConfigSO'den geldiğini doğrula (boss seviyesinde %80, normalde %40).
3. Settings popup'ında dil listesinin tam olduğunu ve değiştirilebildiğini doğrula.

### 5.3 Editör Modülü Testi

Unity EditMode testleri çalıştırılır:
- `GameplayStartupTests.cs`: Startup smoke testi PASS.
- `SettingsAndLocalizationTests.cs`: Dil listesi testi PASS.
- `EconomyAndProgressionTests.cs`: StoreCatalog yüklemesi PASS.
- Yeni `NoHardcodedFallbacksTests.cs`: hardcoded yedek yokluğu testi PASS.

### 5.4 PlayMode Entegrasyon Testi

`PlayModeIntegrationTests.cs`: Oyun başlatılır → bir hamle yapılır → geri dönüş yapılır → ipucu çalışır → hepsi yeni config-driven değerlerle doğru çalışır.

### 5.5 Data-Driven Denetimi

"DiagnosticsSection" altındaki yeni "Data-Driven Denetimi" butonuyla:
- 0 eksik `.asset` varlığı
- 0 hardcoded `Tuning.X` runtime referansı
- 0 hardcoded BGM çarpanı
- 0 hardcoded IAP ürün kimliği

---

## Uygulama Sırası

1. **Başlatma: yeni SO'ler ve .asset'lerin oluşturulması** (Bölüm 1.2, 1.3, 2.1, 2.2)
2. **Hardcoded temizliği** (Bölüm 1.1, 1.4, 1.5)
3. **Editör bölümleri güncellemesi** (Bölüm 1.6, 3.1, 3.2, 3.3)
4. **Tooltip/Yardım ve validasyon** (Bölüm 3.4, 3.5)
5. **Nexus entegrasyon** (Bölüm 4)
6. **Test ekleme & çalıştırma** (Bölüm 4.3, 5)

---

## Kritik Dosyalar

### Eklenecek Yeni Dosyalar
- `Assets/Scripts/Gameplay/Economy/StoreCatalogSO.cs`
- `Assets/Scripts/Gameplay/Localization/LocalizationConfigSO.cs`
- `Assets/Scripts/Gameplay/Strategies/RingMechanicDataSO.cs`
- `Assets/Scripts/Gameplay/Views/ThemeSkinDatabaseSO.cs`
- `Assets/Editor/StoreCatalogSOEditor.cs`
- `Assets/Editor/LocalizationConfigSOEditor.cs`
- `Assets/Editor/RingMechanicDataSOEditor.cs`
- `Assets/Editor/ThemeSkinDatabaseSOEditor.cs`
- `Assets/Tests/Editor/NoHardcodedFallbacksTests.cs`

### Değiştirilecek Mevcut Dosyalar
- `RingFlowEditorWindow.cs` — yeni section field'ları ve DrawToolsTab güncellemesi
- `ConfigSection.cs` — yeni DrawRow'lar
- `DatabaseSection.cs` — Bölüm 1.6 hardcoded zinciri kaldırma + GameBalance/LevelGen alanları
- `GeneratorSection.cs` — clamp 12 → PoleCountClamp
- `AudioConfigSO.cs` — StateMultiplier alanları
- `PlayingState.cs` — hardcoded 0.40/0.80 temizliği
- `GameplayLifecycle.cs` — IAP'den kataloğa geçiş + dil listesi SO'ye
- `SettingsSection.cs` — dil listesi SO'dan okuma
- `GameplayAssetKeys.cs` — yeni asset keyleri
- `EditorPaths.cs` — yeni path/key sabitleri
- `GameConfigDatabaseSO.cs` — MechanicUnlocks listesi
- `InitLevelCommand.cs` — hardcoded yedekleri kaldırma
- `HintCommand.cs` — hardcoded yedek kaldırma
- `PlayerProgressModel.cs` — hardcoded yedek kaldırma
- `GameplayStartupTests.cs`, `SettingsAndLocalizationTests.cs`, `EconomyAndProgressionTests.cs`, `DiagnosticsSection.cs`
- `EditorPrefsKeys.cs` — yeni foldout anahtarları
- `AOTPreserveAttributes.cs` — yeni tipler
