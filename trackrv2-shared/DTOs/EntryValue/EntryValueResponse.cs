namespace trackrv2_shared.DTOs;

public record EntryValueResponse(Guid Id, Guid FieldDefinitionId, string FieldLabel, FieldType FieldType, string Value, DateTime CreatedAt,
    DateTime LastUpdated);