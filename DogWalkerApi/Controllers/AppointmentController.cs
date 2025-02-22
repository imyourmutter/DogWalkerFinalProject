using Microsoft.AspNetCore.Mvc;
using DogWalkerApi.Services;
using MySql.Data.MySqlClient;
using System.Data;

namespace DogWalkerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentController : ControllerBase
    {
        private readonly DatabaseService _databaseService;

        public AppointmentController(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        [HttpGet("owner/{userId}")]
        public async Task<IActionResult> GetOwnerAppointments(int userId, [FromQuery] string? status = null, [FromQuery] string? serviceType = null)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        a.AppointmentID,
                        a.PetID,
                        a.ProviderID,
                        a.ServiceType,
                        a.AppointmentDate,
                        a.StartTime,
                        a.EndTime,
                        a.Status,
                        p.PetName,
                        u.Username as ProviderName,
                        u.FullName as ProviderFullName
                    FROM appointments a
                    JOIN petprofiles p ON a.PetID = p.PetID
                    JOIN userprofiles u ON a.ProviderID = u.UserID
                    WHERE p.OwnerID = @UserId";

                if (!string.IsNullOrEmpty(status))
                {
                    query += " AND a.Status = @Status";
                }
                if (!string.IsNullOrEmpty(serviceType))
                {
                    query += " AND a.ServiceType = @ServiceType";
                }
                query += " ORDER BY a.AppointmentDate DESC, a.StartTime DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                if (!string.IsNullOrEmpty(status))
                {
                    command.Parameters.AddWithValue("@Status", status);
                }
                if (!string.IsNullOrEmpty(serviceType))
                {
                    command.Parameters.AddWithValue("@ServiceType", serviceType);
                }

                using var reader = await command.ExecuteReaderAsync();
                var appointments = new List<object>();

                while (await reader.ReadAsync())
                {
                    appointments.Add(new
                    {
                        AppointmentId = reader.GetInt32("AppointmentID"),
                        PetId = reader.GetInt32("PetID"),
                        PetName = reader.GetString("PetName"),
                        ProviderId = reader.GetInt32("ProviderID"),
                        ProviderName = reader.GetString("ProviderName"),
                        ProviderFullName = reader.GetString("ProviderFullName"),
                        ServiceType = reader.GetString("ServiceType"),
                        AppointmentDate = reader.GetDateTime("AppointmentDate"),
                        StartTime = ((TimeSpan)(reader.GetValue("StartTime"))),
                        EndTime = ((TimeSpan)(reader.GetValue("EndTime"))),
                        Status = reader.GetString("Status")
                    });
                }

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בטעינת הפגישות: {ex.Message}");
            }
        }

        [HttpGet("provider/{userId}")]
        public async Task<IActionResult> GetProviderAppointments(int userId, [FromQuery] string? status = null)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        a.AppointmentID,
                        a.PetID,
                        a.ProviderID,
                        a.ServiceType,
                        a.AppointmentDate,
                        a.StartTime,
                        a.EndTime,
                        a.Status,
                        p.PetName,
                        u.Username as OwnerName,
                        u.FullName as OwnerFullName
                    FROM appointments a
                    JOIN petprofiles p ON a.PetID = p.PetID
                    JOIN userprofiles u ON p.OwnerID = u.UserID
                    WHERE a.ProviderID = @UserId";

                if (!string.IsNullOrEmpty(status))
                {
                    query += " AND a.Status = @Status";
                }
                query += " ORDER BY a.AppointmentDate DESC, a.StartTime DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                if (!string.IsNullOrEmpty(status))
                {
                    command.Parameters.AddWithValue("@Status", status);
                }

                using var reader = await command.ExecuteReaderAsync();
                var appointments = new List<object>();

                while (await reader.ReadAsync())
                {
                    appointments.Add(new
                    {
                        AppointmentId = reader.GetInt32("AppointmentID"),
                        PetId = reader.GetInt32("PetID"),
                        PetName = reader.GetString("PetName"),
                        ProviderId = reader.GetInt32("ProviderID"),
                        OwnerName = reader.GetString("OwnerName"),
                        OwnerFullName = reader.GetString("OwnerFullName"),
                        ServiceType = reader.GetString("ServiceType"),
                        AppointmentDate = reader.GetDateTime("AppointmentDate"),
                        StartTime = ((TimeSpan)(reader.GetValue("StartTime"))),
                        EndTime = ((TimeSpan)(reader.GetValue("EndTime"))),
                        Status = reader.GetString("Status")
                    });
                }

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בטעינת הפגישות: {ex.Message}");
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllAppointments([FromQuery] string? status = null, [FromQuery] string? serviceType = null)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        a.AppointmentID,
                        a.PetID,
                        a.ProviderID,
                        a.ServiceType,
                        a.AppointmentDate,
                        a.StartTime,
                        a.EndTime,
                        a.Status,
                        p.PetName,
                        o.Username as OwnerName,
                        o.FullName as OwnerFullName,
                        pr.Username as ProviderName,
                        pr.FullName as ProviderFullName
                    FROM appointments a
                    JOIN petprofiles p ON a.PetID = p.PetID
                    JOIN userprofiles o ON p.OwnerID = o.UserID
                    JOIN userprofiles pr ON a.ProviderID = pr.UserID
                    WHERE 1=1";

                if (!string.IsNullOrEmpty(status))
                {
                    query += " AND a.Status = @Status";
                }
                if (!string.IsNullOrEmpty(serviceType))
                {
                    query += " AND a.ServiceType = @ServiceType";
                }
                query += " ORDER BY a.AppointmentDate DESC, a.StartTime DESC";

                using var command = new MySqlCommand(query, connection);
                if (!string.IsNullOrEmpty(status))
                {
                    command.Parameters.AddWithValue("@Status", status);
                }
                if (!string.IsNullOrEmpty(serviceType))
                {
                    command.Parameters.AddWithValue("@ServiceType", serviceType);
                }

                using var reader = await command.ExecuteReaderAsync();
                var appointments = new List<object>();

                while (await reader.ReadAsync())
                {
                    appointments.Add(new
                    {
                        AppointmentId = reader.GetInt32("AppointmentID"),
                        PetId = reader.GetInt32("PetID"),
                        PetName = reader.GetString("PetName"),
                        ProviderId = reader.GetInt32("ProviderID"),
                        ProviderName = reader.GetString("ProviderName"),
                        ProviderFullName = reader.GetString("ProviderFullName"),
                        OwnerName = reader.GetString("OwnerName"),
                        OwnerFullName = reader.GetString("OwnerFullName"),
                        ServiceType = reader.GetString("ServiceType"),
                        AppointmentDate = reader.GetDateTime("AppointmentDate"),
                        StartTime = ((TimeSpan)(reader.GetValue("StartTime"))),
                        EndTime = ((TimeSpan)(reader.GetValue("EndTime"))),
                        Status = reader.GetString("Status")
                    });
                }

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בטעינת הפגישות: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequest request)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                // Get next AppointmentID
                string maxIdQuery = "SELECT COALESCE(MAX(AppointmentID), 0) FROM appointments";
                using var maxIdCommand = new MySqlCommand(maxIdQuery, connection);
                int nextAppointmentId = Convert.ToInt32(await maxIdCommand.ExecuteScalarAsync()) + 1;

                string query = @"
                    INSERT INTO appointments 
                    (AppointmentID, PetID, ProviderID, ServiceType, AppointmentDate, StartTime, EndTime, Status)
                    VALUES 
                    (@AppointmentID, @PetID, @ProviderID, @ServiceType, @AppointmentDate, @StartTime, @EndTime, 'pending')";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@AppointmentID", nextAppointmentId);
                command.Parameters.AddWithValue("@PetID", request.PetId);
                command.Parameters.AddWithValue("@ProviderID", request.ProviderId);
                command.Parameters.AddWithValue("@ServiceType", request.ServiceType);
                command.Parameters.AddWithValue("@AppointmentDate", request.AppointmentDate.Date);
                command.Parameters.AddWithValue("@StartTime", request.StartTime);
                command.Parameters.AddWithValue("@EndTime", request.EndTime);

                await command.ExecuteNonQueryAsync();
                return Ok(new { AppointmentId = nextAppointmentId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה ביצירת הפגישה: {ex.Message}");
            }
        }

        [HttpPut("status/{appointmentId}")]
        public async Task<IActionResult> UpdateAppointmentStatus(int appointmentId, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                string query = "UPDATE appointments SET Status = @Status WHERE AppointmentID = @AppointmentID";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@AppointmentID", appointmentId);
                command.Parameters.AddWithValue("@Status", request.Status);

                await command.ExecuteNonQueryAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בעדכון סטטוס הפגישה: {ex.Message}");
            }
        }

        [HttpDelete("{appointmentId}")]
        public async Task<IActionResult> DeleteAppointment(int appointmentId)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                string query = "DELETE FROM appointments WHERE AppointmentID = @AppointmentID";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@AppointmentID", appointmentId);

                await command.ExecuteNonQueryAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה במחיקת הפגישה: {ex.Message}");
            }
        }
    }

    public class CreateAppointmentRequest
    {
        public int PetId { get; set; }
        public int ProviderId { get; set; }
        public string ServiceType { get; set; } = "";
        public DateTime AppointmentDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = "";
    }
} 