namespace trackrv2_shared.DTOs;

public record FieldDefinitionResponse(Guid Id, string Label, FieldType Type, DateTime CreatedAt,
    DateTime LastUpdated);