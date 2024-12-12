using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingSystem.Infrastructure.Data;

namespace TradingSystem.Infrastructure.Repositories;

public abstract class RepositoryBase<TEntity> where TEntity : class
{
    protected readonly TradingContext Context;
    protected readonly DbSet<TEntity> DbSet;
    protected readonly ILogger Logger;

    protected RepositoryBase(
        TradingContext context,
        ILogger logger)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
        Logger = logger;
    }

    protected virtual async Task<TEntity?> GetByIdAsync(object id)
    {
        try
        {
            return await DbSet.FindAsync(id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting entity by id {Id}", id);
            throw;
        }
    }

    protected virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        try
        {
            return await DbSet.ToListAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting all entities");
            throw;
        }
    }

    protected virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
    {
        try
        {
            return await DbSet.Where(predicate).ToListAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error finding entities with predicate");
            throw;
        }
    }

    protected virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        try
        {
            var entry = await DbSet.AddAsync(entity);
            await Context.SaveChangesAsync();
            return entry.Entity;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error adding entity");
            throw;
        }
    }

    protected virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        try
        {
            Context.Entry(entity).State = EntityState.Modified;
            await Context.SaveChangesAsync();
            return entity;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating entity");
            throw;
        }
    }

    protected virtual async Task DeleteAsync(TEntity entity)
    {
        try
        {
            DbSet.Remove(entity);
            await Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting entity");
            throw;
        }
    }

    protected virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null)
    {
        try
        {
            return predicate == null
                ? await DbSet.CountAsync()
                : await DbSet.CountAsync(predicate);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error counting entities");
            throw;
        }
    }

    protected virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
    {
        try
        {
            return await DbSet.AnyAsync(predicate);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking if entity exists");
            throw;
        }
    }

    protected virtual IQueryable<TEntity> GetQueryable()
    {
        return DbSet.AsQueryable();
    }

    protected virtual async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        params Expression<Func<TEntity, object>>[] includes)
    {
        try
        {
            var query = DbSet.AsQueryable();
            query = includes.Aggregate(query, (current, include) => current.Include(include));
            return await query.FirstOrDefaultAsync(predicate);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting first entity");
            throw;
        }
    }

    protected virtual async Task<List<TEntity>> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<TEntity, object>> orderBy,
        bool ascending = true)
    {
        try
        {
            var query = ascending
                ? DbSet.OrderBy(orderBy)
                : DbSet.OrderByDescending(orderBy);

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting paged entities");
            throw;
        }
    }

    protected virtual async Task<(List<TEntity> Items, int TotalCount)> GetPagedWithTotalAsync(
        int page,
        int pageSize,
        Expression<Func<TEntity, object>> orderBy,
        Expression<Func<TEntity, bool>>? filter = null,
        bool ascending = true)
    {
        try
        {
            var query = DbSet.AsQueryable();

            if (filter != null)
                query = query.Where(filter);

            var totalCount = await query.CountAsync();

            query = ascending
                ? query.OrderBy(orderBy)
                : query.OrderByDescending(orderBy);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting paged entities with total");
            throw;
        }
    }

    protected virtual async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        using var transaction = await Context.Database.BeginTransactionAsync();
        try
        {
            await action();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Logger.LogError(ex, "Error executing transaction");
            throw;
        }
    }

    protected virtual async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action)
    {
        using var transaction = await Context.Database.BeginTransactionAsync();
        try
        {
            var result = await action();
            await transaction.CommitAsync();
            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Logger.LogError(ex, "Error executing transaction");
            throw;
        }
    }

    protected virtual async Task BulkInsertAsync(IEnumerable<TEntity> entities)
    {
        try
        {
            await DbSet.AddRangeAsync(entities);
            await Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error bulk inserting entities");
            throw;
        }
    }

    protected virtual async Task BulkUpdateAsync(IEnumerable<TEntity> entities)
    {
        try
        {
            DbSet.UpdateRange(entities);
            await Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error bulk updating entities");
            throw;
        }
    }

    protected virtual async Task BulkDeleteAsync(IEnumerable<TEntity> entities)
    {
        try
        {
            DbSet.RemoveRange(entities);
            await Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error bulk deleting entities");
            throw;
        }
    }
}
