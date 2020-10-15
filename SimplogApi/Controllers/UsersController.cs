using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplogApi.Models;

namespace SimplogApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly SimplogContext _context;

        public UsersController(SimplogContext context)
        {
            _context = context;
        }

        // Debug-purposed route.
        // GET: api/Users/List
        [HttpGet("List")]
        public IQueryable GetUsers()
        {
            // we're not going to send password.
            return _context.Users.Select(user => new { id = user.Id, username = user.Username });
        }

        // Debug-purposed route.
        // GET: api/Users/List
        [HttpGet("ListAll")]
        public async Task<IEnumerable<User>> GetUsersWithPassword()
        {
            // we're not going to send password.
            return await _context.Users.ToListAsync();
        }

        // Debug-purposed route.
        // GET: api/Users/Id/5
        [HttpGet("Id/{id}")]
        public async Task<IActionResult> GetUser([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(user.Username);
        }

        // Login API (content-type: application/www-form-urlencoded, Angular may send JSON instead).
        // Returns:
        //  ID: if username and password are matched in the database.
        //  0 : if nothing is matched, the ID column in the Users table starts at 1.
        // POST: api/Users
        [HttpPost]
        public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // Probably don't need this since FindAsync returns null if either username or password is null.
            // else if (username == null || password == null)
            // {
            //     return BadRequest("Missing user info");
            // }

            // Console.WriteLine($"---\n{username} :: {password}\n---");

            var matchedUser = await FindAsync(username, password);
            if (matchedUser != null)
            {
                return Ok(matchedUser.Id);
            }
            else
            {
                return Ok(-1);
            }

        }

        // Register new user.
        // PUT: api/Users/
        [HttpPut]
        public async Task<IActionResult> RegisterUser([FromForm] string username, [FromForm] string password)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await UserExists(username))
            {
                // Username is already existed.
                return Ok(-1);
            }
            else
            {
                // Register new user.
                // TODO: password should be hashed.
                var newUser = await _context.Users.AddAsync(new Models.User
                {
                    Id = 0,
                    Username = username,
                    Password = password,
                });

                // Save it.
                await _context.SaveChangesAsync();
                return Ok(newUser.Entity.Id);
            }
        }

        // Delete a user from database, need username and password matched with a record in the database.
        // DELETE: api/Users/
        [HttpDelete]
        public async Task<IActionResult> DeleteUser([FromForm] string username, [FromForm] string password)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await FindAsync(username, password);
            if (user == null)
            {
                return NotFound(-1);
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(user.Id);
        }

        // Find a user record asynchronously.
        private async Task<User> FindAsync(string username, string password)
        {
            try
            {
                // FirstAsync will either return a valid value or throw an exception (not found or args are null).
                return await _context.Users.FirstAsync(user => user.Username == username && user.Password == password);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private async Task<bool> UserExists(string username)
        {
            return await _context.Users.AnyAsync(user => user.Username == username);
        }
    }
}