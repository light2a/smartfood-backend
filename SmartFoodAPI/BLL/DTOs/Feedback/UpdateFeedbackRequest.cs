using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.Feedback
{
    public class UpdateFeedbackRequest
    {
        [Required]
        public int CustomerAccountId { get; set; }
        [Required]
        public int Rating { get; set; }
        [Required, MaxLength(1000)]
        public string? Comment { get; set; }
    }
}
