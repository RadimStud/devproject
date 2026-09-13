using System.Net;
using System.Text;
using System.Text.Json;
using MiniLottery.Core;
using MiniLottery.Warrior;

internal static class WarriorChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        int count = 0;
        void Check(bool value, string message) { check(value, message); count++; }
        void Reject(Action action, string message)
        {
            bool rejected = false;
            try { action(); } catch (ArgumentException) { rejected = true; } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, message);
        }
        var values = new[] { 2, 2, 2, 2, 2, 2 };
        var balanced = new WarriorBuild(values);
        values[0] = 5;
        Check(balanced[WarriorPerk.Strength] == 2, "Build copies input values");
        foreach (var invalid in new[] { new[] { 1, 1 }, new[] { 0, 0, 0, 0, 0, 0 }, new[] { 6, 2, 1, 1, 1, 1 }, new[] { -1, 5, 2, 2, 2, 2 } })
            Reject(() => new WarriorBuild(invalid), "Invalid perk build rejected");
        foreach (decimal invalid in new[] { 0m, -.01m, .001m, 1000000.01m })
            Reject(() => new WarriorMatch(balanced, invalid, new Random(1)), "Invalid stake rejected");
        var outcomes = new HashSet<RoundWinner>();
        for (int seed = 0; seed < 150; seed++)
        {
            var match = new WarriorMatch(balanced, .25m, new Random(seed));
            var alternative = new WarriorMatch(new WarriorBuild(new[] { 5, 5, 2, 0, 0, 0 }), .25m, new Random(seed));
            Check(WarriorBuild.Perks.Sum(p => match.Computer[p.Id]) == 12 && WarriorBuild.Perks.All(p => match.Computer[p.Id] is >= 0 and <= 5), "PC has the same valid budget");
            Check(match.Computer.Describe() == alternative.Computer.Describe(), "PC does not counter-pick player perks");
            Reject(() => match.TakePayout(), "Cannot withdraw before five rounds");
            for (int n = 1; n <= 5; n++)
            {
                int hp = match.PlayerHealth, cp = match.ComputerHealth;
                var round = match.PlayRound();
                Check(round.Number == n && round.PlayerHealth == hp - round.PlayerDamage && round.ComputerHealth == cp - round.ComputerDamage, "Health persists between rounds");
                Check(round.PlayerHealth >= 0 && round.ComputerHealth >= 0 && (n == 5 || round.PlayerHealth > 0 && round.ComputerHealth > 0), "Combat permits all five rounds without negative health");
                Check(round.Winner == (round.PlayerPower > round.ComputerPower ? RoundWinner.Player : round.PlayerPower < round.ComputerPower ? RoundWinner.Computer : RoundWinner.Draw), "Round winner agrees with powers");
                string prompt = BattleImagePrompt.Create(match, round);
                Check(prompt.Contains(round.Story) && prompt.Contains(round.Scenario.Environment), "Image uses the resolved story and scenario");
                string outcome = round.Winner == RoundWinner.Player ? "teal player knight wins" : round.Winner == RoundWinner.Computer ? "crimson computer knight wins" : "A tied exchange";
                Check(prompt.Contains(outcome), "Illustration identifies the actual round winner");
            }
            Check(match.Rounds.Select(r => r.Scenario.Id).Distinct().Count() == 5, "Five distinct scenarios");
            Check(match.State == WarriorMatchState.Completed && match.PlayerWins == match.Rounds.Count(r => r.Winner == RoundWinner.Player), "Match completes after five rounds");
            outcomes.Add(match.Winner);
            decimal payout = match.TakePayout();
            Check(payout == (match.Winner == RoundWinner.Player ? .50m : match.Winner == RoundWinner.Draw ? .25m : 0m), "Exact win, draw or loss payout");
            Reject(() => match.TakePayout(), "No double payout");
            Reject(() => match.PlayRound(), "No sixth round");
            match.Forfeit();
            Check(match.State == WarriorMatchState.Completed, "Forfeit cannot change a completed result");
        }
        Check(outcomes.Count == 3, "Seeded runs cover wins, losses and draws");
        var abandoned = new WarriorMatch(balanced, 1.25m, new Random(9));
        abandoned.PlayRound(); abandoned.Forfeit(); abandoned.Forfeit();
        Check(abandoned.TakePayout() == 0 && abandoned.State == WarriorMatchState.Forfeited, "Forfeit loses only the reserved stake");
        Reject(() => abandoned.PlayRound(), "Forfeited match cannot resume");
        Reject(() => abandoned.TakePayout(), "Forfeit cannot pay twice");
        var scene = WarriorMatch.Scenarios[0];
        Check(WarriorMatch.Power(new WarriorBuild(new[] { 5, 2, 2, 2, 1, 0 }), scene, 10, 100) > WarriorMatch.Power(balanced, scene, 10, 100), "Scenario perks affect combat");

        int calls = 0;
        using var client = new HttpClient(new Handler(async (request, _) =>
        {
            calls++;
            Check(request.RequestUri?.AbsoluteUri == "https://api.openai.com/v1/images/generations" && request.Method == HttpMethod.Post, "Image request targets the official endpoint");
            Check(request.Headers.Authorization?.ToString() == "Bearer test-key", "Key sent only as bearer authentication");
            string body = await request.Content!.ReadAsStringAsync();
            using var json = JsonDocument.Parse(body);
            Check(json.RootElement.GetProperty("model").GetString() == OpenAiBattleImages.Model && json.RootElement.GetProperty("n").GetInt32() == 1 && !body.Contains("test-key"), "One image requested without leaking key into prompt");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"data\":[{\"b64_json\":\"AQID\"}]}") };
        }));
        var service = new OpenAiBattleImages(client);
        Check((await service.GenerateAsync("test-key", "resolved story", default)).SequenceEqual(new byte[] { 1, 2, 3 }) && calls == 1, "Image bytes decoded once");
        try { await service.GenerateAsync("", "story", default); throw new Exception("Missing key accepted"); }
        catch (InvalidOperationException) { Check(calls == 1, "Missing key sends no request"); }
        foreach (var status in new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.TooManyRequests, HttpStatusCode.InternalServerError })
        {
            int failures = 0;
            using var badClient = new HttpClient(new Handler((_, _) => { failures++; return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent("secret-response") }); }));
            try { await new OpenAiBattleImages(badClient).GenerateAsync("test-key", "story", default); throw new Exception("Failure accepted"); }
            catch (InvalidOperationException ex) { Check(failures == 1 && !ex.Message.Contains("secret-response") && !ex.Message.Contains("test-key"), "API error sanitized without paid retry"); }
        }
        foreach (string body in new[] { "{\"data\":[]}", "{\"data\":[{\"b64_json\":\"!invalid!\"}]}" })
        {
            using var malformed = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") })));
            try { await new OpenAiBattleImages(malformed).GenerateAsync("test-key", "story", default); throw new Exception("Malformed image accepted"); }
            catch (InvalidDataException) { Check(true, "Malformed response rejected"); }
        }
        using var cancel = new CancellationTokenSource();
        using var slow = new HttpClient(new Handler(async (_, token) => { cancel.Cancel(); await Task.Delay(Timeout.Infinite, token); return new HttpResponseMessage(); }));
        try { await new OpenAiBattleImages(slow).GenerateAsync("test-key", "story", cancel.Token); throw new Exception("Cancellation ignored"); }
        catch (OperationCanceledException) { Check(true, "Image generation can be cancelled"); }
        Console.WriteLine($"PASS: {count} Warrior checks (five rounds, perks, settlement, prompts and mocked image API).");
    }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
