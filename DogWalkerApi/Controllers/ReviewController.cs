using Microsoft.AspNetCore.Mvc;
using DogWalkerApi.Services;
using MySql.Data.MySqlClient;
using System.Data;

namespace DogWalkerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewController : ControllerBase
    {
        private readonly DatabaseService _databaseService;

        public ReviewController(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        [HttpGet("owner/{userId}")]
        public async Task<IActionResult> GetOwnerReviews(int userId)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                string query = @"
                    SELECT r.ReviewID, r.AppointmentID, r.ReviewerID, r.Rating, r.ReviewText, r.ReviewDate,
                           u.Username as ReviewerUsername, u.FullName as ReviewerFullName,
                           a.ProviderID, p.Username as ProviderUsername, p.FullName as ProviderFullName
                    FROM reviewsandratings r
                    JOIN appointments a ON r.AppointmentID = a.AppointmentID
                    JOIN petprofiles pet ON a.PetID = pet.PetID
                    JOIN userprofiles u ON r.ReviewerID = u.UserID
                    JOIN userprofiles p ON a.ProviderID = p.UserID
                    WHERE pet.OwnerID = @UserId
                    ORDER BY r.ReviewDate DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();
                var reviews = new List<object>();

                while (await reader.ReadAsync())
                {
                    reviews.Add(new
                    {
                        ReviewId = reader.GetInt32("ReviewID"),
                        AppointmentId = reader.GetInt32("AppointmentID"),
                        ReviewerId = reader.GetInt32("ReviewerID"),
                        ReviewerUsername = reader.GetString("ReviewerUsername"),
                        ReviewerFullName = reader.GetString("ReviewerFullName"),
                        ProviderId = reader.GetInt32("ProviderID"),
                        ProviderUsername = reader.GetString("ProviderUsername"),
                        ProviderFullName = reader.GetString("ProviderFullName"),
                        Rating = reader.GetInt32("Rating"),
                        ReviewText = reader.GetString("ReviewText"),
                        ReviewDate = reader.GetDateTime("ReviewDate")
                    });
                }

                return Ok(reviews);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בטעינת הביקורות: {ex.Message}");
            }
        }

        [HttpGet("provider/{userId}")]
        public async Task<IActionResult> GetProviderReviews(int userId)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                string query = @"
                    SELECT r.ReviewID, r.AppointmentID, r.ReviewerID, r.Rating, r.ReviewText, r.ReviewDate,
                           u.Username as ReviewerUsername, u.FullName as ReviewerFullName,
                           o.UserID as OwnerID, o.Username as OwnerUsername, o.FullName as OwnerFullName
                    FROM reviewsandratings r
                    JOIN appointments a ON r.AppointmentID = a.AppointmentID
                    JOIN petprofiles pet ON a.PetID = pet.PetID
                    JOIN userprofiles u ON r.ReviewerID = u.UserID
                    JOIN userprofiles o ON pet.OwnerID = o.UserID
                    WHERE a.ProviderID = @UserId
                    ORDER BY r.ReviewDate DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();
                var reviews = new List<object>();

                while (await reader.ReadAsync())
                {
                    reviews.Add(new
                    {
                        ReviewId = reader.GetInt32("ReviewID"),
                        AppointmentId = reader.GetInt32("AppointmentID"),
                        ReviewerId = reader.GetInt32("ReviewerID"),
                        ReviewerUsername = reader.GetString("ReviewerUsername"),
                        ReviewerFullName = reader.GetString("ReviewerFullName"),
                        OwnerId = reader.GetInt32("OwnerID"),
                        OwnerUsername = reader.GetString("OwnerUsername"),
                        OwnerFullName = reader.GetString("OwnerFullName"),
                        Rating = reader.GetInt32("Rating"),
                        ReviewText = reader.GetString("ReviewText"),
                        ReviewDate = reader.GetDateTime("ReviewDate")
                    });
                }

                return Ok(reviews);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בטעינת הביקורות: {ex.Message}");
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllReviews()
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                string query = @"
                    SELECT r.ReviewID, r.AppointmentID, r.ReviewerID, r.Rating, r.ReviewText, r.ReviewDate,
                           u.Username as ReviewerUsername, u.FullName as ReviewerFullName,
                           a.ProviderID, p.Username as ProviderUsername, p.FullName as ProviderFullName,
                           o.UserID as OwnerID, o.Username as OwnerUsername, o.FullName as OwnerFullName
                    FROM reviewsandratings r
                    JOIN appointments a ON r.AppointmentID = a.AppointmentID
                    JOIN petprofiles pet ON a.PetID = pet.PetID
                    JOIN userprofiles u ON r.ReviewerID = u.UserID
                    JOIN userprofiles p ON a.ProviderID = p.UserID
                    JOIN userprofiles o ON pet.OwnerID = o.UserID
                    ORDER BY r.ReviewDate DESC";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                var reviews = new List<object>();

                while (await reader.ReadAsync())
                {
                    reviews.Add(new
                    {
                        ReviewId = reader.GetInt32("ReviewID"),
                        AppointmentId = reader.GetInt32("AppointmentID"),
                        ReviewerId = reader.GetInt32("ReviewerID"),
                        ReviewerUsername = reader.GetString("ReviewerUsername"),
                        ReviewerFullName = reader.GetString("ReviewerFullName"),
                        ProviderId = reader.GetInt32("ProviderID"),
                        ProviderUsername = reader.GetString("ProviderUsername"),
                        ProviderFullName = reader.GetString("ProviderFullName"),
                        OwnerId = reader.GetInt32("OwnerID"),
                        OwnerUsername = reader.GetString("OwnerUsername"),
                        OwnerFullName = reader.GetString("OwnerFullName"),
                        Rating = reader.GetInt32("Rating"),
                        ReviewText = reader.GetString("ReviewText"),
                        ReviewDate = reader.GetDateTime("ReviewDate")
                    });
                }

                return Ok(reviews);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בטעינת הביקורות: {ex.Message}");
            }
        }

        [HttpDelete("{reviewId}")]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                string query = "DELETE FROM reviewsandratings WHERE ReviewID = @ReviewId";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ReviewId", reviewId);

                await command.ExecuteNonQueryAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה במחיקת הביקורת: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                // Get next ReviewID
                string maxIdQuery = "SELECT COALESCE(MAX(ReviewID), 0) FROM reviewsandratings";
                using var maxIdCommand = new MySqlCommand(maxIdQuery, connection);
                int nextReviewId = Convert.ToInt32(await maxIdCommand.ExecuteScalarAsync()) + 1;

                // Insert review
                string insertQuery = @"
                    INSERT INTO reviewsandratings 
                    (ReviewID, AppointmentID, ReviewerID, Rating, ReviewText, ReviewDate)
                    VALUES 
                    (@ReviewID, @AppointmentID, @ReviewerID, @Rating, @ReviewText, @ReviewDate)";

                using var insertCommand = new MySqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@ReviewID", nextReviewId);
                insertCommand.Parameters.AddWithValue("@AppointmentID", request.AppointmentId);
                insertCommand.Parameters.AddWithValue("@ReviewerID", request.ReviewerId);
                insertCommand.Parameters.AddWithValue("@Rating", request.Rating);
                insertCommand.Parameters.AddWithValue("@ReviewText", request.ReviewText);
                insertCommand.Parameters.AddWithValue("@ReviewDate", DateTime.Now);

                await insertCommand.ExecuteNonQueryAsync();

                // Get the reviewed user's ID and current average rating
                string getReviewedUserQuery = @"
                    SELECT 
                        CASE 
                            WHEN a.ProviderID = @ReviewerId THEN pet.OwnerID
                            ELSE a.ProviderID
                        END as ReviewedUserId,
                        u.AverageRating,
                        (SELECT COUNT(*) FROM reviewsandratings r2
                         JOIN appointments a2 ON r2.AppointmentID = a2.AppointmentID
                         WHERE (a2.ProviderID = CASE 
                                                WHEN a.ProviderID = @ReviewerId THEN pet.OwnerID
                                                ELSE a.ProviderID
                                              END)
                         OR EXISTS (SELECT 1 FROM petprofiles p2 
                                  WHERE p2.OwnerID = CASE 
                                                      WHEN a.ProviderID = @ReviewerId THEN pet.OwnerID
                                                      ELSE a.ProviderID
                                                    END
                                  AND p2.PetID = a2.PetID)) as ReviewCount
                    FROM appointments a
                    JOIN petprofiles pet ON a.PetID = pet.PetID
                    JOIN userprofiles u ON u.UserID = CASE 
                                                        WHEN a.ProviderID = @ReviewerId THEN pet.OwnerID
                                                        ELSE a.ProviderID
                                                      END
                    WHERE a.AppointmentID = @AppointmentID";

                using var getUserCommand = new MySqlCommand(getReviewedUserQuery, connection);
                getUserCommand.Parameters.AddWithValue("@ReviewerId", request.ReviewerId);
                getUserCommand.Parameters.AddWithValue("@AppointmentID", request.AppointmentId);

                using var userReader = await getUserCommand.ExecuteReaderAsync();
                if (await userReader.ReadAsync())
                {
                    int reviewedUserId = userReader.GetInt32("ReviewedUserId");
                    float? currentAverage = userReader.IsDBNull("AverageRating") ? null : (float?)userReader.GetFloat("AverageRating");
                    int reviewCount = userReader.GetInt32("ReviewCount");

                    userReader.Close();

                    // Calculate new average rating
                    float newAverage;
                    if (currentAverage.HasValue)
                    {
                        // Calculate new average including the new review
                        newAverage = ((currentAverage.Value * (reviewCount - 1)) + request.Rating) / reviewCount;
                    }
                    else
                    {
                        newAverage = request.Rating;
                    }

                    // Update user's average rating
                    string updateAverageQuery = "UPDATE userprofiles SET AverageRating = @AverageRating WHERE UserID = @UserID";
                    using var updateCommand = new MySqlCommand(updateAverageQuery, connection);
                    updateCommand.Parameters.AddWithValue("@AverageRating", newAverage);
                    updateCommand.Parameters.AddWithValue("@UserID", reviewedUserId);

                    await updateCommand.ExecuteNonQueryAsync();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה ביצירת הביקורת: {ex.Message}");
            }
        }

        [HttpGet("reviewer/{userId}")]
        public async Task<IActionResult> GetReviewerAppointments(int userId)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                string query = "SELECT AppointmentID FROM reviewsandratings WHERE ReviewerID = @UserId";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();
                var appointments = new List<object>();

                while (await reader.ReadAsync())
                {
                    appointments.Add(new
                    {
                        AppointmentId = reader.GetInt32("AppointmentID")
                    });
                }

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בטעינת הפגישות: {ex.Message}");
            }
        }

        [HttpGet("about/{userId}")]
        public async Task<IActionResult> GetReviewsAboutUser(int userId)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                string query = @"
                    SELECT 
                        r.ReviewID,
                        r.ReviewerID,
                        u.Username as ReviewerName,
                        r.Rating,
                        r.ReviewText,
                        r.ReviewDate
                    FROM reviewsandratings r
                    JOIN appointments a ON r.AppointmentID = a.AppointmentID
                    JOIN userprofiles u ON r.ReviewerID = u.UserID
                    WHERE 
                        (
                            -- If the user is a provider, get reviews where they were the provider
                            (a.ProviderID = @UserId AND r.ReviewerID = (SELECT OwnerID FROM petprofiles WHERE PetID = a.PetID))
                            OR
                            -- If the user is an owner, get reviews where they were the owner
                            (EXISTS (
                                SELECT 1 FROM petprofiles p 
                                WHERE p.OwnerID = @UserId 
                                AND p.PetID = a.PetID
                                AND r.ReviewerID = a.ProviderID
                            ))
                        )
                        AND r.ReviewerID != @UserId
                    ORDER BY r.ReviewDate DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();
                var reviews = new List<object>();

                while (await reader.ReadAsync())
                {
                    reviews.Add(new
                    {
                        ReviewId = reader.GetInt32("ReviewID"),
                        ReviewerId = reader.GetInt32("ReviewerID"),
                        ReviewerName = reader.GetString("ReviewerName"),
                        Rating = reader.GetInt32("Rating"),
                        ReviewText = reader.GetString("ReviewText"),
                        ReviewDate = reader.GetDateTime("ReviewDate")
                    });
                }

                return Ok(reviews);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בטעינת הביקורות: {ex.Message}");
            }
        }

        [HttpGet("by/{userId}")]
        public async Task<IActionResult> GetReviewsByUser(int userId)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                string query = @"
                    SELECT 
                        r.ReviewID,
                        CASE 
                            WHEN a.ProviderID = r.ReviewerID THEN 
                                (SELECT OwnerID FROM petprofiles WHERE PetID = a.PetID)
                            ELSE a.ProviderID
                        END as ReviewedUserId,
                        CASE 
                            WHEN a.ProviderID = r.ReviewerID THEN 
                                (SELECT Username FROM userprofiles WHERE UserID = (SELECT OwnerID FROM petprofiles WHERE PetID = a.PetID))
                            ELSE (SELECT Username FROM userprofiles WHERE UserID = a.ProviderID)
                        END as ReviewedUserName,
                        r.Rating,
                        r.ReviewText,
                        r.ReviewDate
                    FROM reviewsandratings r
                    JOIN appointments a ON r.AppointmentID = a.AppointmentID
                    WHERE r.ReviewerID = @UserId
                    ORDER BY r.ReviewDate DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();
                var reviews = new List<object>();

                while (await reader.ReadAsync())
                {
                    reviews.Add(new
                    {
                        ReviewId = reader.GetInt32("ReviewID"),
                        ReviewedUserId = reader.GetInt32("ReviewedUserId"),
                        ReviewedUserName = reader.GetString("ReviewedUserName"),
                        Rating = reader.GetInt32("Rating"),
                        ReviewText = reader.GetString("ReviewText"),
                        ReviewDate = reader.GetDateTime("ReviewDate")
                    });
                }

                return Ok(reviews);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בטעינת הביקורות: {ex.Message}");
            }
        }

        public class CreateReviewRequest
        {
            public int AppointmentId { get; set; }
            public int ReviewerId { get; set; }
            public int Rating { get; set; }
            public string ReviewText { get; set; } = "";
        }
    }
} 