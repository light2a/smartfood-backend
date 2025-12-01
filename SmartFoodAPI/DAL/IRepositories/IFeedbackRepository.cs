using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Models;

namespace DAL.IRepositories
{
    public interface IFeedbackRepository
    {
        Task<IEnumerable<Feedback>> GetAllAsync();
        Task<PagedResult<Feedback>> GetPagedAsync(int pageNumber, int pageSize, string? keyword);
        Task<Feedback?> GetByIdAsync(int id);
        Task<Feedback> AddAsync(Feedback feedback);
        //Task UpdateAsync(Feedback feedback);
        Task DeleteAsync(int id);
        Task<IEnumerable<Feedback>> SearchAsync(string keyword);
    }
}
