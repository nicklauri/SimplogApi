using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Domain;
using Service.Users;

namespace SimplogApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService Service;

        public UsersController(IUsersService service)
        {
            Service = service;
        }

        // Debug-purposed route.
        // GET: api/Users/List
        [HttpGet("List")]
        public IQueryable GetUsers()
        {
            // we're not going to send password.
            return Service.ListUsers();
        }

        // GET: api/Users/Id/5
        [HttpGet("Id/{id}")]
        public string GetUser([FromRoute] int id)
        {
            return Service.GetUser(id);
        }

        // Login API (content-type: application/www-form-urlencoded, Angular may send JSON instead).
        // Returns:
        //  ID: if username and password are matched in the database.
        //  0 : if nothing is matched, the ID column in the Users table starts at 1.
        // POST: api/Users/Login
        [HttpPost("Login")]
        public object Login([FromBody] User userinfo)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return Service.Login(userinfo);
        }

        // Register new user.
        // POST: api/Users/Register
        [HttpPost("Register")]
        public object RegisterUser([FromBody] User userinfo)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return Service.Register(userinfo);
        }

        // Delete a user from database, need username and password matched with a record in the database.
        // DELETE: api/Users/
        [HttpDelete]
        public object DeleteUser([FromBody] User userinfo)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return Service.Delete(userinfo);
        }
    }
}