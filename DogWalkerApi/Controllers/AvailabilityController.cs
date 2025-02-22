using Microsoft.AspNetCore.Mvc;
using DogWalkerApi.Services;
using DogWalkerClassLibrary.Models;
using MySql.Data.MySqlClient;
using System.Data;

namespace DogWalkerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AvailabilityController : ControllerBase
    {
        private readonly DatabaseService _databaseService;

        public AvailabilityController(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        private TimeSpan GetTimeFromReader(MySqlDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return TimeSpan.Zero;

            return reader.GetTimeSpan(ordinal);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllAvailabilities([FromQuery] string? location = null, [FromQuery] float? minRating = null, [FromQuery] int? serviceType = null)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                var conditions = new List<string>();
                var parameters = new List<MySqlParameter>();

                if (!string.IsNullOrEmpty(location))
                {
                    conditions.Add("u.Address LIKE @Location");
                    parameters.Add(new MySqlParameter("@Location", $"%{location}%"));
                }

                if (minRating.HasValue)
                {
                    conditions.Add("(u.AverageRating >= @MinRating OR u.AverageRating IS NULL)");
                    parameters.Add(new MySqlParameter("@MinRating", minRating.Value));
                }

                if (serviceType.HasValue)
                {
                    conditions.Add("u.UserRole = @ServiceType");
                    parameters.Add(new MySqlParameter("@ServiceType", serviceType.Value));
                }

                string whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";

                string query = $@"
                    SELECT 
                        a.AvailabilityID,
                        a.ProviderID,
                        a.ProviderType,
                        a.Date,
                        a.StartTime,
                        a.EndTime,
                        u.FullName as ProviderName,
                        u.PhoneNumber,
                        u.Address,
                        COALESCE(u.AverageRating, 0) as AverageRating,
                        u.UserRole
                    FROM serviceprovideravailability a
                    JOIN userprofiles u ON a.ProviderID = u.UserID
                    {whereClause}
                    ORDER BY a.Date, a.StartTime";

                using var command = new MySqlCommand(query, connection);
                foreach (var param in parameters)
                {
                    command.Parameters.Add(param);
                }

                using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();
                
                var availabilities = new List<ServiceProviderAvailability>();
                
                while (await reader.ReadAsync())
                {
                    availabilities.Add(new ServiceProviderAvailability
                    {
                        AvailabilityID = reader.GetInt32("AvailabilityID"),
                        ProviderID = reader.GetInt32("ProviderID"),
                        ProviderType = reader.GetString("ProviderType"),
                        Date = reader.GetDateTime("Date"),
                        StartTime = GetTimeFromReader(reader, "StartTime"),
                        EndTime = GetTimeFromReader(reader, "EndTime"),
                        ProviderName = reader.GetString("ProviderName"),
                        PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString("PhoneNumber"),
                        Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString("Address"),
                        AverageRating = reader.IsDBNull(reader.GetOrdinal("AverageRating")) ? null : (float?)reader.GetFloat("AverageRating"),
                        UserRole = reader.GetInt32("UserRole")
                    });
                }

                return Ok(availabilities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("provider/{providerId}")]
        public async Task<IActionResult> GetProviderAvailabilities(int providerId)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                string query = @"
                    SELECT 
                        AvailabilityID,
                        ProviderID,
                        ProviderType,
                        Date,
                        StartTime,
                        EndTime
                    FROM serviceprovideravailability
                    WHERE ProviderID = @ProviderId
                    ORDER BY Date, StartTime";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ProviderId", providerId);
                using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();
                
                var availabilities = new List<ServiceProviderAvailability>();
                
                while (await reader.ReadAsync())
                {
                    availabilities.Add(new ServiceProviderAvailability
                    {
                        AvailabilityID = reader.GetInt32("AvailabilityID"),
                        ProviderID = reader.GetInt32("ProviderID"),
                        ProviderType = reader.GetString("ProviderType"),
                        Date = reader.GetDateTime("Date"),
                        StartTime = GetTimeFromReader(reader, "StartTime"),
                        EndTime = GetTimeFromReader(reader, "EndTime")
                    });
                }

                return Ok(availabilities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateAvailability([FromBody] NewAvailabilityRequest request)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                // First get the provider type based on UserRole
                string userRoleQuery = "SELECT UserRole FROM userprofiles WHERE UserID = @UserId";
                using var userRoleCommand = new MySqlCommand(userRoleQuery, connection);
                userRoleCommand.Parameters.AddWithValue("@UserId", request.ProviderID);
                var userRole = await userRoleCommand.ExecuteScalarAsync();

                if (userRole == null)
                {
                    return BadRequest("Provider not found");
                }

                string providerType = ((int)userRole) switch
                {
                    1 => "walker",
                    2 => "groomer",
                    3 => "vet",
                    _ => throw new Exception("Invalid provider type")
                };

                string query = @"
                    INSERT INTO serviceprovideravailability 
                    (ProviderID, ProviderType, Date, StartTime, EndTime)
                    VALUES 
                    (@ProviderID, @ProviderType, @Date, @StartTime, @EndTime)";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ProviderID", request.ProviderID);
                command.Parameters.AddWithValue("@ProviderType", providerType);
                command.Parameters.AddWithValue("@Date", request.Date.Date);
                command.Parameters.AddWithValue("@StartTime", request.StartTime);
                command.Parameters.AddWithValue("@EndTime", request.EndTime);

                await command.ExecuteNonQueryAsync();
                return Ok("Availability created successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{availabilityId}")]
        public async Task<IActionResult> DeleteAvailability(int availabilityId)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                string query = "DELETE FROM serviceprovideravailability WHERE AvailabilityID = @AvailabilityId";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@AvailabilityId", availabilityId);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected > 0)
                {
                    return Ok("Availability deleted successfully");
                }
                return NotFound("Availability not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
} 