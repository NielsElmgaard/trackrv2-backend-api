using System.ComponentModel.DataAnnotations.Schema;
using trackrv2_shared;

namespace trackrv2_efc.Entities;

public class User : BaseEntity
{
    [Column(TypeName = "varchar(80)")]
    public required string Username { get; set; }

    [Column(TypeName = "varchar(80)")]
    public required string Email { get; set; }

    [Column(TypeName = "varchar(256)")]
    public required string Password { get; set; }


    [Column(TypeName = "varchar(80)")] public string? Nationality { get; set; }

    [Column(TypeName = "varchar(80)")]
    public required string FirstName { get; set; }

    [Column(TypeName = "varchar(80)")] public string? MiddleName { get; set; }

    [Column(TypeName = "varchar(80)")]
    public required string LastName { get; set; }

    public required long PhoneNumber { get; set; }

    public Role Role { get; set; } = Role.Regular;

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    public List<Tracker> Trackers { get; set; } = new();
}

