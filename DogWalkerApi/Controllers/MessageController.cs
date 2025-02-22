using Microsoft.AspNetCore.Mvc;
using DogWalkerApi.Services;
using MySql.Data.MySqlClient;
using System.Data;

namespace DogWalkerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly DatabaseService _databaseService;

        public MessageController(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        [HttpGet("conversations/{userId}")]
        public async Task<IActionResult> GetConversations(int userId)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                string query = @"
                    SELECT DISTINCT 
                        CASE 
                            WHEN SenderID = @UserId THEN ReceiverID
                            ELSE SenderID 
                        END as OtherUserId,
                        (SELECT Username FROM userprofiles WHERE UserID = OtherUserId) as Username,
                        (SELECT FullName FROM userprofiles WHERE UserID = OtherUserId) as FullName,
                        (SELECT MAX(SentDate) FROM messages 
                         WHERE (SenderID = @UserId AND ReceiverID = OtherUserId) 
                            OR (SenderID = OtherUserId AND ReceiverID = @UserId)) as LastMessageDate
                    FROM messages
                    WHERE SenderID = @UserId OR ReceiverID = @UserId
                    ORDER BY LastMessageDate DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();
                var conversations = new List<ConversationResponse>();

                while (await reader.ReadAsync())
                {
                    conversations.Add(new ConversationResponse
                    {
                        UserId = reader.GetInt32("OtherUserId"),
                        Username = reader.GetString("Username"),
                        FullName = reader.GetString("FullName"),
                        LastMessageDate = reader.GetDateTime("LastMessageDate")
                    });
                }

                return Ok(conversations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בטעינת השיחות: {ex.Message}");
            }
        }

        [HttpGet("chat/{userId}/{otherUserId}")]
        public async Task<IActionResult> GetChatMessages(int userId, int otherUserId)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                // First, mark messages as read
                var updateQuery = @"
                    UPDATE messages 
                    SET IsRead = 1 
                    WHERE ReceiverID = @UserId 
                    AND SenderID = @OtherUserId 
                    AND IsRead = 0";

                using var updateCommand = new MySqlCommand(updateQuery, connection);
                updateCommand.Parameters.AddWithValue("@UserId", userId);
                updateCommand.Parameters.AddWithValue("@OtherUserId", otherUserId);
                await updateCommand.ExecuteNonQueryAsync();

                // Then get all messages
                var query = @"
                    SELECT MessageID, SenderID, ReceiverID, MessageText, SentDate, IsRead
                    FROM messages
                    WHERE (SenderID = @UserId AND ReceiverID = @OtherUserId)
                    OR (SenderID = @OtherUserId AND ReceiverID = @UserId)
                    ORDER BY SentDate ASC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@OtherUserId", otherUserId);

                using var reader = await command.ExecuteReaderAsync();
                var messages = new List<MessageResponse>();

                while (await reader.ReadAsync())
                {
                    messages.Add(new MessageResponse
                    {
                        MessageId = reader.GetInt32("MessageID"),
                        SenderId = reader.GetInt32("SenderID"),
                        ReceiverId = reader.GetInt32("ReceiverID"),
                        MessageText = reader.GetString("MessageText"),
                        SentDate = reader.GetDateTime("SentDate"),
                        IsRead = reader.GetBoolean("IsRead")
                    });
                }

                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בטעינת ההודעות: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                // Get next MessageID
                string maxIdQuery = "SELECT COALESCE(MAX(MessageID), 0) FROM messages";
                using var maxIdCommand = new MySqlCommand(maxIdQuery, connection);
                int nextMessageId = Convert.ToInt32(await maxIdCommand.ExecuteScalarAsync()) + 1;

                string query = @"
                    INSERT INTO messages 
                    (MessageID, SenderID, ReceiverID, MessageText, SentDate, IsRead)
                    VALUES 
                    (@MessageID, @SenderID, @ReceiverID, @MessageText, @SentDate, 0)";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@MessageID", nextMessageId);
                command.Parameters.AddWithValue("@SenderID", request.SenderId);
                command.Parameters.AddWithValue("@ReceiverID", request.ReceiverId);
                command.Parameters.AddWithValue("@MessageText", request.MessageText);
                command.Parameters.AddWithValue("@SentDate", DateTime.Now);

                await command.ExecuteNonQueryAsync();
                return Ok(new { MessageId = nextMessageId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בשליחת ההודעה: {ex.Message}");
            }
        }

        [HttpPut("read/{messageId}")]
        public async Task<IActionResult> MarkAsRead(int messageId)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                string query = "UPDATE messages SET IsRead = 1 WHERE MessageID = @MessageID";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@MessageID", messageId);

                await command.ExecuteNonQueryAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בעדכון סטטוס ההודעה: {ex.Message}");
            }
        }

        [HttpGet("unread/{userId}")]
        public async Task<IActionResult> GetUnreadCount(int userId)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT COUNT(*) 
                    FROM messages 
                    WHERE ReceiverID = @UserId 
                    AND IsRead = 0";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                var unreadCount = Convert.ToInt32(await command.ExecuteScalarAsync());
                return Ok(unreadCount);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בסעינת מספר ההודעות שלא נקראו: {ex.Message}");
            }
        }
    }

    public class ConversationResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public DateTime LastMessageDate { get; set; }
    }

    public class MessageResponse
    {
        public int MessageId { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string MessageText { get; set; } = "";
        public DateTime SentDate { get; set; }
        public bool IsRead { get; set; }
    }

    public class SendMessageRequest
    {
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string MessageText { get; set; } = "";
    }
} 