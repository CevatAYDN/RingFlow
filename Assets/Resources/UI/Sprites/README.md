# UI Sprite Kütüphanesi — Kurulum Talimatı

## Unity'de Yapılması Gerekenler

### Adım 1 — UISpriteLibrary Asset Oluştur

1. Unity'de **Project** panelinde sağ tıkla
2. **Create → RingFlow → UI Sprite Library** seç
3. Oluşan asset'i `Assets/Resources/Configs/` klasörüne taşı
4. İsmi `UISpriteLibrary` olarak bırak

### Adım 2 — UIThemeConfig'e Bağla

1. `Assets/Resources/Configs/UIThemeConfig` asset'ini seç
2. Inspector'da en altta **🖼 Sprite Library** alanını bul
3. Oluşturduğun `UISpriteLibrary` asset'ini bu alana sürükle

### Adım 3 — Sprite'ları Ata

`UISpriteLibrary` asset'ini seç. Inspector'da şu kategorileri göreceksin:

| Kategori | İçerik |
|---|---|
| 🔘 Buttons | btn_primary, btn_secondary, btn_success, btn_danger, btn_icon, btn_back, btn_close |
| 🗂 Panels | panel_card, panel_popup, panel_hud, panel_tooltip |
| 💰 Icons — Currency | icon_coin, icon_gem, icon_xp, icon_star, icon_star_empty, icon_chest, icon_trophy, icon_daily |
| 🎮 Icons — Controls | icon_settings, icon_pause, icon_undo, icon_hint, icon_play, icon_lock, icon_check, icon_close, icon_home |
| 🔊 Icons — Audio | icon_sound_on/off, icon_music_on/off, icon_vibration, icon_colorblind, icon_world |
| 🖥 HUD | hud_top_bar, hud_currency_pill, hud_progress_fill, hud_progress_bg |
| 🌄 Backgrounds | bg_main, bg_gameplay, bg_blur_overlay |
| 🏆 Rewards | reward_star_1/2/3, reward_chest_common/rare/epic, reward_coin_burst |
| 🔴 Badges | badge_notification, badge_new, badge_hot, badge_sale |

PNG dosyaları zaten `Assets/Resources/UI/Sprites/` altında var.  
Her alanı doldurmak için ilgili PNG'yi sürükle.

---

## Kod'dan Sprite Kullanımı

### Yöntem 1 — String key ile (esnek, runtime swap'ı destekler)

```csharp
// UISpriteKeys sabitleri kullan (magic string yok)
var icon = GameUIResources.GetSprite(UISpriteKeys.IconCoin);
myImage.sprite = icon;
```

### Yöntem 2 — Doğrudan typed property ile (derleme zamanı güvenli)

```csharp
// Inspector'dan atanmış sprite'a direkt eriş
myImage.sprite = GameUIResources.SpriteLibrary?.IconCoin;
```

### Yöntem 3 — Fallback otomatik çalışır

Eğer `SpriteLibrary` atanmamış veya bir alan null ise,  
`GetSprite()` otomatik olarak `Resources.Load<Sprite>("UI/Sprites/Icons/icon_coin")` yapar.

---

## Sprite'ı Değiştirmek

İstediğin zaman:
1. `UISpriteLibrary` asset'ini Inspector'dan aç
2. Değiştirmek istediğin alanın üzerine yeni sprite'ı sürükle
3. **C# kodu değiştirme — sadece referans değişir**

---

## Klasör Yapısı

```
Assets/Resources/UI/Sprites/
├── Buttons/
│   ├── btn_primary.png
│   ├── btn_secondary.png
│   ├── btn_success.png
│   ├── btn_danger.png
│   ├── btn_icon.png
│   ├── btn_back.png
│   └── btn_close.png
├── Panels/
│   ├── panel_card.png
│   ├── panel_popup.png
│   ├── panel_hud.png
│   └── panel_tooltip.png
├── Icons/
│   ├── icon_coin.png
│   ├── icon_gem.png
│   ├── icon_xp.png
│   ├── icon_star.png
│   ├── icon_star_empty.png
│   ├── icon_chest.png
│   ├── icon_trophy.png
│   ├── icon_daily.png
│   ├── icon_settings.png
│   ├── icon_pause.png
│   ├── icon_undo.png
│   ├── icon_hint.png
│   ├── icon_play.png
│   ├── icon_lock.png
│   ├── icon_check.png
│   ├── icon_close.png
│   ├── icon_home.png
│   ├── icon_sound_on.png / icon_sound_off.png
│   ├── icon_music_on.png / icon_music_off.png
│   ├── icon_vibration.png
│   ├── icon_colorblind.png
│   └── icon_world.png
├── HUD/
│   ├── hud_top_bar.png
│   ├── hud_currency_pill.png
│   ├── hud_progress_fill.png
│   └── hud_progress_bg.png
├── Backgrounds/
│   ├── bg_main.png
│   ├── bg_gameplay.png
│   └── bg_blur_overlay.png
├── Rewards/
│   ├── reward_star_1.png / reward_star_2.png / reward_star_3.png
│   ├── reward_chest_common.png / reward_chest_rare.png / reward_chest_epic.png
│   └── reward_coin_burst.png
└── Badges/
    ├── badge_notification.png
    ├── badge_new.png
    ├── badge_hot.png
    └── badge_sale.png
```
