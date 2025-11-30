using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RepairShop.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        //T - Category
        Task<IEnumerable<T>> GetAllAsy(Expression<Func<T, bool>>? filter = null, string? includeProperties = null, bool tracked = false);

        Task<T> GetAsy(Expression<Func<T, bool>> filter, string? includeProperties = null, bool tracked = false);

        //Task AddAsy(T entity);

        //Task AddRangeAsy(IEnumerable<T> entity);

        Task RemoveAsy(T entity);

        Task RemoveRangeAsy(IEnumerable<T> entity);
    }
}