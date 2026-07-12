namespace trackrv2_shared.DTOs;

public record FieldDefinitionRequest(
    Guid Id,
    string Label,
    string Description,
    FieldType Type,
    IEnumerable<EntryValueResponse> EntryValues
);
