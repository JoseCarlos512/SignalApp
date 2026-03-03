namespace MyProject.Domain.Interfaces;

public interface IAdvisorConnectionManager
{
    void AddConnection(string advisorId, string connectionId);
    void RemoveConnection(string advisorId, string connectionId);
}
