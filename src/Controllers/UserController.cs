using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Financa.Models;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Financa.src.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly FinancaContext _context;

        public UserController(FinancaContext context)
        {
            _context = context;
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
    }
}