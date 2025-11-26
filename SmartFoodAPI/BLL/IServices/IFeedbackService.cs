using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs.Feedback;
using DAL.Models;

namespace BLL.IServices
{
    public interface IFeedbackService
    {
        Task<IEnumerable<FeedbackDto>> GetAllAsync();
        Task<PagedResult<FeedbackDto>> GetPagedAsync(int pageNumber, int pageSize, string? keyword);
        Task<FeedbackDto?> GetByIdAsync(int id);
        Task<FeedbackDto> CreateAsync(CreateFeedbackRequest request);
        //Task UpdateAsync(int id, UpdateFeedbackRequest request);
        Task DeleteAsync(int id);
        Task<IEnumerable<FeedbackDto>> SearchAsync(string keyword);
    }
}
