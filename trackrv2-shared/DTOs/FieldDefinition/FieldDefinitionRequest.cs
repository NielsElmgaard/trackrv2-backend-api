namespace trackrv2_shared.DTOs;

public record FieldDefinitionRequest(
    Guid Id,
    string Label,
    FieldType Type,
    IEnumerable<EntryValueResponse> EntryValues
);
