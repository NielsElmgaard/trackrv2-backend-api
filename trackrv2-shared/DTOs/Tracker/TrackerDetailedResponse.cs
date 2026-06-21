namespace trackrv2_shared.DTOs;

public record TrackerDetailedResponse
(Guid Id, string Name,Guid UserId, DateTime CreatedAt,
    DateTime LastUpdated, List<FieldDefinitionResponse> Fields);