namespace Implement.ViewModels.Response
{
    public class LoginResponse
    {
        public string UserName { get; set; }
        public string Token { get; set; }
        public string Role { get; set; } = "admin";
    }
}
