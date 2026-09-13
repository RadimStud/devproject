using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MiniLottery.Warrior;

public sealed class OpenAiBattleImages
{
    public const string Model = "gpt-image-2";
    private readonly HttpClient client;
    public OpenAiBattleImages(HttpClient client) => this.client = client;

    public async Task<byte[]> GenerateAsync(string apiKey, string prompt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) throw new InvalidOperationException("Nejdřív nastavte vlastní OpenAI API klíč.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(150));
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/generations");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        request.Content = JsonContent.Create(new { model = Model, prompt, n = 1, size = "1536x1024", quality = "low", output_format = "png" });
        HttpResponseMessage response;
        try { response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, timeout.Token); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new InvalidOperationException("Generování překročilo 150 sekund. Zůstává místní ilustrace."); }
        catch (HttpRequestException)
        { throw new InvalidOperationException("Obrázková služba není dostupná. Zkontrolujte připojení."); }
        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(response.StatusCode switch {
                    HttpStatusCode.Unauthorized => "API klíč nebyl přijat. Zkontrolujte nastavení obrázků.",
                    HttpStatusCode.Forbidden => "API účet nemá přístup k obrázkovému modelu.",
                    HttpStatusCode.TooManyRequests => "API hlásí vyčerpaný kredit nebo limit požadavků.",
                    HttpStatusCode.BadRequest => "API odmítlo zadání obrázku. Zůstává místní ilustrace.",
                    _ => $"Obrázková služba odpověděla chybou {(int)response.StatusCode}. Zůstává místní ilustrace."
                });
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);
            if (!json.RootElement.TryGetProperty("data", out var data) || data.GetArrayLength() != 1 ||
                !data[0].TryGetProperty("b64_json", out var encoded))
                throw new InvalidDataException("Služba nevrátila očekávaný obrázek.");
            string? value = encoded.GetString();
            if (string.IsNullOrWhiteSpace(value) || value.Length > 24000000)
                throw new InvalidDataException("Obrázek je prázdný nebo příliš velký.");
            try { return Convert.FromBase64String(value); }
            catch (FormatException) { throw new InvalidDataException("Služba vrátila neplatná obrazová data."); }
        }
    }
}
