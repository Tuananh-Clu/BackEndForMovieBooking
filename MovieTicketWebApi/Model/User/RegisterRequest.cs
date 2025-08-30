namespace MovieTicketWebApi.Model.User
{
    public class RegisterRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Name { get; set; } = "";
        public string Avatar { get; set; }
    }
}
