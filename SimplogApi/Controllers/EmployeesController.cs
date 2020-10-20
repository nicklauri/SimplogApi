using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplogApi.Models;

namespace SimplogApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly SimplogContext _context;

        public EmployeesController(SimplogContext context)
        {
            _context = context;
        }

        // GET: api/Employees
        [Authorize]
        [HttpGet("All")]
        public IEnumerable<Employee> GetEmployees()
        {
            return _context.Employees;
        }

        // GET: api/Employees/?page=3&maxEntries=10
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetEmployeesWithPaging(int page = 1, int maxEntries = 5)
        {
            if (maxEntries <= 0)
            {
                return BadRequest(new { isSuccess = false, status = "maxEntries must not be less or equal to 0" });
            }

            // maxEntries has already checked, so totalPages is not null.
            var totalPages = await GetTotalPages(maxEntries);

            // `page` starts at 1, not 0 for human logic.
            if (page <= 0)
            {
                page = 1;
            }
            else if (page > totalPages)
            {
                return BadRequest(new { isSuccess = false, status = $"page number ({page}) exceeded totalPages ({totalPages})" });
            }

            // step starts at 0 is for machine logic.
            var step = page - 1;
            var data = _context.Employees.Skip(step * maxEntries).Take(maxEntries);
            return Ok(new { isSuccess = true, totalPages, data });
        }

        [Authorize]
        [HttpGet("TotalPages")]
        public async Task<IActionResult> TotalPages(int maxEntries = 5)
        {
            if (maxEntries <= 0)
            {
                return BadRequest(new { isSuccess = false, status = "maxEntries shouldn't be less than or equal to 0" });
            }

            var totalEntries = await _context.Employees.CountAsync();

            // maxEntries is checked <= 0 before calling GetTotalPages,
            // so the value is not null. (?)
            var totalPages = await GetTotalPages(maxEntries);

            return Ok(new { isSuccess = true, totalEntries, totalPages, maxEntries});
        }

        // GET: api/Employees/Id/86a1f89f-6f17-4041-8fa7-08d8718c2bdb
        [Authorize]
        [HttpGet("Id/{id}")]
        public async Task<IActionResult> GetEmployee([FromRoute] Guid id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var employee = await _context.Employees.FindAsync(id);

            if (employee == null)
            {
                return NotFound(new { isSuccess = false, status = "Not found" });
            }

            return Ok(new { isSuccess = true, employee });
        }
        
        // This route is remind me to not forget to put employeeId in the route.
        // PUT: api/Employees
        [Authorize]
        [HttpPut]
        public IActionResult PutEmployeeWithoutId([FromBody] Employee _)
        {
            return Ok(new { isSuccess = false, status = "Employee ID on the URL is required" });
        }

        // TODO: check image size.
        // PUT: api/Employees/86a1f89f-6f17-4041-8fa7-08d8718c2bdb
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEmployee([FromRoute] Guid id, [FromBody] Employee newEmployee)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != newEmployee.EmployeeId)
            {
                return BadRequest();
            }

            // Check if employee's email or code has been changed.
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                // No employee with ID was found.
                return Ok(new { isSuccess = false, status = "Employee's ID was not found" });
            }
            else if (CompareEmployee(employee, newEmployee))
            {
                return Ok(new { isSuccess = true, status = "Nothing has changed" });
            }
            else
            {
                // Check employee.Email and employee.Code if any of them has already existed.
                // Can't use EmployeeEmailOrCodeExists, because when only Code has changed and Email hasn't,
                // this method may return that Email has already existed.
                var errorMessage = string.Empty;
                if (employee.Email != newEmployee.Email)
                {
                    // New Employee's email has changed, check if it' existed.
                    if (await EmployeeEmailExists(newEmployee.Email))
                    {
                        errorMessage += "Email";
                    }
                }
                if (employee.Code != newEmployee.Code)
                {
                    if (await EmployeeCodeExists(newEmployee.Code))
                    {
                        if (string.IsNullOrEmpty(errorMessage))
                        {
                            errorMessage += "Code has already existed";
                        }
                        else
                        {
                            // Email and code are existed.
                            errorMessage += " and code have already existed";
                        }
                        return Ok(new { isSuccess = false, status = errorMessage });
                    }
                }
                
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    // Employee's code is unchanged, but errorMessage is not null or empty,
                    // which means employee's email has changed and already existed.
                    errorMessage += " has already existed";
                    return Ok(new { isSuccess = false, status = errorMessage });
                }
            }

            // Update employee.
            CopyEmployee(ref employee, newEmployee);

            //_context.Entry(employee).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { isSuccess = true });
        }

        // POST: api/Employees/
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PostEmployee([FromBody] Employee employee)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check employee.Email and employee.Code if they have already existed.
            var errorMessage = await EmployeeEmailOrCodeExists(employee);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                return Ok(new { isSuccess = false, status = errorMessage });
            }

            employee.EmployeeId = new Guid();

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            //return CreatedAtAction("GetEmployee", new { id = employee.EmployeeId }, employee);
            return Ok(new { isSuccess = true, employee });
        }

        // DELETE: api/Employees/86a1f89f-6f17-4041-8fa7-08d8718c2bdb
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee([FromRoute] Guid id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return Ok(employee);
        }

        private bool EmployeeExists(Guid id)
        {
            return _context.Employees.Any(e => e.EmployeeId == id);
        }

        private async Task<string> EmployeeEmailOrCodeExists(Employee employee)
        {
            var errorMessage = string.Empty;
            if (await EmployeeEmailExists(employee.Email))
            {
                errorMessage = "Employee's email";
            }
            if (await EmployeeCodeExists(employee.Code))
            {
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    errorMessage += " and code have";
                }
                else
                {
                    errorMessage += "Employee's code has";
                }
            }
            else if (!string.IsNullOrEmpty(errorMessage))
            {
                errorMessage += " has";
            }

            // If errorMessage is not null or empty, return the errors.
            if (!string.IsNullOrEmpty(errorMessage))
            {
                //  Employee's email has already existed
                //  Employee's email and code have already existed
                //  Employee's code has already existed.
                errorMessage += " already existed";
            }

            return errorMessage;
        }

        private async Task<bool> EmployeeEmailExists(string email)
        {
            return await _context.Employees.AnyAsync(emp => emp.Email == email);
        }

        private async Task<bool> EmployeeCodeExists(int code)
        {
            return await _context.Employees.AnyAsync(emp => emp.Code == code);
        }

        private void CopyEmployee(ref Employee dst, Employee src)
        {
            dst.Name = src.Name;
            dst.Email = src.Email;
            dst.Image = src.Image;
            dst.TaxCode = src.TaxCode;
            dst.Code = src.Code;
        }

        private bool CompareEmployee(Employee left, Employee right)
        {
            return left.Email == right.Email
                && left.TaxCode == right.TaxCode
                && left.Name == right.Name
                && left.Code == right.Code
                && left.Image == right.Image
                && left.EmployeeId == right.EmployeeId;
        }

        private string GetCurrentUsername()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            IList<Claim> claim = identity.Claims.ToList();
            var username = claim[0].Value;  // others are GUID, issuer,...

            return username;
        }

        private async Task<int?> GetTotalPages(int maxEntries)
        {
            // Although maxEntries will be checked first before calling this method.
            // But I should make sure that maxEntries always be checked.
            if (maxEntries <= 0)
            {
                return null;
            }

            var totalEntries = await _context.Employees.CountAsync();
            var totalPages = totalEntries / maxEntries;

            // If totalEntries is smaller, its contents can fit in 1 page.
            if (maxEntries > totalEntries)
            {
                totalPages = 1;
            }
            else if (totalEntries % maxEntries != 0)
            {
                // If totalEntries is not divisible by maxEntries, which means there is a page
                // that has fewer entries than a normal page. We should totalPages by 1.
                totalPages += 1;
            }

            return totalPages;
        }
    }
}