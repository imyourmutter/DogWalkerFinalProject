using Microsoft.AspNetCore.Mvc;
using DogWalkerApi.Services;
using DogWalkerClassLibrary.Models;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Data;
using BCrypt.Net;

namespace DogWalkerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly DatabaseService _databaseService;

        public UserController(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            using var connection = _databaseService.GetConnection();
            await connection.OpenAsync();

            // First get the user's hashed password
            string query = "SELECT UserId, UserRole, Password FROM userprofiles WHERE Username = @Username";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", request.Username);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var storedHash = reader.GetString("Password");
                var userId = reader.GetInt32("UserId");
                var userRole = reader.GetInt32("UserRole");

                // Verify the password using BCrypt
                if (BCrypt.Net.BCrypt.Verify(request.Password, storedHash))
                {
                    if (userRole == 5) // Banned user
                    {
                        return BadRequest("משתמש זה חסום במערכת");
                    }
                    return Ok(userId.ToString());
                }
            }
            return BadRequest("פרטי התחברות שגויים");
        }

        [HttpGet("role/{userId}")]
        public async Task<IActionResult> GetUserRole(int userId)
        {
            using var connection = _databaseService.GetConnection();
            await connection.OpenAsync();

            string query = "SELECT UserRole FROM userprofiles WHERE UserId = @UserId";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            var userRole = await command.ExecuteScalarAsync();

            if (userRole != null)
            {
                return Ok(Convert.ToInt32(userRole));
            }
            return NotFound("User not found");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegistrationModel model)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                // Check if username exists
                string checkUsernameQuery = "SELECT COUNT(*) FROM userprofiles WHERE Username = @Username";
                using var checkCommand = new MySqlCommand(checkUsernameQuery, connection);
                checkCommand.Parameters.AddWithValue("@Username", model.Username);
                int existingCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                if (existingCount > 0)
                {
                    return BadRequest("שם המשתמש כבר קיים במערכת");
                }

                // Get next UserID
                string maxIdQuery = "SELECT COALESCE(MAX(UserID), 0) FROM userprofiles";
                using var maxIdCommand = new MySqlCommand(maxIdQuery, connection);
                int nextUserId = Convert.ToInt32(await maxIdCommand.ExecuteScalarAsync()) + 1;

                // Hash the password using BCrypt
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

                // Insert user
                string insertUserQuery = @"
                    INSERT INTO userprofiles 
                    (UserID, Username, Password, FullName, Email, PhoneNumber, Address, UserRole, AverageRating)
                    VALUES 
                    (@UserID, @Username, @Password, @FullName, @Email, @PhoneNumber, @Address, @UserRole, NULL)";

                using var insertCommand = new MySqlCommand(insertUserQuery, connection);
                insertCommand.Parameters.AddWithValue("@UserID", nextUserId);
                insertCommand.Parameters.AddWithValue("@Username", model.Username);
                insertCommand.Parameters.AddWithValue("@Password", hashedPassword);
                insertCommand.Parameters.AddWithValue("@FullName", model.FullName);
                insertCommand.Parameters.AddWithValue("@Email", model.Email);
                insertCommand.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@Address", model.Address);
                insertCommand.Parameters.AddWithValue("@UserRole", model.UserRole);

                await insertCommand.ExecuteNonQueryAsync();

                // If user is a dog owner, insert pets
                if (model.UserRole == 0 && model.Pets.Any())
                {
                    // Get next PetID
                    string maxPetIdQuery = "SELECT COALESCE(MAX(PetID), 0) FROM petprofiles";
                    using var maxPetIdCommand = new MySqlCommand(maxPetIdQuery, connection);
                    int nextPetId = Convert.ToInt32(await maxPetIdCommand.ExecuteScalarAsync()) + 1;

                    string insertPetQuery = @"
                        INSERT INTO petprofiles 
                        (PetID, OwnerID, PetName, Breed, Weight, Allergies, SpecialNeeds)
                        VALUES 
                        (@PetID, @OwnerID, @PetName, @Breed, @Weight, @Allergies, @SpecialNeeds)";

                    foreach (var pet in model.Pets)
                    {
                        using var insertPetCommand = new MySqlCommand(insertPetQuery, connection);
                        insertPetCommand.Parameters.AddWithValue("@PetID", nextPetId++);
                        insertPetCommand.Parameters.AddWithValue("@OwnerID", nextUserId);
                        insertPetCommand.Parameters.AddWithValue("@PetName", pet.PetName);
                        insertPetCommand.Parameters.AddWithValue("@Breed", pet.Breed);
                        insertPetCommand.Parameters.AddWithValue("@Weight", pet.Weight);
                        insertPetCommand.Parameters.AddWithValue("@Allergies", pet.Allergies ?? (object)DBNull.Value);
                        insertPetCommand.Parameters.AddWithValue("@SpecialNeeds", pet.SpecialNeeds ?? (object)DBNull.Value);

                        await insertPetCommand.ExecuteNonQueryAsync();
                    }
                }

                var response = new { UserId = nextUserId.ToString() };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בהרשמה: {ex.Message}");
            }
        }

        [HttpGet("profile/{userId}")]
        public async Task<IActionResult> GetProfile(int userId)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                // Get user profile
                string query = @"
                    SELECT UserID, Username, Password, FullName, Email, PhoneNumber, Address, UserRole, AverageRating 
                    FROM userprofiles 
                    WHERE UserID = @UserId";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return NotFound("משתמש לא נמצא");
                }

                var profile = new RegistrationModel
                {
                    Username = reader.GetString("Username"),
                    Password = "", // Don't send the hashed password to the client
                    FullName = reader.GetString("FullName"),
                    Email = reader.GetString("Email"),
                    PhoneNumber = reader.IsDBNull("PhoneNumber") ? null : reader.GetString("PhoneNumber"),
                    Address = reader.GetString("Address"),
                    UserRole = reader.GetInt32("UserRole")
                };

                var averageRating = reader.IsDBNull("AverageRating") ? null : (float?)reader.GetFloat("AverageRating");

                reader.Close();

                // If user is a dog owner, get their pets
                if (profile.UserRole == 0)
                {
                    string petsQuery = @"
                        SELECT PetID, PetName, Breed, Weight, Allergies, SpecialNeeds 
                        FROM petprofiles 
                        WHERE OwnerID = @OwnerId";
                    using var petsCommand = new MySqlCommand(petsQuery, connection);
                    petsCommand.Parameters.AddWithValue("@OwnerId", userId);

                    using var petsReader = await petsCommand.ExecuteReaderAsync();

                    var pets = new List<object>();
                    while (await petsReader.ReadAsync())
                    {
                        profile.Pets.Add(new PetModel
                        {
                            PetId = petsReader.GetInt32("PetID"),
                            PetName = petsReader.GetString("PetName"),
                            Breed = petsReader.GetString("Breed"),
                            Weight = petsReader.GetFloat("Weight"),
                            Allergies = petsReader.IsDBNull("Allergies") ? null : petsReader.GetString("Allergies"),
                            SpecialNeeds = petsReader.IsDBNull("SpecialNeeds") ? null : petsReader.GetString("SpecialNeeds")
                        });
                    }
                }

                var response = new
                {
                    profile.Username,
                    Password = "", // Don't send the hashed password to the client
                    profile.FullName,
                    profile.Email,
                    profile.PhoneNumber,
                    profile.Address,
                    profile.UserRole,
                    AverageRating = averageRating,
                    profile.Pets
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בטעינת הפרופיל: {ex.Message}");
            }
        }

        [HttpPost("profile/update/{userId}")]
        public async Task<IActionResult> UpdateProfile(int userId, [FromBody] RegistrationModel request)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                // Check if new username exists (if username is being changed)
                if (!string.IsNullOrEmpty(request.Username))
                {
                    string checkUsernameQuery = "SELECT COUNT(*) FROM userprofiles WHERE Username = @Username AND UserID != @UserId";
                    using var checkCommand = new MySqlCommand(checkUsernameQuery, connection);
                    checkCommand.Parameters.AddWithValue("@Username", request.Username);
                    checkCommand.Parameters.AddWithValue("@UserId", userId);
                    int existingCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                    if (existingCount > 0)
                    {
                        return BadRequest("שם המשתמש כבר קיים במערכת");
                    }
                }

                // Get current password hash
                string getCurrentPasswordQuery = "SELECT Password FROM userprofiles WHERE UserID = @UserId";
                using var getCurrentPasswordCommand = new MySqlCommand(getCurrentPasswordQuery, connection);
                getCurrentPasswordCommand.Parameters.AddWithValue("@UserId", userId);
                string? currentPasswordHash = (string?)await getCurrentPasswordCommand.ExecuteScalarAsync();

                // Only update password if it's provided and different
                string passwordToSave;
                if (!string.IsNullOrEmpty(request.Password))
                {
                    // Hash the new password if it's different from the current one
                    if (currentPasswordHash == null || !BCrypt.Net.BCrypt.Verify(request.Password, currentPasswordHash))
                    {
                        passwordToSave = BCrypt.Net.BCrypt.HashPassword(request.Password);
                    }
                    else
                    {
                        passwordToSave = currentPasswordHash; // Keep existing hash if password hasn't changed
                    }
                }
                else
                {
                    passwordToSave = currentPasswordHash ?? ""; // Keep existing hash if no new password provided
                }

                // Update user profile
                string updateQuery = @"
                    UPDATE userprofiles 
                    SET Username = @Username,
                        Password = @Password,
                        FullName = @FullName,
                        Email = @Email,
                        PhoneNumber = @PhoneNumber,
                        Address = @Address
                    WHERE UserID = @UserId";

                using var updateCommand = new MySqlCommand(updateQuery, connection);
                updateCommand.Parameters.AddWithValue("@UserId", userId);
                updateCommand.Parameters.AddWithValue("@Username", request.Username);
                updateCommand.Parameters.AddWithValue("@Password", passwordToSave);
                updateCommand.Parameters.AddWithValue("@FullName", request.FullName);
                updateCommand.Parameters.AddWithValue("@Email", request.Email);
                updateCommand.Parameters.AddWithValue("@PhoneNumber", request.PhoneNumber ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@Address", request.Address);

                await updateCommand.ExecuteNonQueryAsync();

                // If user is a dog owner, update pets
                if (request.UserRole == 0 && request.Pets != null)
                {
                    // Delete all existing pets
                    string deletePetsQuery = "DELETE FROM petprofiles WHERE OwnerID = @OwnerId";
                    using var deletePetsCommand = new MySqlCommand(deletePetsQuery, connection);
                    deletePetsCommand.Parameters.AddWithValue("@OwnerId", userId);
                    await deletePetsCommand.ExecuteNonQueryAsync();

                    // Add new pets
                    if (request.Pets.Any())
                    {
                        string maxPetIdQuery = "SELECT COALESCE(MAX(PetID), 0) FROM petprofiles";
                        using var maxPetIdCommand = new MySqlCommand(maxPetIdQuery, connection);
                        int nextPetId = Convert.ToInt32(await maxPetIdCommand.ExecuteScalarAsync()) + 1;

                        string insertPetQuery = @"
                            INSERT INTO petprofiles 
                            (PetID, OwnerID, PetName, Breed, Weight, Allergies, SpecialNeeds)
                            VALUES 
                            (@PetID, @OwnerID, @PetName, @Breed, @Weight, @Allergies, @SpecialNeeds)";

                        foreach (var pet in request.Pets)
                        {
                            using var insertPetCommand = new MySqlCommand(insertPetQuery, connection);
                            insertPetCommand.Parameters.AddWithValue("@PetID", nextPetId++);
                            insertPetCommand.Parameters.AddWithValue("@OwnerID", userId);
                            insertPetCommand.Parameters.AddWithValue("@PetName", pet.PetName);
                            insertPetCommand.Parameters.AddWithValue("@Breed", pet.Breed);
                            insertPetCommand.Parameters.AddWithValue("@Weight", pet.Weight);
                            insertPetCommand.Parameters.AddWithValue("@Allergies", pet.Allergies ?? (object)DBNull.Value);
                            insertPetCommand.Parameters.AddWithValue("@SpecialNeeds", pet.SpecialNeeds ?? (object)DBNull.Value);

                            await insertPetCommand.ExecuteNonQueryAsync();
                        }
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בעדכון הפרופיל: {ex.Message}");
            }
        }

        [HttpPost("ban/{userId}")]
        public async Task<IActionResult> BanUser(int userId)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                // Check if target user is an admin
                string checkRoleQuery = "SELECT UserRole FROM userprofiles WHERE UserID = @UserId";
                using var checkCommand = new MySqlCommand(checkRoleQuery, connection);
                checkCommand.Parameters.AddWithValue("@UserId", userId);
                var currentRole = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                if (currentRole == 4)
                {
                    return BadRequest("לא ניתן לחסום מנהל מערכת");
                }

                string query = "UPDATE userprofiles SET UserRole = 5 WHERE UserID = @UserId";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                await command.ExecuteNonQueryAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בחסימת המשתמש: {ex.Message}");
            }
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                // Check if target user is an admin
                string checkRoleQuery = "SELECT UserRole FROM userprofiles WHERE UserID = @UserId";
                using var checkCommand = new MySqlCommand(checkRoleQuery, connection);
                checkCommand.Parameters.AddWithValue("@UserId", userId);
                var currentRole = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                if (currentRole == 4)
                {
                    return BadRequest("לא ניתן למחוק מנהל מערכת");
                }

                // Start a transaction since we're deleting from multiple tables
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    // Delete user's pets (if they are a dog owner)
                    string deletePetsQuery = "DELETE FROM petprofiles WHERE OwnerID = @UserId";
                    using var deletePetsCommand = new MySqlCommand(deletePetsQuery, connection);
                    deletePetsCommand.Parameters.AddWithValue("@UserId", userId);
                    deletePetsCommand.Transaction = transaction;
                    await deletePetsCommand.ExecuteNonQueryAsync();

                    // Delete user's reviews (both as reviewer and provider)
                    string deleteReviewsQuery = "DELETE FROM reviewsandratings WHERE ReviewerID = @UserId OR AppointmentID IN (SELECT AppointmentID FROM appointments WHERE ProviderID = @UserId)";
                    using var deleteReviewsCommand = new MySqlCommand(deleteReviewsQuery, connection);
                    deleteReviewsCommand.Parameters.AddWithValue("@UserId", userId);
                    deleteReviewsCommand.Transaction = transaction;
                    await deleteReviewsCommand.ExecuteNonQueryAsync();

                    // Delete user's messages (both sent and received)
                    string deleteMessagesQuery = "DELETE FROM messages WHERE SenderID = @UserId OR ReceiverID = @UserId";
                    using var deleteMessagesCommand = new MySqlCommand(deleteMessagesQuery, connection);
                    deleteMessagesCommand.Parameters.AddWithValue("@UserId", userId);
                    deleteMessagesCommand.Transaction = transaction;
                    await deleteMessagesCommand.ExecuteNonQueryAsync();

                    // Delete user's appointments (both as owner and provider)
                    string deleteAppointmentsQuery = "DELETE FROM appointments WHERE ProviderID = @UserId OR PetID IN (SELECT PetID FROM petprofiles WHERE OwnerID = @UserId)";
                    using var deleteAppointmentsCommand = new MySqlCommand(deleteAppointmentsQuery, connection);
                    deleteAppointmentsCommand.Parameters.AddWithValue("@UserId", userId);
                    deleteAppointmentsCommand.Transaction = transaction;
                    await deleteAppointmentsCommand.ExecuteNonQueryAsync();

                    // Delete user's service provider availabilities
                    string deleteAvailabilitiesQuery = "DELETE FROM serviceprovideravailability WHERE ProviderID = @UserId";
                    using var deleteAvailabilitiesCommand = new MySqlCommand(deleteAvailabilitiesQuery, connection);
                    deleteAvailabilitiesCommand.Parameters.AddWithValue("@UserId", userId);
                    deleteAvailabilitiesCommand.Transaction = transaction;
                    await deleteAvailabilitiesCommand.ExecuteNonQueryAsync();

                    // Delete user's reports (both as reporter and reported)
                    string deleteReportsQuery = "DELETE FROM reports WHERE ReporterID = @UserId OR ReportedID = @UserId";
                    using var deleteReportsCommand = new MySqlCommand(deleteReportsQuery, connection);
                    deleteReportsCommand.Parameters.AddWithValue("@UserId", userId);
                    deleteReportsCommand.Transaction = transaction;
                    await deleteReportsCommand.ExecuteNonQueryAsync();

                    // Finally, delete the user profile
                    string deleteUserQuery = "DELETE FROM userprofiles WHERE UserID = @UserId";
                    using var deleteUserCommand = new MySqlCommand(deleteUserQuery, connection);
                    deleteUserCommand.Parameters.AddWithValue("@UserId", userId);
                    deleteUserCommand.Transaction = transaction;
                    await deleteUserCommand.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();
                    return Ok();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה במחיקת המשתמש: {ex.Message}");
            }
        }
    }
}