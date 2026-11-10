# DotNet Quest 🎮

Oynayarak **.NET Web API ve best practice'leri** öğreten bir oyun sitesi.
Her turda seçtiğin kategori ve zorlukta 5 soru gelir; her cevaptan sonra *neden* doğru ya da yanlış olduğunu anlatan bir açıklama görürsün.

Projenin kendisi de bir ders: backend, oyunda anlatılan best practice'lerle yazıldı.

## Çalıştırma

Gerekenler: **.NET 10 SDK**, **Node.js 20+** ve **PostgreSQL 13+**

İlk seferde veritabanı bağlantısını tanımla. Şifre içerdiği için repoya değil, bilgisayarındaki
[user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) deposuna yazılır:

```bash
cd src/DotnetQuest.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=dotnetquest;Username=postgres;Password=SIFREN"
```

Sonra iki terminal aç:

```bash
# 1) API (http://localhost:5080)
cd src/DotnetQuest.Api
dotnet run
```

```bash
# 2) React (http://localhost:5173)
cd client
npm install     # sadece ilk sefer
npm run dev
```

Tarayıcıda **http://localhost:5173** adresini aç.
API dokümantasyonu (Scalar): **http://localhost:5080/scalar**

> `dotnetquest` veritabanı ilk açılışta otomatik oluşturulur, tablolar kurulur ve sorular yüklenir.

### Testler

```bash
dotnet test
```

Testler aynı bağlantı ayarını kullanır ama kendi geçici veritabanlarını (`dotnetquest_tests_...`) oluşturup sonunda siler; asıl veritabanına dokunmaz.

### Tek uygulama olarak çalıştırma (production benzeri)

```bash
cd client && npm run build     # çıktı: src/DotnetQuest.Api/wwwroot
cd ../src/DotnetQuest.Api && dotnet run
```

