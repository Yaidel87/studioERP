using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace StudioERP.Web.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _js;

    public CustomAuthStateProvider(IJSRuntime js)
    {
        _js = js;
    }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _js.InvokeAsync<string>("leerToken");
            
            if (string.IsNullOrEmpty(token))
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var claims = jwtToken.Claims.ToList();
            
            // --- AGREGA EL NOMBRE (Nickname) ---
            if (!claims.Any(c => c.Type == ClaimTypes.Name))
            {
                var nickname = jwtToken.Claims.FirstOrDefault(c => c.Type == "nickname")?.Value 
                            ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value 
                            ?? jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value 
                            ?? "Usuario";
                claims.Add(new Claim(ClaimTypes.Name, nickname));
            }

            // --- AGREGA EL ROL (Vital para IsInRole) ---
            if (!claims.Any(c => c.Type == ClaimTypes.Role))
            {
                var rol = jwtToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
                if (!string.IsNullOrEmpty(rol))
                {
                    claims.Add(new Claim(ClaimTypes.Role, rol));
                }
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public void NotifyUserAuthentication(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var claims = jwtToken.Claims.ToList();

            if (!claims.Any(c => c.Type == ClaimTypes.Name))
            {
                var nickname = jwtToken.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value 
                            ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value 
                            ?? "Usuario";
                claims.Add(new Claim(ClaimTypes.Name, nickname));
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);
            
            // Notificamos a Blazor que el estado de autenticación ha cambiado
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }
        catch
        {
            NotifyUserLogout();
        }
    }

    public void NotifyUserLogout()
    {
        // Limpiamos la identidad (usuario anónimo)
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
    }
}