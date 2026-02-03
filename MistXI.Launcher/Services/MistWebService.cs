namespace MistXI.Launcher.Services;

public sealed class MistWebService
{
    private readonly HttpClient _http;

    public MistWebService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("MistXI-Launcher/1.0");
        _http.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<int?> GetPlayersOnlineAsync(CancellationToken ct)
    {
        var urls = new[]
        {
            "https://api.mistxi.com/status.php",
            "http://api.mistxi.com/status.php",
        };

        foreach (var url in urls)
        {
            try
            {
                var text = await _http.GetStringAsync(url, ct);

                // Try JSON first:
                try
                {
                    using var doc = JsonDocument.Parse(text);
                    var root = doc.RootElement;

                    // Your API: {"onlinePlayers":0,...}
                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        if (root.TryGetProperty("onlinePlayers", out var op) && op.ValueKind == JsonValueKind.Number)
                            return op.GetInt32();

                        // Common fallbacks:
                        foreach (var name in new[] { "players_online", "playersOnline", "online", "players", "count" })
                        {
                            if (root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number)
                                return v.GetInt32();
                        }

                        // Sometimes nested:
                        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                        {
                            if (data.TryGetProperty("onlinePlayers", out var op2) && op2.ValueKind == JsonValueKind.Number)
                                return op2.GetInt32();

                            foreach (var name in new[] { "players_online", "playersOnline", "online", "players", "count" })
                            {
                                if (data.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number)
                                    return v.GetInt32();
                            }
                        }
                    }
                }
                catch
                {
                    // Not JSON.
                }

                // If plain number:
                if (int.TryParse(text.Trim(), out var n))
                    return n;

                // If "Players Online: 123" (or any first number)
                var m = Regex.Match(text, @"(\d+)");
                if (m.Success && int.TryParse(m.Groups[1].Value, out var n2))
                    return n2;
            }
            catch
            {
                // try next
            }
        }

        return null;
    }

    public async Task<Announcement?> GetLatestAnnouncementAsync(CancellationToken ct)
    {
        // Prefer RSS/Atom feeds (Hugo commonly exposes /news/index.xml)
        var feedUrls = new[]
        {
            "https://mistxi.com/news/index.xml",
            "https://mistxi.com/news/index.xml/",
            "https://mistxi.com/index.xml",
            "https://mistxi.com/rss.xml",
        };

        foreach (var url in feedUrls)
        {
            try
            {
                var xml = await _http.GetStringAsync(url, ct);
                var ann = TryParseRssOrAtom(xml);
                if (ann != null) return ann;
            }
            catch
            {
                // try next
            }
        }

        // Fallback: scrape /news/ page for first link
        try
        {
            var html = await _http.GetStringAsync("https://mistxi.com/news/", ct);
            var m = Regex.Match(html, "href=\"(?<u>/news/[^\"]+)\"", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var rel = m.Groups["u"].Value;
                var full = "https://mistxi.com" + rel;
                var titleMatch = Regex.Match(html,
                    "href=\"" + Regex.Escape(rel) + "\"[^>]*>(?<t>[^<]{3,120})<",
                    RegexOptions.IgnoreCase);
                var title = titleMatch.Success
                    ? WebUtility.HtmlDecode(titleMatch.Groups["t"].Value.Trim())
                    : "Latest News";
                return new Announcement(title, null, full, null);
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static Announcement? TryParseRssOrAtom(string xml)
    {
        var xdoc = XDocument.Parse(xml);

        // RSS 2.0: /rss/channel/item
        var item = xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "item");
        if (item != null)
        {
            string? title = item.Elements().FirstOrDefault(e => e.Name.LocalName == "title")?.Value?.Trim();
            string? link = item.Elements().FirstOrDefault(e => e.Name.LocalName == "link")?.Value?.Trim();
            string? desc = item.Elements().FirstOrDefault(e => e.Name.LocalName == "description")?.Value?.Trim();
            string? pub = item.Elements().FirstOrDefault(e => e.Name.LocalName == "pubDate")?.Value?.Trim();
            DateTimeOffset? dt = null;
            if (DateTimeOffset.TryParse(pub, out var dto)) dt = dto;
            if (!string.IsNullOrWhiteSpace(title))
                return new Announcement(title!, dt, link, CleanSnippet(desc));
        }

        // Atom: /feed/entry
        var entry = xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "entry");
        if (entry != null)
        {
            string? title = entry.Elements().FirstOrDefault(e => e.Name.LocalName == "title")?.Value?.Trim();
            string? summary = entry.Elements().FirstOrDefault(e => e.Name.LocalName == "summary")?.Value?.Trim();
            string? updated = entry.Elements().FirstOrDefault(e => e.Name.LocalName == "updated")?.Value?.Trim();
            DateTimeOffset? dt = null;
            if (DateTimeOffset.TryParse(updated, out var dto)) dt = dto;
            string? link = entry.Elements().FirstOrDefault(e => e.Name.LocalName == "link")?.Attribute("href")?.Value
                           ?? entry.Elements().FirstOrDefault(e => e.Name.LocalName == "id")?.Value?.Trim();
            if (!string.IsNullOrWhiteSpace(title))
                return new Announcement(title!, dt, link, CleanSnippet(summary));
        }

        return null;
    }

    private static string? CleanSnippet(string? htmlOrText)
    {
        if (string.IsNullOrWhiteSpace(htmlOrText)) return null;
        var t = Regex.Replace(htmlOrText, "<.*?>", " ");
        t = WebUtility.HtmlDecode(t);
        t = Regex.Replace(t, @"\s+", " ").Trim();
        if (t.Length > 220) t = t[..220] + "…";
        return t;
    }
}

public sealed record Announcement(string Title, DateTimeOffset? Date, string? Url, string? Summary);