Site artık doğrudan http://localhost:5080 adresinde.
`dotnet publish` (ve Visual Studio'daki Publish) React'i kendisi derler; yayın öncesi ayrıca `npm run build` gerekmez.

## Yayınlama (Render + Neon)

Site **[Render](https://render.com)**'da, veritabanı **[Neon](https://neon.com)**'da çalışır. İkisinin de ücretsiz planı var ve GitHub hesabıyla giriş yapılır.
Ücretsiz planın tek dezavantajı: 15 dakika kimse siteye girmezse Render uygulamayı uyutur, sonraki ilk ziyaret yaklaşık 1 dakika sürer.

### 1. Veritabanı (Neon)

1. https://neon.com → **Sign up** → **Continue with GitHub**
2. Yeni proje oluştur: adı `dotnetquest`, bölge **AWS Europe Central 1 (Frankfurt)**
3. Proje sayfasında **Connect** butonuna bas. **Connection pooling** seçeneğini kapat ve `postgresql://` ile başlayan
   bağlantı adresini kopyala. Bu adres şifre içerir: kimseyle paylaşma, repoya koyma.

### 2. Site (Render)

1. https://render.com → **Get Started** → **GitHub** ile giriş yap
2. **New** → **Web Service** → GitHub'daki `Dotnet-Quest` deposunu seç (Render depoya erişim izni ister, onayla)
3. Ayarlar:
   - **Name:** `dotnetquest` (site adresi `https://dotnetquest.onrender.com` gibi olur)
   - **Language:** Docker (repodaki `Dockerfile`'ı kendisi bulur)
   - **Branch:** `main`
   - **Region:** Frankfurt (Neon'la aynı bölge)
   - **Instance Type:** Free
4. **Environment Variables** bölümüne ekle:

   | Key | Value |
   |---|---|
   | `ConnectionStrings__Default` | Neon'dan kopyaladığın `postgresql://...` adresi |

5. **Advanced** → **Health Check Path**: `/health`
6. **Deploy Web Service**. İlk derleme birkaç dakika sürer; bitince sayfanın üstündeki adresten siteye girebilirsin.

Bundan sonra `main` dalına her `git push` yaptığında Render siteyi kendiliğinden yeniden derleyip yayınlar.
Bir sorun olursa Render'da servisin **Logs** sekmesi uygulamanın loglarını gösterir.

> Uygulama **tek instance** ile çalışmalı (ücretsiz planda zaten öyle). Rate limit sayaçları ve önbellek uygulamanın belleğinde
> tutulur; birden fazla instance olursa her biri kendi sayacını tutar. Ölçeklemek gerekirse önbellek için Redis gibi ortak bir depo gerekir.

## Oyun

| | |
|---|---|
| **Zorluklar** | Kolay (10 puan/soru), Orta (20), Zor (30) |
| **Kategoriler** | HTTP & Status Kodları, REST API Tasarımı, Dependency Injection, Async/Await, Middleware & Pipeline, EF Core & Veri, Bug Avı |
| **Soru tipleri** | Çoktan seçmeli, Bug Avı (hatalı satıra tıkla), Sıralama |
| **XP** | Tur bitince puanın kadar XP; hatasız turda %25 bonus. Misafirler oynayabilir ama XP sadece kayıtlı oyunculara yazılır |
| **Kısayollar** | `A-D` / `1-4` şık seçer, `Enter` cevaplar ve sonraki soruya geçer |

## Proje yapısı

```
src/DotnetQuest.Api/
  Program.cs                 Middleware pipeline (sıralama yorumlarıyla)
  Common/                    ProblemDetails, global exception handler, rate limiting
  Domain/                    Entity'ler ve iş kuralları (puanlama, XP, seviye)
  Data/                      DbContext, entity configuration'ları, migration'lar
  Data/Seed/*.json           TÜM SORULAR burada
  Features/                  Özellik bazlı (vertical slice) Minimal API endpoint'leri
    Sessions/                Oyun turu: başlat, getir, cevapla
    Categories/ Leaderboard/ Players/
tests/DotnetQuest.Api.Tests/ Integration (WebApplicationFactory) + unit testler
client/                      React + TypeScript + Vite
```

## Yeni soru eklemek

`src/DotnetQuest.Api/Data/Seed/` içindeki ilgili JSON dosyasına bir soru ekle ve API'yi yeniden başlat. Mevcut soruları düzeltmek de aynı şekilde çalışır (`key` alanıyla eşleşir).

```jsonc
{
  "key": "http-easy-06",            // benzersiz ve değişmez olmalı
  "difficulty": "Easy",             // Easy | Medium | Hard
  "type": "MultipleChoice",         // MultipleChoice | BugHunt | Ordering
  "prompt": "Soru metni",
  "options": ["A", "B", "C", "D"],  // Ordering'de DOĞRU sırayla yaz; oyuncuya karışık gösterilir
  "correctOption": 1,               // MultipleChoice: 0 tabanlı indeks (şıklar oyuncuya karışık gösterilir)
  "code": ["satır 1", "satır 2"],   // BugHunt: kod satırları
  "correctLine": 2,                 // BugHunt: 1 tabanlı satır numarası
  "explanation": "Neden doğru?"
}
```

Hatalı bir soru (ör. şıkların dışında `correctOption`) uygulamanın açılmasını engeller ve testler de bunu yakalar.

## Kodda uygulanan best practice'ler

| Konu | Nerede |
|---|---|
| Minimal API + `TypedResults` + `Results<...>` | `Features/*/*Endpoints.cs` |
| İstek/yanıt DTO'ları (entity dönmez, over-posting yok) | `SessionContracts.cs` |
| Doğru cevaplar sunucuda kontrol edilir, cevaplanmadan istemciye gitmez | `AnswerEvaluator.cs`, `SessionMapper.cs` |
| `201 Created` + `Location` header | `CreateSession` |
| RFC 9457 ProblemDetails, global `IExceptionHandler` | `Common/` |
| .NET 10 yerleşik validation (`AddValidation`) | `[Required]`, `[Range]` attribute'ları |
| Optimistic concurrency → `409 Conflict` | `GameSession.Version` |
| Transaction + atomik `ExecuteUpdate` | `SubmitAnswer` |
| `AsNoTracking`, `Select` projeksiyonu, index'ler | Endpoint'ler, `EntityConfigurations.cs` |
| `CancellationToken` her async çağrıda | Tüm endpoint'ler |
| `TimeProvider` (test edilebilir zaman) | `GameSession`, endpoint'ler |
| Rate limiting (`429` + `Retry-After`), kullanıcı/IP bazlı | `ApiExtensions.cs` |
| Output caching | Kategoriler, liderlik tablosu |
| HttpOnly cookie ile Identity (token'ı localStorage'da tutmaz) | `AddIdentityApiEndpoints` |
| Başkasının turuna `403` yerine `404` (varlık sızdırmaz) | `FindSessionAsync` |
| Health check | `/health` |
| Integration testler (gerçek PostgreSQL, test başına geçici veritabanı) | `SessionEndpointsTests.cs`, `ApiFactory.cs` |
| Şifreler repoda değil: yerelde user-secrets, sunucuda ortam değişkeni | `DatabaseExtensions.cs` |
| Çok aşamalı Docker build, root olmayan kullanıcıyla çalışan küçük imaj | `Dockerfile` |
| Proxy arkasında gerçek istemci IP'si ve HTTPS bilgisi (Forwarded Headers) | `ApiExtensions.cs`, `Program.cs` |
| Data Protection anahtarları veritabanında (yeniden deploy'da oturumlar kapanmaz) | `AppDbContext.cs`, `ApiExtensions.cs` |
| React build'i publish'e otomatik dahil | `DotnetQuest.Api.csproj` |

## Sonraki adım fikirleri

- **API Kaçış Odası:** Oyuncunun Scalar/curl ile gerçek HTTP istekleri atarak ilerlediği mod (JWT, ETag, sayfalama, 429...)
- Zamanlı "hızlı tur" modu ve günlük görev
- Rozetler ("Async Ustası", "Status Code Ninja")
- Kategori bazlı istatistik: en çok hangi konuda yanılıyorsun?
