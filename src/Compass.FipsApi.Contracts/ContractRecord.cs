using System.Text.Json;
using System.Text.Json.Serialization;

namespace Compass.FipsApi.Contracts;

// The generated records are read from COMPASS's controller source, not its documentation (which describes shapes the API does not send).
// Every member is nullable: the API builds responses from anonymous objects, so no type on its side guarantees a member.
// Never JsonUnmappedMemberHandling.Disallow: an additive COMPASS release must land in Unexpected, not fail here.

/// <summary>A record of one COMPASS response, or part of one.</summary>
public interface IContractRecord
{
    /// <summary>The members COMPASS sent that this record does not name; null when there were none.</summary>
    IDictionary<string, JsonElement>? Unexpected { get; }
}

/// <summary>How contract payloads are deserialised.</summary>
public static class CompassJson
{
    /// <summary>Web defaults, case-insensitive, an unnamed member kept in <see cref="IContractRecord.Unexpected"/> rather than rejected.</summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    };
}
