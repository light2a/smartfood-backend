using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.Feedback
{
    public class FeedbackDto
    {
        public int Id { get; set; }
        public int CustomerAccountId { get; set; }
        public int OrderId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CustomerName { get; set; }
    }
}
