// A stand-in for the COMPASS service-register API, chosen by a path prefix so the application needs no change:
//
//   dotnet run --project src/Compass.FipsApi.Stub --urls http://127.0.0.1:1339
//   Compass__BaseUrl=http://127.0.0.1:1339/seeded/   Compass__ApiToken=any
//
// Scenarios: `seeded` and `drift` are files under scenarios/<scenario>/, served verbatim (never re-serialised, so
// every member the recording carries reaches the client); `empty` answers as a COMPASS holding nothing (every
// collection empty, any product id unknown); `unavailable` answers 503. A path with no file answers 404 naming the
// file that would have answered; a path that climbs out of the scenario folder answers 404 too.
using Compass.FipsApi.Stub;

var app = WebApplication.CreateBuilder(args).Build();
var scenarios = new Scenarios(Path.Combine(app.Environment.ContentRootPath, "scenarios"));

app.Map("/{scenario}/{**path}", (string scenario, string? path) =>
{
    var answer = scenarios.Answer(scenario, path ?? "");
    Console.WriteLine($"{scenario} /{path} -> {answer.Status}");
    return Results.Content(answer.Body, "application/json", statusCode: answer.Status);
});

app.Run();
