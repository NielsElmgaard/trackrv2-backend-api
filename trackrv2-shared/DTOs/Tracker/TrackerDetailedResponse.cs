namespace trackrv2_shared.DTOs;

public record TrackerDetailedResponse(
    Guid Id,
    string Name,
    string Description,
    Guid UserId,
    DateTime CreatedAt,
    DateTime LastUpdated,
    List<FieldDefinitionResponse> Fields
);
