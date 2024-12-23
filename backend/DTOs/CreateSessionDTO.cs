using System.ComponentModel.DataAnnotations;
public class CreateSessionDTO
{
    [Required(ErrorMessage = "UserId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "UserId must be greater than 0.")]
    public int UserId { get; set; }
}
