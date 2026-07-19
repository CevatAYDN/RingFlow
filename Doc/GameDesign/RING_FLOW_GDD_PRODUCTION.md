# Ring Flow GDD - Tutarlı Tasarım Kuralları

## Temel hedef
Ring Flow dikey ekranda oynanan, data-driven ve deterministik bir puzzle oyunudur. Tüm level, tutorial, kapasite, renk havuzu, direk yerleşimi ve zorluk kuralları veri üzerinden yönetilir. Designer, Nexus MVCS altyapısına uygun veri ve editor araçlarıyla oyun içeriğini kod yazmadan yönetebilmelidir.

## Ekran ve yerleşim
- Oyun dikey ekrana göre tasarlanır.
- Direkler dinamik üretilir.
- Direk boyutu, aralıkları ve dokunma alanları cihaz çözünürlüğüne göre uyarlanır.
- Direkler oyuncunun rahatça tıklayabileceği mesafede konumlandırılır.
- Sabit ekran koordinatları kullanılmaz.
- Yerleşim ve kamera framing, `GameFeelConfigSO` ve `BoardView` tarafından veri odaklı hesaplanır.
- Safe area ve cihaz uyarlaması `SafeAreaHandler` ile desteklenir.
- Kamera başlangıç konumu ve bazı temel çerçeve değerleri lifecycle başlatılırken zorlanır; bu davranış config ile çelişmeyecek şekilde korunur.

## Direk kapasitesi
- Her direğin kapasitesi veri olarak tanımlanır.
- Kapasite zorlukla birlikte kademeli artar.
- Başlangıç kapasitesi 3’tür.
- İlerleyen seviyelerde daha yüksek kapasite değerleri difficulty band ve `GameConfigDatabaseSO` üzerinden belirlenir.
- Kapasite hardcode edilmez.

## Boş direk kuralı
- Boş direk sayısı level tipine ve zorluk bandına göre değişebilir.
- Kolay ve orta seviyelerde en az 1 boş direk bulunur.
- Zor seviyelerde boş direk sayısı azaltılabilir.
- Boş direk kullanımı level verisiyle belirlenir.
- “Her level sonunda tüm direkler dolu olacak” kuralı mutlak değildir.
- Minimum boş direk değeri `GameConfigDatabaseSO` içindeki difficulty band verisinden okunur.

## Renk sistemi
- Renk havuzu level ilerledikçe kademeli genişler.
- Başlangıç seviyelerinde az renk kullanılır.
- Orta seviyelerde çeşitlilik artar.
- İleri seviyelerde daha geniş ve dengeli kombinasyonlar kullanılır.
- Aynı renk kombinasyonlarının gereksiz tekrar etmesi engellenir.

## Zorluk eğrisi
- Zorluk tek parametreyle belirlenmez.
- Zorluk şu faktörlerle artar:
  - renk sayısı
  - direk kapasitesi
  - boş direk sayısı
  - başlangıç karışıklığı
  - çözüm derinliği
  - karar baskısı
- İlk seviyeler öğretici olmalı.
- Orta seviyeler tempo ve çeşitlilik artırmalı.
- İleri seviyeler daha stratejik olmalı.
- Oyuncu ilerledikçe zorluk hissi net biçimde artmalı.
- Zorluk bandı, kapasite ve boş direk kurallarını belirleyen veri seti tarafından yönetilmelidir.

## Tutorial
- Tutorial, editörden yönetilen özel bir level setidir.
- Designer istediği kadar tutorial level ekleyebilir.
- Tutorial level’lar ana progression’dan bağımsızdır.
- Tutorial ve normal level’lar aynı `LevelDataSO` şemasını kullanır.
- Tutorial akışı tamamen data-driven olur.
- Tutorial, normal level şemasından ayrı bir veri modeli üretmez.

## Level verisi
Her level verisi, tutorial ve normal akışta aynı temel şemayı paylaşmalıdır. Veri modeli şu alanları kapsamalıdır:
- level tipi
- direk sayısı
- direk kapasitesi
- renk havuzu
- boş direk sayısı
- başlangıç dizilimi
- zorluk seviyesi
- tutorial / normal etiketi
- solver ve editor validasyonunda kullanılan kural referansları
- challenge durumu ve diğer progression bayrakları

## Görsel kurallar
- Halkalar direk içinde sıkı ve temaslı görünmelidir.
- Halkalar arasında gereksiz boşluk kalmamalıdır.
- Halkalar direğin içinden geçiyormuş gibi görünmemelidir.
- Sorting, layer ve efekt düzeni bu algıyı engelleyecek şekilde ayarlanır.
- Direk ve halka görünümü premium ve okunaklı olmalıdır.

## UI ve akış
- Aktif level numarası tüm ana ekranlarda görünür olmalıdır.
- Sonuç ekranında level numarası daha belirgin gösterilmelidir.
- Level tamamlama ekranı, konfeti ve bitiş animasyonları tatmin edici olmalıdır.
- UI butonları modern, net ve okunaklı olmalıdır.

## Data-driven mimari
- Tüm kritik oyun parametreleri veri tabanlıdır.
- Hardcode level mantığı kullanılmaz.
- Designer, kod yazmadan tutorial, level, kapasite, renk havuzu ve boş direk dağılımını yönetebilir.
- İlgili editorler tutarsız veri girişini engeller.
- Generator, solver ve editor validasyonları aynı kural setini kullanır.
- Command, Model, View ve Mediator rolleri Nexus MVCS sınırlarına uygun ayrılır.

## Validasyon
- Her level solvable olmalıdır.
- Kapasite dağılımı zorluk bandı ile uyumlu olmalıdır.
- Boş direk sayısı level tipine uygun olmalıdır.
- Renk havuzu ve başlangıç dizilimi çelişmemelidir.
- Geçersiz level kombinasyonları kaydedilmemelidir.
- Editor, hatalı veriyi oluşturma aşamasında işaretlemelidir.
- Validator, aynı kural kaynağını kullanarak generator ve solver ile tutarlı sonuç vermelidir.

## Özet
Ring Flow’da kalite; görsel polish, veri tutarlılığı, solvable level yapısı, kademeli zorluk eğrisi ve designer dostu editor akışı ile sağlanır.