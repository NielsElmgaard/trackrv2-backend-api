namespace trackrv2_shared.DTOs;

public record TrackerRequest(
    Guid? TrackerId,
    string Name,
    string? Description,
    bool IsPublic,
    List<FieldDefinitionRequest> Fields
);
