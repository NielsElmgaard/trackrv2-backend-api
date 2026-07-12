namespace trackrv2_shared.DTOs;

public record EntryValueResponse(
    Guid Id,
    Guid FieldDefinitionId,
    string FieldLabel,
    string FieldDescription,
    FieldType FieldType,
    string Value,
    DateTime CreatedAt,
    DateTime LastUpdated
);
