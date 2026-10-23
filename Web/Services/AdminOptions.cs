namespace Web.Services
{
    // Bound from the "Admin" section of appsettings.json
    public class AdminOptions
    {
        public string Name { get; set; } = "Administrator";
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
