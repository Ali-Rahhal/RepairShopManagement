using Microsoft.EntityFrameworkCore;
using RepairShop.Data;
using RepairShop.Models;
using RepairShop.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace RepairShop.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly AppDbContext _db;
        internal DbSet<T> dbSet;

        public Repository(AppDbContext db)
        {
            _db = db;
            dbSet = _db.Set<T>();//means for example _db.Categories == dbSet
        }

        //public async Task AddAsy(T entity)
        //{
        //    await dbSet.AddAsync(entity);
        //}

        //public async Task AddRangeAsy(IEnumerable<T> entity)
        //{
        //    await dbSet.AddRangeAsync(entity);
        //}

        public async Task<T> GetAsy(Expression<Func<T, bool>> filter, string? includeProperties = null, bool tracked = false)//includeProperties is for including data like the Category var in Product class
        {
            IQueryable<T> query;
            if (tracked)
            {
                query = dbSet;
            }
            else
            {
                query = dbSet.AsNoTracking();//AsNoTracking() means we are not tracking changes to the entities retrieved
                                             //from the database. This is useful for read-only scenarios where you don't intend to modify the entities.
            }

            query = query.Where(filter);
            //include properties will be comma separated like "Category,CategoryId" if we want to include multiple properties
            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProp in includeProperties
                    .Split([','], StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp);
                }
            }
            return await query.FirstOrDefaultAsync();
            //4 statements above same as return dbSet.Where(filter).Include(u => u.includeProp).//otherInclusions//.FirstOrDefault();
        }

        public async Task<IEnumerable<T>> GetAllAsy(Expression<Func<T, bool>>? filter = null, string? includeProperties = null, bool tracked = false)//includeProperties is for including data like the Category var in Product class(like the Get method above)
        {
            IQueryable<T> query;
            if (tracked)
            {
                query = dbSet;
            }
            else
            {
                query = dbSet.AsNoTracking();//AsNoTracking() means we are not tracking changes to the entities retrieved
                                             //from the database. This is useful for read-only scenarios where you don't intend to modify the entities.
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProp in includeProperties
                    .Split([','], StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp);
                }
            }
            return await query.ToListAsync();
        }

        public Task<IQueryable<T>> GetQueryableAsy(Expression<Func<T, bool>>? filter = null, string? includeProperties = null)
        {
            IQueryable<T> query = dbSet;

            if (filter != null)
                query = query.Where(filter);

            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProp in includeProperties
                    .Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp);
                }
            }
            return Task.FromResult(query);
        }

        //virtual means we can override this method in a derived class
        public virtual async Task RemoveAsy(T entity)
        {
            dbSet.Remove(entity);
            await Task.CompletedTask;
        }

        public virtual async Task RemoveRangeAsy(IEnumerable<T> entity)
        {
            dbSet.RemoveRange(entity);
            await Task.CompletedTask;
        }
    }
}