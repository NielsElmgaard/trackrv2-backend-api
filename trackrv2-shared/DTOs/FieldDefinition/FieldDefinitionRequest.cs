namespace trackrv2_shared.DTOs;

public record FieldDefinitionRequest(string Label, FieldType Type,IEnumerable<EntryValueResponse> EntryValues);