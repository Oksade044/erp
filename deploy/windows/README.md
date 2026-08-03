# ERP Sistemi — Windows quraşdırıcısı (setup.exe)

Masaüstü tətbiqi (Avalonia) **self-contained** paketlənir: bütün .NET 10 runtime + kitabxanalar
setup daxilindədir, hədəf kompüterdə **heç nə (o cümlədən .NET) quraşdırmaq lazım deyil**.
Tətbiq açılışda **konsol/kod göstərmir** (`OutputType=WinExe`) və defolt olaraq **VPS API-yə**
qoşulur (`https://186.240.145.239.sslip.io`).

## Qurma (setup.exe yaratmaq)

Tələb: .NET 10 SDK + [Inno Setup 6+](https://jrsoftware.org/isdl.php).

```powershell
# 1) Self-contained publish
dotnet publish src/Clients/ERP.Desktop/ERP.Desktop.csproj -c Release -r win-x64 `
  --self-contained true -o publish/desktop

# 2) Server ünvanı (istəyə görə dəyiş — default onsuz da VPS-dir)
"https://186.240.145.239.sslip.io" | Out-File -Encoding ascii -NoNewline publish/desktop/server.url

# 3) Installer-i qur
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" `
  /DPublishDir="$(Resolve-Path publish/desktop)" deploy/windows/ERP-Setup.iss

# Nəticə: deploy/windows/Output/ERP-Setup.exe
```

## Quraşdırma (son istifadəçi)
`ERP-Setup.exe` → "Növbəti" → quraşdır (Program Files\ERP Sistemi). Başlanğıc menyusu + (istəyə görə)
masaüstü qısayolu yaranır. İlk açılışda giriş: **admin / Admin123!** (dərhal dəyişin).

## Server ünvanını dəyişmək (rebuild olmadan)
Tətbiq API ünvanını bu ardıcıllıqla həll edir:
1. `ERP_API_URL` mühit dəyişəni
2. exe qovluğundakı `server.url` faylı (`C:\Program Files\ERP Sistemi\server.url`)
3. default `https://186.240.145.239.sslip.io`

Başqa serverə keçmək üçün `server.url` faylını redaktə edin (məs. domen + HTTPS).
