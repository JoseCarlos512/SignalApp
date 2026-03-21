using System.Collections.Concurrent;
using ChatBackend.Domain.Interfaces;

namespace ChatBackend.Infrastructure.Realtime;

public class InMemoryAdvisorConnectionManager : IAdvisorConnectionManager
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _advisorConnections = new();

    public void AddConnection(string advisorId, string connectionId)
    {
        var connections = _advisorConnections.GetOrAdd(advisorId, _ => new HashSet<string>());
        lock (connections)
        {
            connections.Add(connectionId);
        }
    }

    public void RemoveConnection(string advisorId, string connectionId)
    {
        if (!_advisorConnections.TryGetValue(advisorId, out var connections))
        {
            return;
        }

        lock (connections)
        {
            connections.Remove(connectionId);
            if (connections.Count == 0)
            {
                _advisorConnections.TryRemove(advisorId, out _);
            }
        }
    }
}
