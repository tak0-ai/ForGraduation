using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace RuralTourism.Web.Client.Services;

public class BanCheckHandler : DelegatingHandler
{
    private readonly IJSRuntime _js;
    private readonly NavigationManager _nav;
    private readonly IServiceProvider _sp;
    private bool _alerted = false;

    public BanCheckHandler(IJSRuntime js, NavigationManager nav, IServiceProvider sp)
    {
        _js = js;
        _nav = nav;
        _sp = sp;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden && response.Headers.Contains("X-Account-Banned") && !_alerted)
        {
            _alerted = true;
            try
            {
                var authService = (AuthService)_sp.GetService(typeof(AuthService))!;
                if (authService != null)
                {
                    await authService.LogoutAsync();
                }
                await _js.InvokeVoidAsync("alert", "安全拦截：您的账户已处于封禁状态。请联系管理员解除后重试。");
                _nav.NavigateTo("/", forceLoad: true);
            }
            catch {}
            finally
            {
                _alerted = false;
            }
        }
        
        return response;
    }
}
