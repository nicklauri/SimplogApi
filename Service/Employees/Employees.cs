using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Infrastructure.Extensions;
using Domain;

namespace Service.Employees
{
    public interface IEmployeesService
    {
        IEnumerable<Employee> GetEmployees();
        object GetEmployeesWithPaging(int page, int maxEntries);
        object GetEmployeeById(Guid id);
        object TotalPages(int maxEntries);
        object Update(Guid id, Employee emp);
        object UpdateWithoutId();
        object Register(Employee emp);
        object Delete(Guid id);
    }

    public class EmployeesService: IEmployeesService
    {
        private readonly SimplogContext Context;
        private readonly IConfiguration Config;

        public EmployeesService(SimplogContext context, IConfiguration config)
        {
            Context = context;
            Config = config;
        }

        // GET: api/Employees
        public IEnumerable<Employee> GetEmployees()
        {
            return Context.Employees;
        }

        public object GetEmployeesWithPaging(int page = 1, int maxEntries = 5)
        {
            if (maxEntries <= 0)
            {
                return new { success = false, status = "maxEntries must not be less or equal to 0" };
            }

            // maxEntries has already checked, so totalPages is not null.
            var totalPages = GetTotalPages(maxEntries);

            // `page` starts at 1, not 0 for human logic.
            if (page <= 0)
            {
                page = 1;
            }
            else if (page > totalPages)
            {
                return new { success = false, status = $"page number ({page}) exceeded totalPages ({totalPages})" };
            }

            // step starts at 0 is for machine logic.
            var step = page - 1;
            var data = Context.Employees.Skip(step * maxEntries).Take(maxEntries);
            return new { success = true, totalPages, data };
        }

        public object TotalPages(int maxEntries = 5)
        {
            if (maxEntries <= 0)
            {
                return new { success = false, status = "maxEntries shouldn't be less than or equal to 0" };
            }

            var totalEntries = Context.Employees.Count();

            // maxEntries is checked <= 0 before calling GetTotalPages,
            // so the value is not null. (?)
            var totalPages = GetTotalPages(maxEntries);

            return new { success = true, totalEntries, totalPages, maxEntries };
        }

        public object GetEmployeeById(Guid id)
        {
            var employee = Context.Employees.Find(id);

            if (employee == null)
            {
                return new { success = false, status = "Not found" };
            }

            return new { success = true, employee };
        }

        public object UpdateWithoutId()
        {
            // This is just a remind that I need to have employee ID on the route.
            return new { success = false, status = "Employee ID on the URL is required" };
        }

        public object Update(Guid id, Employee newEmployee)
        {
            if (id != newEmployee.EmployeeId)
            {
                return new { success = false, status = "Employee IDs don't match" };
            }

            // Check if employee's email or code has been changed.
            var employee = Context.Employees.Find(id);
            if (employee == null)
            {
                // No employee with ID was found.
                return new { success = false, status = "Employee's ID was not found" };
            }
            else if (CompareEmployee(employee, newEmployee))
            {
                return new { success = true, status = "Nothing has changed" };
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
                    if (IsEmployeeEmailExists(newEmployee.Email))
                    {
                        errorMessage += "Email";
                    }
                }
                if (employee.Code != newEmployee.Code)
                {
                    if (IsEmployeeCodeExists(newEmployee.Code))
                    {
                        if (errorMessage.IsNullOrEmpty())
                        {
                            errorMessage += "Code has already existed";
                        }
                        else
                        {
                            // Email and code are existed.
                            errorMessage += " and code have already existed";
                        }
                        return new { success = false, status = errorMessage };
                    }
                }

                if (!errorMessage.IsNullOrEmpty())
                {
                    // Employee's code is unchanged, but errorMessage is not null or empty,
                    // which means employee's email has changed and already existed.
                    errorMessage += " has already existed";
                    return new { success = false, status = errorMessage };
                }
            }

            // Update employee.
            CopyEmployee(ref employee, newEmployee);

            //_context.Entry(employee).State = EntityState.Modified;

            Context.SaveChanges();

            return new { success = true };
        }

        public object Register(Employee employee)
        {

            // Check employee.Email and employee.Code if they have already existed.
            var errorMessage = IsEmployeeEmailOrCodeExists(employee);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                return new { success = false, status = errorMessage };
            }

            employee.EmployeeId = new Guid();

            Context.Employees.Add(employee);
            Context.SaveChanges();

            //return CreatedAtAction("GetEmployee", new { id = employee.EmployeeId }, employee);
            return new { success = true, employee };
        }

        public object Delete(Guid id)
        {
            var employee = Context.Employees.Find(id);
            if (employee == null)
            {
                return new { success = false, status = "Employee ID was not found" };
            }

            Context.Employees.Remove(employee);
            Context.SaveChanges();

            return new { success = true, employee };
        }

        private bool IsEmployeeIdExists(Guid id)
        {
            return Context.Employees.Any(e => e.EmployeeId == id);
        }

        private string IsEmployeeEmailOrCodeExists(Employee employee)
        {
            var errorMessage = string.Empty;
            if (IsEmployeeEmailExists(employee.Email))
            {
                errorMessage = "Employee's email";
            }
            if (IsEmployeeCodeExists(employee.Code))
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

        private bool IsEmployeeEmailExists(string email)
        {
            return Context.Employees.Any(emp => emp.Email == email);
        }

        private bool IsEmployeeCodeExists(int code)
        {
            return Context.Employees.Any(emp => emp.Code == code);
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

        //private string GetCurrentUsername()
        //{
        //    var identity = HttpContext.User.Identity as ClaimsIdentity;
        //    IList<Claim> claim = identity.Claims.ToList();
        //    var username = claim[0].Value;  // others are GUID, issuer,...

        //    return username;
        //}

        private int? GetTotalPages(int maxEntries)
        {
            // Although maxEntries will be checked first before calling this method.
            // But I should make sure that maxEntries always be checked.
            if (maxEntries <= 0)
            {
                return null;
            }

            var totalEntries = Context.Employees.Count();
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
