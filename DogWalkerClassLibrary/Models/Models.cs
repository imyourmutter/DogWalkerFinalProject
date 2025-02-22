using System.ComponentModel.DataAnnotations;

namespace DogWalkerClassLibrary.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePic { get; set; }
        public int? UserRole { get; set; } // 0 = DogOwner, 1 = DogWalker, 2 = DogGroomer, 3 = DogVet, 4 = Admin
        public float? AverageRating { get; set; } //Updated with each new review. Exists to theres no need to recompute it for each page.
        public string? Address { get; set; } // User written address
    }

    public class RegistrationModel
    {
        [Required(ErrorMessage = "שם משתמש הוא שדה חובה")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "סיסמה היא שדה חובה")]
        [MinLength(8, ErrorMessage = "הסיסמה חייבת להכיל לפחות 8 תווים")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "שם מלא הוא שדה חובה")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "דוא\"ל הוא שדה חובה")]
        [EmailAddress(ErrorMessage = "כתובת דוא\"ל לא תקינה")]
        public string Email { get; set; } = "";

        [Phone(ErrorMessage = "מספר טלפון לא תקין")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "כתובת היא שדה חובה")]
        public string Address { get; set; } = "";

        public int UserRole { get; set; }

        public List<PetModel> Pets { get; set; } = new();
    }

    public class PetModel
    {
        public int PetId { get; set; }

        [Required(ErrorMessage = "שם הכלב הוא שדה חובה")]
        public string PetName { get; set; } = "";

        [Required(ErrorMessage = "גזע הכלב הוא שדה חובה")]
        public string Breed { get; set; } = "";

        [Range(0.1, 100, ErrorMessage = "משקל הכלב חייב להיות בין 0.1 ל-100 ק\"ג")]
        public float Weight { get; set; }

        public string? Allergies { get; set; }
        public string? SpecialNeeds { get; set; }
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "שם משתמש הוא שדה חובה")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "סיסמה היא שדה חובה")]
        public string Password { get; set; } = "";
    }

    public class ServiceProviderAvailability
    {
        public int AvailabilityID { get; set; }
        public int ProviderID { get; set; }
        public string ProviderType { get; set; } = "";
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        // Additional properties for joined data from userprofiles
        public string? ProviderName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public float? AverageRating { get; set; }
        public int UserRole { get; set; }
    }

    public class NewAvailabilityRequest
    {
        public int ProviderID { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    public class RegisterResponse
    {
        public string UserId { get; set; } = "";
    }
} 