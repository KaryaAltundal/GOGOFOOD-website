using Microsoft.AspNetCore.Identity;

namespace Lab3.Models.Identity
{
    public class ApplicationUser : IdentityUser
    {
        // Basic info
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string? Address { get; set; }

        // Profile Photo
        public string? ProfilePhotoFileName { get; set; }
        public string? ProfilePhotoContentType { get; set; }
        public long? ProfilePhotoSize { get; set; }
        public byte[]? ProfilePhotoData { get; set; }
    }
}