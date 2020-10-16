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
using SimplogApi.Models;

namespace SimplogApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly SimplogContext _context;
        private readonly IConfiguration _config;

        public UsersController(SimplogContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
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
        // POST: api/Users/Login
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] User userInfo)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var matchedUser = await FindAsync(userInfo.Username, userInfo.Password);
            if (matchedUser != null)
            {
                // TODO: generate JSON Web Token and return to client.
                return Ok(new { isSusccess = true, userId = matchedUser.Id, token = GenerateJSONWebToken(matchedUser) }) ;
            }
            else
            {
                return Ok(new { isSuccess = false, status = "Wrong username or password" });
            }

        }

        [Authorize]
        [HttpPost("Verify")]
        public IActionResult Verify()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            IList<Claim> claim = identity.Claims.ToList();
            var username = claim[0].Value;  // others are GUID, issuer,...

            return Ok(new { status = $"Username `{username}` is authorized." });
        }

        // Register new user.
        // POST: api/Users/Register
        [HttpPost("Register")]
        public async Task<IActionResult> RegisterUser([FromBody] User userInfo)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await UserExists(userInfo.Username))
            {
                // Username is already existed.
                return Ok(new { isSuccess = false, status = "Username has already existed" } );
            }
            else
            {
                // Register new user.
                // TODO: password should be hashed.
                var newUser = await _context.Users.AddAsync(userInfo);

                // Save it.
                await _context.SaveChangesAsync();
                return Ok(new { isSuccess = true, userId = newUser.Entity.Id });
            }
        }

        // Delete a user from database, need username and password matched with a record in the database.
        // DELETE: api/Users/
        [HttpDelete]
        public async Task<IActionResult> DeleteUser([FromBody] User userInfo)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await FindAsync(userInfo.Username, userInfo.Password);
            if (user == null)
            {
                return NotFound(new { isSuccess = false, status = "Can't delete because of wrong username or password" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { isSuccess = true, username = user.Username, userId = user.Id });
        }

        // Find a user record asynchronously.
        private async Task<User> FindAsync(string username, string password)
        {
            try
            {
                // FirstOrDefaultAsync returns either user or null.
                return await _context.Users.FirstOrDefaultAsync(user => user.Username == username && user.Password == password);
            }
            catch (ArgumentNullException)
            {
                return null;
            }
        }

        private async Task<bool> UserExists(string username)
        {
            return await _context.Users.AnyAsync(user => user.Username == username);
        }

        // Generate JSON Web Token based on user information
        private string GenerateJSONWebToken(User userInfo)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userInfo.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                // They have email field in the tutorial, but our userInfo doesn't have Email field.
                // new Claim(JwtRegisteredClaimNames.Email, userInfo.Email),
            };

            var issuer = _config["Jwt:Issuer"];
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: issuer,
                claims,
                expires: DateTime.Now.AddMinutes(120),
                signingCredentials: credentials);
            var encodeToken = new JwtSecurityTokenHandler().WriteToken(token);
 
            return encodeToken;
        }
    }
}