namespace trackrv2_shared.DTOs;

public record FieldDefinitionResponse(
    Guid Id,
    string Label,
    string Description,
    FieldType Type,
    DateTime CreatedAt,
    DateTime LastUpdated
);
