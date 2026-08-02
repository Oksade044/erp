# ERP İşçi — Mobil Tətbiq (Android + iOS)

Şirkət əməkdaşları üçün .NET MAUI tətbiqi. ERP sistemi ilə **vahid**: eyni REST API
(`/api/v1`), eyni DTO kontraktları (`ERP.Shared.Contracts` birbaşa istinad olunur), JWT auth.
Ayrıca baza yoxdur — bütün əməliyyatlar real vaxtda ERP-də əks olunur.

## Arxitektura
- **Framework:** .NET MAUI (tək kod bazası → Android + iOS).
- **Kontraktlar:** `ERP.Shared.Contracts` layihəsinə ProjectReference (DRY — DTO təkrarı yoxdur).
- **API client:** `Services/MobileApiClient.cs` (masaüstü `ErpApiClient` nümunəsi).
- **Sessiya:** `Services/AppState.cs` — JWT token + server ünvanı (`Preferences`-də saxlanılır).
- **MVVM:** CommunityToolkit.Mvvm; Shell + TabBar naviqasiya.

## Ekranlar
| Ekran | Fayl | Təsvir |
|------|------|--------|
| Giriş | `Views/LoginPage` | admin yaratdığı hesabla (qeydiyyat yoxdur) + server ünvanı ayarı |
| Əsas | `Views/DashboardPage` | fərdi kartlar (bugün təhvil/qaytarma, aktiv/gözləyən, ay sifariş/dövriyyə); kartа toxununca siyahı |
| Sifarişlərim | `Views/MyOrdersPage` | yalnız öz sifarişlərim, gün/status süzgəci |
| Sifariş detalı | `Views/OrderDetailPage` | məhsullar, faktura, status dəyişmə, depozit, ödəniş, PDF |
| Yeni sifariş | `Views/NewOrderPage` | müştəri axtar/yarat + məhsul axtarışı + anbar seçimi + sətirlər |
| Maliyyəm | `Views/FinancePage` | yalnız öz dövriyyəm (gün/həftə/ay/il, icarə/satış) |
| Borcum | `Views/DebtPage` | təmsilçi/işçi kart borcu + hərəkət tarixçəsi (`/api/v1/me/debt`) |
| Profil | `Views/ProfilePage` | işçi məlumatları + çıxış |

Backend dəstəyi: `/api/v1/me/dashboard`, `/api/v1/me/finance`, `/api/v1/me/orders?filter=…`
— hamısı JWT-dəki istifadəçiyə görə süzülür (işçi yalnız öz işini görür, TDD §7).

## Server ünvanı (localdan VPS-ə keçid)
Tətbiq server ünvanını `AppState.BaseUrl`-də saxlayır (giriş ekranında "Server ayarları").
- **Android emulyator (lokal):** `http://10.0.2.2:5080` (host `localhost`-a yönəlir) — default.
- **Real cihaz (lokal şəbəkə):** `http://<PC-nin-IP>:5080`.
- **VPS (production):** `https://erp.sizindomen.az` (aşağıya bax).

Kodda heç nə dəyişmir — yalnız giriş ekranında ünvanı yazmaq kifayətdir.

## Build

Hədəflər `.csproj`-da platformaya görə **avtomatik** seçilir — heç bir əl dəyişikliyi lazım deyil:
- **Windows/Linux** (yalnız `maui-android` workload) → yalnız `net10.0-android` qurulur.
- **macOS** (Xcode + full `maui` workload) → `net10.0-android;net10.0-ios;net10.0-maccatalyst` avtomatik daxil olur.

### Android (bu mühitdə hazır — release APK verilir)
```
dotnet workload install maui-android            # bir dəfəlik
dotnet build src/Clients/ERP.Mobile/ERP.Mobile.csproj -f net10.0-android -c Release
# İmzalı APK: bin/Release/net10.0-android/az.erp.employee-Signed.apk
# Google Play üçün AAB: bin/Release/net10.0-android/az.erp.employee.aab
```
> Hazır imzalı APK: `D:\tmp\ERP-Isci.apk` (cihaza köçürüb quraşdırın; "naməlum mənbələr"ə icazə verin).

### iOS (macOS + Xcode + Apple Developer tələb olunur)
Windows-da mümkün deyil. Mac-də əlavə addım lazım DEYİL (multi-target avtomatikdir):
```
sudo dotnet workload install maui                # ios daxil (Mac-də)
# Simulyatorda işə salma:
dotnet build src/Clients/ERP.Mobile/ERP.Mobile.csproj -f net10.0-ios -t:Run
# Cihaz/AppStore üçün imzalı .ipa (Apple Developer hesabı + provisioning profile):
dotnet publish src/Clients/ERP.Mobile/ERP.Mobile.csproj -f net10.0-ios -c Release \
  -p:RuntimeIdentifier=ios-arm64 \
  -p:CodesignKey="Apple Distribution: <Şirkət> (<TeamId>)" \
  -p:CodesignProvision="<Provisioning Profile adı>"
# .ipa: bin/Release/net10.0-ios/ios-arm64/publish/*.ipa
```
Bundle Id: `az.erp.employee` (App Store Connect-də eyni Id ilə tətbiq yaradın).

## VPS Deployment (production)
1. Backend artıq hazırdır: `deploy/docker-compose.yml` (API + PostgreSQL + Redis).
2. VPS-də:
   ```
   POSTGRES_PASSWORD=... JWT_KEY=<ən azı 32 bayt> docker compose -f deploy/docker-compose.yml up -d
   ```
3. API-ni HTTPS ilə domenə bağlayın (Nginx/Caddy reverse proxy + Let's Encrypt).
4. Mobil tətbiqin giriş ekranında server ünvanını həmin HTTPS domeninə yazın.

## Push Bildirişlər (növbəti addım — xarici hesab tələb olunur)
Canlı push üçün:
- **Android:** Firebase Cloud Messaging (FCM) — `google-services.json` (sizin Firebase layihəniz).
- **iOS:** Apple Push Notification service (APNs) — sertifikat/key (Apple Developer).

Bu kredensiallar təqdim edildikdə: (a) backend-ə `IPushNotifier` + FCM göndərici,
(b) mobil tərəfə `Plugin.Firebase` alıcısı əlavə olunur. Hazırda tətbiq açılışda/refresh-də
məlumatı yeniləyir (pull); FCM qoşulanda hadisələr (yeni sifariş, təhvil vaxtı, ödəniş) push kimi gələcək.

## Məhdudiyyətlər (dizayn)
İşçi mobil tətbiqdə: məhsul əlavə/redaktə/sil, SKU/qiymət/anbar tərifini dəyişmə,
başqa işçinin sifariş/maliyyəsini görmə, istifadəçi/rol yaratma — **edə bilməz**.
Bunlar yalnız ERP admin panelindədir (permission-based RBAC + `/me` süzgəci).
