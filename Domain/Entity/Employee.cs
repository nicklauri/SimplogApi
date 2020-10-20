using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Domain
{
    public class Employee
    {
        [Key]
        public Guid EmployeeId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Email { get; set; }
        public int TaxCode { get; set; }
        public int Code { get; set; }
        public byte[] Image { get; set; }
    }
}
