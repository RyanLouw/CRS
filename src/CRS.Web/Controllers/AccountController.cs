using System.Security.Claims;
using CRS.Core.Demo;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRS.Controllers;

public sealed class AccountController(IWebHostEnvironment environment, IConfiguration configuration) : Controller
{
    private bool Demo => environment.IsDevelopment() && configuration.GetValue<bool>("Demo:Enabled");
    [AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Calls");
        return Demo ? View() : Challenge(new AuthenticationProperties { RedirectUri = "/Calls" }, OpenIdConnectDefaults.AuthenticationScheme);
    }
    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> DemoLogin(string role)
    {
        if (!Demo) return NotFound();
        if (role is not ("Operator" or "Admin" or "PSP")) return BadRequest();
        var id = role == "Admin" ? DemoServices.AdminId : DemoServices.OperatorId;
        var identity = new ClaimsIdentity([new(ClaimTypes.NameIdentifier, id.ToString()), new(ClaimTypes.Name, "Demo " + role),
            new(ClaimTypes.Role, role)], CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return RedirectToAction("Index", role == "Admin" ? "Admin" : "Calls");
    }
    [HttpPost, Authorize]
    public async Task<IActionResult> Logout()
    {
        if (!Demo) return SignOut(new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
    [AllowAnonymous]
    public IActionResult Denied() { Response.StatusCode = 403; return View(); }

    [Authorize(Policy = "Recordings"), HttpGet("/demo/recording")]
    public IActionResult DemoRecording()
    {
        if (!Demo) return NotFound();
        // A short synthetic tone: no caller recording or external download is used.
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        const int samples = 8000;
        writer.Write("RIFF"u8); writer.Write(36 + samples * 2); writer.Write("WAVEfmt "u8);
        writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(8000);
        writer.Write(16000); writer.Write((short)2); writer.Write((short)16);
        writer.Write("data"u8); writer.Write(samples * 2);
        for (var i = 0; i < samples; i++) writer.Write((short)(Math.Sin(i * 2 * Math.PI * 440 / 8000) * 1500));
        return File(stream.ToArray(), "audio/wav", "demo-recording.wav");
    }
}
