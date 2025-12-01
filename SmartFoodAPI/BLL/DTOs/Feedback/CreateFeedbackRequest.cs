using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.Feedback
{
    public class CreateFeedbackRequest
    {
        [Required]
        public int CustomerAccountId { get; set; }
        [Required]
        [Range(0, 10, ErrorMessage = "Rating must be between 0 and 10.")]
        public int Rating { get; set; }
        [Required, MaxLength(1000)]
        public string? Comment { get; set; }
        [Required]
        public int OrderId { get; set; }
    }
}
