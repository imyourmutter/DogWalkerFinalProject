using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using DogWalkerApi.Services;
using System.Data;

namespace DogWalkerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly DatabaseService _databaseService;

        public ReportController(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportRequest request)
        {
            if (request.Description.Length > 2000)
            {
                return BadRequest("תיאור הדיווח ארוך מדי. אנא קצר אותו ל-2000 תווים.");
            }

            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                // Get next ReportID
                string maxIdQuery = "SELECT COALESCE(MAX(ReportID), 0) FROM reports";
                using var maxIdCommand = new MySqlCommand(maxIdQuery, connection);
                int nextReportId = Convert.ToInt32(await maxIdCommand.ExecuteScalarAsync()) + 1;

                // Insert report
                string insertQuery = @"
                    INSERT INTO reports 
                    (ReportID, ReporterID, ReportedID, Description, ReportDate, Status)
                    VALUES 
                    (@ReportID, @ReporterID, @ReportedID, @Description, @ReportDate, @Status)";

                using var insertCommand = new MySqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@ReportID", nextReportId);
                insertCommand.Parameters.AddWithValue("@ReporterID", request.ReporterId);
                insertCommand.Parameters.AddWithValue("@ReportedID", request.ReportedId);
                insertCommand.Parameters.AddWithValue("@Description", request.Description);
                insertCommand.Parameters.AddWithValue("@ReportDate", DateTime.Now);
                insertCommand.Parameters.AddWithValue("@Status", "pending");

                await insertCommand.ExecuteNonQueryAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה ביצירת הדיווח: {ex.Message}");
            }
        }

        [HttpGet("by/{userId}")]
        public async Task<IActionResult> GetReportsByUser(int userId, [FromQuery] string? status = null)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                string query = @"
                    SELECT 
                        r.ReportID,
                        r.ReporterID,
                        r.ReportedID,
                        reporter.Username as ReporterName,
                        reported.Username as ReportedName,
                        r.Description,
                        r.ReportDate,
                        r.Status
                    FROM reports r
                    JOIN userprofiles reporter ON r.ReporterID = reporter.UserID
                    JOIN userprofiles reported ON r.ReportedID = reported.UserID
                    WHERE r.ReporterID = @UserId";

                if (!string.IsNullOrEmpty(status))
                {
                    query += " AND r.Status = @Status";
                }
                query += " ORDER BY r.ReportDate DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                if (!string.IsNullOrEmpty(status))
                {
                    command.Parameters.AddWithValue("@Status", status);
                }

                using var reader = await command.ExecuteReaderAsync();
                var reports = new List<object>();

                while (await reader.ReadAsync())
                {
                    reports.Add(new
                    {
                        ReportId = reader.GetInt32("ReportID"),
                        ReporterId = reader.GetInt32("ReporterID"),
                        ReportedId = reader.GetInt32("ReportedID"),
                        ReporterName = reader.GetString("ReporterName"),
                        ReportedName = reader.GetString("ReportedName"),
                        Description = reader.GetString("Description"),
                        ReportDate = reader.GetDateTime("ReportDate"),
                        Status = reader.GetString("Status")
                    });
                }

                return Ok(reports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בטעינת הדיווחים: {ex.Message}");
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllReports([FromQuery] string? status = null)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                string query = @"
                    SELECT 
                        r.ReportID,
                        r.ReporterID,
                        r.ReportedID,
                        reporter.Username as ReporterName,
                        reported.Username as ReportedName,
                        r.Description,
                        r.ReportDate,
                        r.Status
                    FROM reports r
                    JOIN userprofiles reporter ON r.ReporterID = reporter.UserID
                    JOIN userprofiles reported ON r.ReportedID = reported.UserID";

                if (!string.IsNullOrEmpty(status))
                {
                    query += " WHERE r.Status = @Status";
                }
                query += " ORDER BY r.ReportDate DESC";

                using var command = new MySqlCommand(query, connection);
                if (!string.IsNullOrEmpty(status))
                {
                    command.Parameters.AddWithValue("@Status", status);
                }

                using var reader = await command.ExecuteReaderAsync();
                var reports = new List<object>();

                while (await reader.ReadAsync())
                {
                    reports.Add(new
                    {
                        ReportId = reader.GetInt32("ReportID"),
                        ReporterId = reader.GetInt32("ReporterID"),
                        ReportedId = reader.GetInt32("ReportedID"),
                        ReporterName = reader.GetString("ReporterName"),
                        ReportedName = reader.GetString("ReportedName"),
                        Description = reader.GetString("Description"),
                        ReportDate = reader.GetDateTime("ReportDate"),
                        Status = reader.GetString("Status")
                    });
                }

                return Ok(reports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בטעינת הדיווחים: {ex.Message}");
            }
        }

        [HttpPut("{reportId}/status")]
        public async Task<IActionResult> UpdateReportStatus(int reportId, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                string query = "UPDATE reports SET Status = @Status WHERE ReportID = @ReportID";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Status", request.Status);
                command.Parameters.AddWithValue("@ReportID", reportId);

                await command.ExecuteNonQueryAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בעדכון סטטוס הדיווח: {ex.Message}");
            }
        }

        public class CreateReportRequest
        {
            public int ReporterId { get; set; }
            public int ReportedId { get; set; }
            public string Description { get; set; } = "";
        }

        public class UpdateStatusRequest
        {
            public string Status { get; set; } = "";
        }
    }
} 