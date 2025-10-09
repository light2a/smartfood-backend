using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.Category
{
    public class UpdateCategoryRequest
    {
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        public string? Description { get; set; }
    }
}
