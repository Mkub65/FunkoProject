﻿using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace FunkoProject.Web.Services;

public class AuthenticationCustomProvider: AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;
    private readonly NavigationManager _navigationManager;
    private ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public AuthenticationCustomProvider(ILocalStorageService localStorage, HttpClient httpClient, NavigationManager navigationManager)
    {
        _localStorage = localStorage;
        _httpClient = httpClient;
        _navigationManager = navigationManager;
    }
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("accessToken");
        if (string.IsNullOrWhiteSpace(token))
        {
            return new AuthenticationState(_anonymous);
        }

        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwtAuthType");
        var user = new ClaimsPrincipal(identity);

        return new AuthenticationState(user);
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        return keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()));
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        base64 = base64.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
    
}