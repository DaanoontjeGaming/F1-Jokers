using Microsoft.AspNetCore.Mvc;
using F1Jokers.Data;
using Microsoft.EntityFrameworkCore;

namespace F1Jokers.Controllers
{
    // De route wordt: /api/teams
    [Route("api/[controller]")]
    [ApiController]
    public class TeamsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TeamsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetTeams()
        {
            try
            {
                var teams = await _context.Teams.ToListAsync();

                return Ok(teams);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Er ging iets mis met de database: {ex.Message}");
            }
        }
    }
}