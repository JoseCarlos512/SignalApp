using ChatBackend.Domain.Entities;
using ChatBackend.Domain.Interfaces;
using ChatBackend.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatBackend.Infrastructure.Persistence.Repositories;

public class EfAdvisorRepository(ChatDbContext dbContext) : IAdvisorRepository
{
    public bool HasActiveAdvisors() => dbContext.Advisors.Any(a => a.IsActive);

    public bool IsAdvisorActive(string advisorId) =>
        dbContext.Advisors.Any(a => a.AdvisorId == advisorId && a.IsActive);

    public void SetAdvisorActive(string advisorId, string advisorName, bool isActive)
    {
        var advisor = dbContext.Advisors.FirstOrDefault(a => a.AdvisorId == advisorId);
        if (advisor is null)
        {
            dbContext.Advisors.Add(new AdvisorStateEntity
            {
                AdvisorId = advisorId,
                Name = advisorName,
                IsActive = isActive
            });
        }
        else
        {
            advisor.Name = advisorName;
            advisor.IsActive = isActive;
        }

        dbContext.SaveChanges();
    }

    public IReadOnlyCollection<AdvisorState> GetAdvisors() => dbContext.Advisors
        .AsNoTracking()
        .Select(a => new AdvisorState { AdvisorId = a.AdvisorId, Name = a.Name, IsActive = a.IsActive })
        .ToList();

    public void EnsureAdvisorExists(string advisorId, string advisorName)
    {
        var advisor = dbContext.Advisors.FirstOrDefault(a => a.AdvisorId == advisorId);
        if (advisor is null)
        {
            dbContext.Advisors.Add(new AdvisorStateEntity
            {
                AdvisorId = advisorId,
                Name = advisorName,
                IsActive = false
            });
            dbContext.SaveChanges();
            return;
        }

        if (advisor.Name != advisorName)
        {
            advisor.Name = advisorName;
            dbContext.SaveChanges();
        }
    }
}
