namespace trackrv2_shared.DTOs;

public record TrackerRequest(Guid? TrackerId, string Name, List<FieldDefinitionRequest> Fields);
