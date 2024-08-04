using Microsoft.AspNetCore.Identity;

namespace BlogApplication.Data
{
    public class User : IdentityUser<Guid>
    {
        public string GivenName { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;

    }
}
