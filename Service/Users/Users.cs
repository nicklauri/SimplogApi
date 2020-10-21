using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Domain;
using AutoMapper;
using Infrastructure.Extensions;

namespace Service.Users
{
    public interface IUsersService
    {
        IQueryable ListUsers();
        object GetUser(int id);
        object Login(User userinfo);
        object Register(User userinfo);
        object Delete(User userinfo);
    }

    public class UsersService: IUsersService
    {
        private readonly SimplogContext Context;
        private readonly IConfiguration Config;
        private readonly IMapper Mapper;
        public UsersService(SimplogContext context, IConfiguration config, IMapper mapper)
        {
            Context = context;
            Config = config;
            Mapper = mapper;
        }

        public IQueryable ListUsers()
        {
            return Context.Users.Select(user => new { id = user.Id, username = user.Username });
        }

        public object GetUser(int id)
        {
            var userinfo = Context.Users.Find(id);

            if (userinfo.IsNull())
            {
                return new { success = false, status = "User ID not found" };
            }
            else
            {
                return new { success = true, user = Mapper.Map<UserDto>(userinfo) };
            }
        }

        public object Login(User userInfo)
        {
            var matchedUser = FindUserByUserInfo(userInfo);
            if (matchedUser != null)
            {
                // TODO: generate JSON Web Token and return to client.
                return new { susccess = true, userId = matchedUser.Id, token = GenerateJSONWebToken(matchedUser) };
            }
            else
            {
                return new { success = false, status = "Wrong username or password" };
            }
        }

        public object Register(User userinfo)
        {
            if (UserExists(userinfo.Username))
            {
                // Username is already existed.
                return new { success = false, status = "Username has already existed" };
            }
            else
            {
                // Register new user.
                // TODO: password should be hashed.
                var newUser = Context.Users.Add(userinfo);

                // Save it.
                Context.SaveChanges();
                return new { success = true, userId = newUser.Entity.Id };
            }
        }

        public object Delete(User userinfo)
        {
            var user = FindUserByUserInfo(userinfo);
            if (user == null)
            {
                return new { success = false, status = "Can't delete because of wrong username or password" };
            }

            Context.Users.Remove(user);
            Context.SaveChanges();

            return new { success = true, user = Mapper.Map<UserDto>(user) };
        }

        private User FindUserByUserInfo(User userInfo)
        {
            try
            {
                // FirstOrDefaultAsync returns either user or null.
                return Context.Users.FirstOrDefault(user => user.Username == userInfo.Username && user.Password == userInfo.Password);
            }
            catch (ArgumentNullException)
            {
                return null;
            }
        }

        private bool UserExists(string username)
        {
            return Context.Users.Any(user => user.Username == username);
        }

        // Generate JSON Web Token based on user information
        private string GenerateJSONWebToken(User userinfo)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userinfo.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                // They have email field in the tutorial, but our userInfo doesn't have Email field.
                // new Claim(JwtRegisteredClaimNames.Email, userInfo.Email),
            };

            var issuer = Config["Jwt:Issuer"];
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
