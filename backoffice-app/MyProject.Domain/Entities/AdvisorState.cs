namespace MyProject.Domain.Entities;

public class AdvisorState
{
    public string AdvisorId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; set; }
}
