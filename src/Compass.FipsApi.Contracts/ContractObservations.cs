using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Compass.FipsApi.Contracts;

/// <summary>
/// Records, once per endpoint and field, every member COMPASS sent that a contract record did not
/// name - the way an additive COMPASS release announces itself here without breaking anything.
/// Bounded: a pair is remembered once, not per request.
/// </summary>
public interface IContractObservations
{
    /// <summary>Walks the record and anything it contains, noting unexpected members against the endpoint.</summary>
    void Observe(string endpoint, object? record);

    /// <summary>Every (endpoint, field) pair seen so far, for a diagnostics page.</summary>
    IReadOnlyCollection<(string Endpoint, string Field)> Seen { get; }
}

public sealed class ContractObservations : IContractObservations
{
    private readonly ConcurrentDictionary<(string Endpoint, string Field), byte> _seen = new();
    private readonly ILogger<ContractObservations> _logger;

    public ContractObservations(ILogger<ContractObservations> logger) => _logger = logger;

    public IReadOnlyCollection<(string Endpoint, string Field)> Seen => _seen.Keys.ToArray();

    public void Observe(string endpoint, object? record)
    {
        var fresh = new List<string>();
        Walk(endpoint, record, "", fresh, depth: 0);
        if (fresh.Count > 0)
        {
            _logger.LogInformation("COMPASS {Endpoint} carries fields this consumer does not read: {Fields}", endpoint, string.Join(", ", fresh));
        }
    }

    private void Walk(string endpoint, object? value, string path, List<string> fresh, int depth)
    {
        if (value is null || depth > 6) return;

        if (value is IContractRecord record)
        {
            if (record.Unexpected is { Count: > 0 } unexpected)
            {
                foreach (var name in unexpected.Keys)
                {
                    var field = path.Length == 0 ? name : $"{path}.{name}";
                    if (_seen.TryAdd((endpoint, field), 0)) fresh.Add(field);
                }
            }
            foreach (var property in value.GetType().GetProperties())
            {
                if (property.Name == nameof(IContractRecord.Unexpected) || property.GetIndexParameters().Length > 0) continue;
                var child = property.GetValue(value);
                var childPath = path.Length == 0 ? property.Name : $"{path}.{property.Name}";
                Walk(endpoint, child, childPath, fresh, depth + 1);
            }
            return;
        }

        if (value is System.Collections.IEnumerable items && value is not string && value is not JsonElement)
        {
            // Every item of a list is checked, but all items share one path: a field is a fact about the
            // list's element shape, not about one position in it.
            foreach (var item in items) Walk(endpoint, item, path + "[]", fresh, depth + 1);
        }
    }
}
