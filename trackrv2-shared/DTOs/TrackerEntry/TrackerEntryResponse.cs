namespace trackrv2_shared.DTOs;

public record TrackerEntryResponse(Guid Id, Guid TrackerId, List<EntryValueResponse> Values, DateTime CreatedAt,
    DateTime LastUpdated);