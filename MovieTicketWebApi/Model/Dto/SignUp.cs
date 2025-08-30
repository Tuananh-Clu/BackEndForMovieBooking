namespace MovieTicketWebApi.Model.Dto
{
    public class SignUp
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }= string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
    }
}
