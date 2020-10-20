using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Employees;
using Domain;

namespace SimplogApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeesService Service;
        public EmployeesController(IEmployeesService service)
        {
            Service = service;
        }

        // GET: api/Employees
        [Authorize]
        [HttpGet("All")]
        public IEnumerable<Employee> GetEmployees()
        {
            return Service.GetEmployees();
        }

        // GET: api/Employees/?page=3&maxEntries=10
        [Authorize]
        [HttpGet]
        public object GetEmployeesWithPaging(int page = 1, int maxEntries = 5)
        {
            return Service.GetEmployeesWithPaging(page, maxEntries);
        }

        [Authorize]
        [HttpGet("TotalPages")]
        public object TotalPages(int maxEntries = 5)
        {
            return Service.TotalPages(maxEntries);
        }

        // GET: api/Employees/Id/86a1f89f-6f17-4041-8fa7-08d8718c2bdb
        [Authorize]
        [HttpGet("Id/{id}")]
        public object GetEmployee([FromRoute] Guid id)
        {
            return Service.GetEmployeeById(id);
        }

        // This route is remind me to not forget to put employeeId in the route.
        // PUT: api/Employees
        [Authorize]
        [HttpPut]
        public object PutEmployeeWithoutId([FromBody] Employee _)
        {
            return Service.UpdateWithoutId();
        }

        // TODO: check image size.
        // PUT: api/Employees/86a1f89f-6f17-4041-8fa7-08d8718c2bdb
        [Authorize]
        [HttpPut("{id}")]
        public object PutEmployee([FromRoute] Guid id, [FromBody] Employee newEmployee)
        {
            return Service.Update(id, newEmployee);
        }

        // POST: api/Employees/
        [Authorize]
        [HttpPost]
        public object PostEmployee([FromBody] Employee employee)
        {
            return Service.Register(employee);
        }

        // DELETE: api/Employees/86a1f89f-6f17-4041-8fa7-08d8718c2bdb
        [Authorize]
        [HttpDelete("{id}")]
        public object DeleteEmployee([FromRoute] Guid id)
        {
            return Service.Delete(id);
        }
    }
}