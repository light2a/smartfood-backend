using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs.Feedback;
using BLL.IServices;
using DAL.IRepositories;
using DAL.Models;

namespace BLL.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IFeedbackRepository _repo;
        public FeedbackService(IFeedbackRepository repo)
        {
            _repo = repo;
        }
        public async Task DeleteAsync(int id)
        {
            await _repo.DeleteAsync(id);
        }
        public async Task<IEnumerable<FeedbackDto>> GetAllAsync()
        {
            var list = await _repo.GetAllAsync();
            return list.Select(f => new FeedbackDto
            {
                Id = f.Id,
                CustomerAccountId = f.CustomerAccountId,
                Comment = f.Comment,
                Rating = f.Rating,
                CreatedAt = f.CreatedAt
            });
        }
        public async Task<FeedbackDto?> GetByIdAsync(int id)
        {
            var f = await _repo.GetByIdAsync(id);
            if (f == null) return null;
            return new FeedbackDto
            {
                Id = f.Id,
                CustomerAccountId = f.CustomerAccountId,
                Comment = f.Comment,
                Rating = f.Rating,
                CreatedAt = f.CreatedAt
            };
        }
        public async Task<PagedResult<FeedbackDto>> GetPagedAsync(int pageNumber, int pageSize, string? keyword)
        {
            var paged = await _repo.GetPagedAsync(pageNumber, pageSize, keyword);
            return new PagedResult<FeedbackDto>
            {
                Items = paged.Items.Select(f => new FeedbackDto
                {
                    Id = f.Id,
                    CustomerAccountId = f.CustomerAccountId,
                    Comment = f.Comment,
                    Rating = f.Rating,
                    CreatedAt = f.CreatedAt
                }),
                TotalItems = paged.TotalItems,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize
            };
        }
        public async Task<FeedbackDto> CreateAsync(CreateFeedbackRequest request)
        {
            var feedback = new Feedback
            {
                CustomerAccountId = request.CustomerAccountId,
                Comment = request.Comment,
                Rating = request.Rating,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _repo.AddAsync(feedback);

            return new FeedbackDto
            {
                Id = created.Id,
                CustomerAccountId = created.CustomerAccountId,
                Comment = created.Comment,
                Rating = created.Rating,
                CreatedAt = created.CreatedAt
            };
        }
        public async Task<IEnumerable<FeedbackDto>> SearchAsync(string keyword)
        {
            var list = await _repo.SearchAsync(keyword);
            return list.Select(f => new FeedbackDto
            {
                Id = f.Id,
                CustomerAccountId = f.CustomerAccountId,
                Comment = f.Comment,
                Rating = f.Rating,
                CreatedAt = f.CreatedAt
            });
        }
    }
}
