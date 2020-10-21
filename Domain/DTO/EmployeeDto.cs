using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    public class EmployeeDto
    {
        public Guid EmployeeId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int Code { get; set; }
        public int TaxCode { get; set; }
        public byte[] Image { get; set; }
    }
}
