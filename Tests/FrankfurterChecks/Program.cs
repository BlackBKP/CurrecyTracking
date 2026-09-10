using System.Net;
using System.Text;
using CurrecyTracking.Services;

const string latest = """
{"base":"THB","date":"2026-07-13","rates":{"USD":101,"EUR":98,"JPY":102,"GBP":100,"CAD":99,"AUD":100}}
""";
const string history = """
{"base":"THB","start_date":"2026-07-06","end_date":"2026-07-13","rates":{
 "2026-07-13":{"USD":101,"EUR":98,"JPY":102,"GBP":100,"CAD":99},
 "2026-07-10":{"USD":100,"EUR":100,"JPY":100,"GBP":0,"CAD":100},
 "2026-07-09":{"USD":90,"GBP":100}}}
""";
void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
FrankfurterService Service(Func<string, string> respond) => new(new HttpClient(new FakeHandler(respond))
{
    BaseAddress = new Uri("https://example.test/v1/")
});
var service = Service(path =>
{
    if (path.Contains("latest?")) return latest;
    Check(path.Contains("2026-07-06..2026-07-13?base=THB"), "History window must use latest published date.");
    return history;
});
var codes = new[] { "USD", "EUR", "JPY", "GBP", "CAD", "AUD", "ZZZ", "THB" };
var rows = await service.GetMonitorRatesAsync(codes, default);
Check(rows[0].BaselineDate == new DateOnly(2026, 7, 10), "Monday must use Friday, excluding latest date.");
Check(rows[0].ChangePercent == 1 && !rows[0].ExceedsTolerance, "Exactly +1% must pass.");
Check(rows[1].ChangePercent == -2 && rows[1].ExceedsTolerance, "Below -1% must highlight.");
Check(rows[2].ChangePercent == 2 && rows[2].ExceedsTolerance, "Above +1% must highlight.");
Check(rows[3].ChangePercent == 0 && rows[3].BaselineDate == new DateOnly(2026, 7, 9), "Skip zero baseline.");
Check(rows[4].ChangePercent == -1 && !rows[4].ExceedsTolerance, "Exactly -1% must pass.");
Check(rows[5].Baseline == null && rows[5].Message != null, "Missing baseline must be explicit.");
Check(rows[6].Rate == null && rows[6].Message != null, "Unsupported currency must not fail other rows.");
Check(rows[7].Rate == 1 && rows[7].ChangePercent == null, "THB needs no tolerance check.");
var repeated = await service.GetMonitorRatesAsync(codes, default);
Check(repeated[2].ChangePercent == rows[2].ChangePercent, "Repeat refresh must use historical baseline.");
foreach (var failure in new Exception[] { new HttpRequestException("Network"), new TaskCanceledException("Timeout"), new System.Text.Json.JsonException("Invalid JSON") })
{
    var partial = Service(path => path.Contains("latest?") ? latest : throw failure);
    var result = await partial.GetMonitorRatesAsync(new[] { "USD" }, default);
    Check(result[0].Rate == 101 && result[0].Baseline == null && result[0].Message != null, "History failure must preserve latest with warning.");
    var failed = Service(_ => throw failure);
    try { await failed.GetMonitorRatesAsync(new[] { "USD" }, default); throw new Exception("Expected failure"); }
    catch (Exception ex) when (ReferenceEquals(ex, failure)) { }
}
Console.WriteLine("PASS: baseline dates, weekend, +/-1% boundaries, repeat refresh, zero/missing baseline, unsupported code, THB, network/timeout/JSON errors.");

sealed class FakeHandler(Func<string, string> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(respond(request.RequestUri!.PathAndQuery), Encoding.UTF8, "application/json")
        });
    }
}
