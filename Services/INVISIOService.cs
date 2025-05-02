using INVISIO.Models; 
using BCrypt.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver; 
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace INVISIO.Services
{
    public class INVISIOService
    {
        private readonly IMongoCollection<User> _users;
        private readonly IConfiguration _config;

        public INVISIOService(IMongoClient client, IConfiguration config)
        {
            var database = client.GetDatabase("INVISIODb");
            _users = database.GetCollection<User>("Users");
            _config = config;
        }

        // Register a new user
        public async Task<User?> RegisterUserAsync(string fullName, string email, string password)
        {
            var existingUser = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (existingUser != null)
                return null;

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = passwordHash
            };

            await _users.InsertOneAsync(user);
            return user;
        }


        // Authenticate user and generate JWT token
        public async Task<string> AuthenticateAsync(string email, string password)
        {
            var user = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return null; // Invalid credentials
            }

            return GenerateJwtToken(user);
        }

        // Generate JWT token
        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(1);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
