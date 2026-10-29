using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Repositories.Interfaces;

namespace WelfareSystem.Api.Repositories;

public class AnomalyRepository : BaseRepository<Anomaly>, IAnomalyRepository
{
    public AnomalyRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Anomaly>> GetBySeverityAsync(string severity)
    {
        return await _dbSet
            .Where(a => a.Severity == severity)
            .Include(a => a.Application)
            .Include(a => a.User)
            .OrderByDescending(a => a.DetectedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Anomaly>> GetByTypeAsync(string anomalyType)
    {
        return await _dbSet
            .Where(a => a.AnomalyType == anomalyType)
            .Include(a => a.Application)
            .Include(a => a.User)
            .OrderByDescending(a => a.DetectedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Anomaly>> GetByApplicationIdAsync(int applicationId)
    {
        return await _dbSet
            .Where(a => a.ApplicationId == applicationId)
            .Include(a => a.User)
            .Include(a => a.Resolver)
            .OrderByDescending(a => a.DetectedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Anomaly>> GetByUserIdAsync(int userId)
    {
        return await _dbSet
            .Where(a => a.UserId == userId)
            .Include(a => a.Application)
            .Include(a => a.Resolver)
            .OrderByDescending(a => a.DetectedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Anomaly>> GetByStatusAsync(string status)
    {
        return await _dbSet
            .Where(a => a.Status == status)
            .Include(a => a.Application)
            .Include(a => a.User)
            .OrderByDescending(a => a.DetectedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Anomaly>> GetByDepartmentIdAsync(int departmentId)
    {
        return await _dbSet
            .Where(a => a.User != null && a.User.DepartmentId == departmentId)
            .Include(a => a.Application)
            .Include(a => a.User)
            .OrderByDescending(a => a.DetectedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Anomaly>> GetRecentAsync(int count = 50)
    {
        return await _dbSet
            .Include(a => a.Application)
            .Include(a => a.User)
            .OrderByDescending(a => a.DetectedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetSeverityCountsAsync()
    {
        return await _dbSet
            .GroupBy(a => a.Severity)
            .Select(g => new { Severity = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Severity, g => g.Count);
    }

    public async Task<Dictionary<string, int>> GetTypeCountsAsync()
    {
        return await _dbSet
            .GroupBy(a => a.AnomalyType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Type, g => g.Count);
    }
}
