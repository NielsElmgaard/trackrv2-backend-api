namespace trackrv2_shared.DTOs;

public record TrackerRequest(string Name, List<FieldDefinitionRequest> Fields);