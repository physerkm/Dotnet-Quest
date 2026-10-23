# DotNet Quest 🎮

Oynayarak **.NET Web API ve best practice'leri** öğreten bir oyun sitesi.
Her turda seçtiğin kategori ve zorlukta 5 soru gelir; her cevaptan sonra *neden* doğru ya da yanlış olduğunu anlatan bir açıklama görürsün.

Projenin kendisi de bir ders: backend, oyunda anlatılan best practice'lerle yazıldı.

## Çalıştırma

Gerekenler: **.NET 10 SDK** ve **Node.js 20+**

İki terminal aç:

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

> Veritabanı (SQLite, `dotnetquest.db`) ilk açılışta otomatik oluşturulur ve sorular yüklenir.

### Testler

```bash
dotnet test
```

### Tek uygulama olarak çalıştırma (production benzeri)

```bash
cd client && npm run build     # çıktı: src/DotnetQuest.Api/wwwroot
cd ../src/DotnetQuest.Api && dotnet run
```

Site artık doğrudan http://localhost:5080 adresinde.

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
| Integration testler | `SessionEndpointsTests.cs` |

## Sonraki adım fikirleri

- **API Kaçış Odası:** Oyuncunun Scalar/curl ile gerçek HTTP istekleri atarak ilerlediği mod (JWT, ETag, sayfalama, 429...)
- Zamanlı "hızlı tur" modu ve günlük görev
- Rozetler ("Async Ustası", "Status Code Ninja")
- Kategori bazlı istatistik: en çok hangi konuda yanılıyorsun?
