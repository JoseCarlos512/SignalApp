using MyProject.Domain.Entities;

namespace MyProject.Domain.Interfaces;

public interface IAdvisorRepository
{
    bool HasActiveAdvisors();
    bool IsAdvisorActive(string advisorId);
    void SetAdvisorActive(string advisorId, string advisorName, bool isActive);
    IReadOnlyCollection<AdvisorState> GetAdvisors();
    void EnsureAdvisorExists(string advisorId, string advisorName);
}
