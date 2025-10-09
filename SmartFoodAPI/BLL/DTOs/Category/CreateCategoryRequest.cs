using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;


namespace BLL.DTOs.Category
{
    public class CreateCategoryRequest
    {
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        public string? Description { get; set; }
    }

}
