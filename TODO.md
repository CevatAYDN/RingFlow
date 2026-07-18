# RingFlow UI Premium Polish — TODO

## Runtime doğruluk & risk audit (tamamlandı)
- [x] UIRoot ekran lifecycle / popup stack / reduced motion okundu
- [x] ScreenRegistrySO + ScreenType sinyalleri doğrulandı
- [x] Splash/SplashMediator akışı doğrulandı
- [x] MainMenu/MainMenuMediator etkileşimleri doğrulandı
- [x] LevelSelect/LevelSelectMediator doğrulandı
- [x] HUD/HUDMediator (overlay/perf & button states) doğrulandı
- [x] Pause/PauseMediator doğrulandı
- [x] Settings/SettingsView + mediator bağlantıları gözden geçirildi
- [x] DailyRewardPopup/DailyRewardPopupMediator doğrulandı
- [x] ChestPopup/ChestPopupMediator doğrulandı
- [x] ParentalGatePopup/ParentalGatePopupMediator + View flow doğrulandı
- [x] Onboarding/OnboardingMediator doğrulandı
- [x] WorldMap/WorldMapMediator (stub) doğrulandı
- [x] Win/WinView + GameOver/GameOverMediator doğrulandı

## UI Studio template + prefab uyumu (başlamadı)
- [ ] UI Studio template’lerinde: font/typography, spacing, buton hover/press/focus/disabled, modal/popup overlay template’leri güncellenecek
- [ ] Nexus UI binding / mediator akışını bozmadan ekran prefab hiyerarşileri doğrulanacak

## Premium polish (başlamadı)
- [ ] Popup/overlay ağırlıklı ekranları öncele: Pause, Settings, DailyReward, ChestPopup, ParentalGate
- [ ] Sonra base ekranlar: MainMenu, LevelSelect
- [ ] Sonra overlay/UX kritik: Gameplay HUD
- [ ] Sonra reward/success: Win, GameOver
- [ ] En son: Onboarding, WorldMap

## Accessibility & reduced motion (başlamadı)
- [ ] Keyboard focus/Tab cycle’i her ekranda garantiye alın (ilk focus target + doğru Selectables)
- [x] Reduce motion: DOTween animasyonları ekran bazında saydamca kesilecek (SettingsModel.ReduceMotion)
- [ ] Kontrast / okunabilir font boyutları / touch target büyüklükleri gözden geçirilecek

## Verification (başlamadı)
- [ ] Tüm ekranlar: aç/kapa testleri
- [ ] Overlay/popup stacking sıralaması testleri
- [ ] Çoklu çözünürlük/aspect ratio görsel testleri
- [ ] Performans regresyon testi (UI update’de GC allocation kontrol)
