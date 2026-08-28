// A stand-in for the content source the application reads.
//
// Today it has one behaviour: every request, whatever its path or method, is answered with 200 and
// an empty collection in the shape the content API returns. The application then renders every
// page with no data instead of spending its retry policy (2 + 4 + 8 seconds) on each failing call,
// so a run of the browser-driven suite against it is quick, and every failure the suite reports is
// about the pages rather than about time-outs or data.
//
//   dotnet run --project tests/FipsFrontend.Tests.StubCmsApi --urls http://127.0.0.1:1338
//
// and point the application at it with CmsApi__BaseUrl=http://127.0.0.1:1338/api.
//
// Scenarios with content are the next step: chosen by a path prefix on that base URL (so the
// application needs no change and one instance serves several scenarios), from fixture files the
// in-process tests read as well.

var app = WebApplication.CreateBuilder(args).Build();

app.Map("{**path}", (string? path) =>
{
    Console.WriteLine($"/{path}");
    return Results.Json(new
    {
        data = Array.Empty<object>(),
        meta = new { pagination = new { page = 1, pageSize = 25, pageCount = 0, total = 0 } },
    });
});

app.Run();
