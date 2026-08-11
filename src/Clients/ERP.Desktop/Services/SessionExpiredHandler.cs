using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ERP.Desktop.Services;

/// <summary>
/// Token vaxtı bitəndə (401) istifadəçini təmiz şəkildə giriş ekranına qaytarır — qarışıq
/// "gözlənilməz xəta" əvəzinə. auth/login və auth/refresh 401-ləri (yanlış parol) istisna edilir.
/// </summary>
public sealed class SessionExpiredHandler : DelegatingHandler
{
    /// <summary>401 baş verəndə çağırılır (App bunu giriş ekranına keçidə bağlayır).</summary>
    public static Action? OnUnauthorized;

    public SessionExpiredHandler() : base(new HttpClientHandler()) { }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var resp = await base.SendAsync(request, ct);
        if (resp.StatusCode == HttpStatusCode.Unauthorized)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (!path.Contains("/auth/login", StringComparison.OrdinalIgnoreCase)
                && !path.Contains("/auth/refresh", StringComparison.OrdinalIgnoreCase))
            {
                OnUnauthorized?.Invoke();
            }
        }
        return resp;
    }
}
