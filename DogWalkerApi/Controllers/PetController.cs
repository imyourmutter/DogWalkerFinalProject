using Microsoft.AspNetCore.Mvc;
using DogWalkerApi.Services;
using MySql.Data.MySqlClient;

namespace DogWalkerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PetController : ControllerBase
    {
        private readonly DatabaseService _databaseService;

        public PetController(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        [HttpGet("{petId}/owner")]
        public async Task<IActionResult> GetPetOwner(int petId)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                var query = "SELECT OwnerID FROM petprofiles WHERE PetID = @PetID";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@PetID", petId);

                var ownerId = await command.ExecuteScalarAsync();
                if (ownerId != null)
                {
                    return Ok(Convert.ToInt32(ownerId));
                }

                return NotFound("כלב לא נמצא");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בטעינת פרטי הבעלים: {ex.Message}");
            }
        }
    }
} 