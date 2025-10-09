using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.Area
{
    public class AreaDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? City { get; set; }
    }
}
