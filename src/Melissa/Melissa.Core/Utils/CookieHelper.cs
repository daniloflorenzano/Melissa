using Microsoft.Playwright;

public class CookieHelper
{
    public static async Task<string> GetCookiesAsync(string url)
    {
        using var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(url);

        var acceptButton = await page.QuerySelectorAsync("button:has-text('Aceitar tudo')")
                           ?? await page.QuerySelectorAsync("button:has-text('Accept all')");

        if (acceptButton != null)
        {
            await acceptButton.ClickAsync();
            await page.WaitForTimeoutAsync(2000);
        }

        var cookies = await context.CookiesAsync();
        await browser.CloseAsync();
        
        var cookieString = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
        return cookieString;
    }

}