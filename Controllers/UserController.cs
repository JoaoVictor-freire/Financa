using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Financa.Models;
using Financa.dto;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.CodeAnalysis.Host.Mef;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Data;

namespace Financa.src.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly FinancaContext _context;
        private readonly IConfiguration _config;

        public UserController(FinancaContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        private string GenerateToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.IdUsuario.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.Nome),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(12),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] User newUser)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                string rawPassword = newUser.Senha;
                string hashPassword = BCrypt.Net.BCrypt.HashPassword(rawPassword, workFactor: 8);
                newUser.Senha = hashPassword;

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetUserById), new { id = newUser.IdUsuario }, newUser);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro interno ao salvar: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserById(int id)
        {
            var userFromDb = await _context.Users.FindAsync(id);
            if (userFromDb == null)
            {
                return NotFound();
            }

            try
            {
                _context.Remove(userFromDb);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch(Exception ex)
            {
                return StatusCode(500, $"Erro interno ao deletar: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var userFromDb = await _context.Users.FindAsync(id);

            if (userFromDb == null)
            {
                return NotFound();
            }

            return Ok(userFromDb);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User updatedUser)
        {
            if (id != updatedUser.IdUsuario)
            {
                return BadRequest("O ID da rota não corresponde ao ID do usuário.");
            }

            var userFromDb = await _context.Users.FindAsync(id);
            if (userFromDb == null)
            {
                return NotFound($"Usuário com ID {id} não encontrado.");
            }

            userFromDb.Nome = updatedUser.Nome;
            userFromDb.Email = updatedUser.Email;

            if (!string.IsNullOrEmpty(updatedUser.Senha))
            {
                userFromDb.Senha = BCrypt.Net.BCrypt.HashPassword(updatedUser.Senha, workFactor: 8);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro interno ao atualizar: {ex.Message}");
            }

            return NoContent();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == loginModel.Email);
            
            if(user == null){
                return Unauthorized("Email ou Senha invalidos.");
            }

            bool correctPassword = BCrypt.Net.BCrypt.Verify(loginModel.Senha, user.Senha);
            if (!correctPassword)
            {
                return Unauthorized("Email ou Senha invalidos.");
            }

            string token = GenerateToken(user);

            return Ok(
                new
                {
                    Token = token,
                    Usuaario = new {user.IdUsuario, user.Nome, user.Email}
                }
            );
        }
    }
}