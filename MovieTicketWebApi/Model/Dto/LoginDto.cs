namespace MovieTicketWebApi.Model.Dto
{
    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; }
        public string Password { get; set; } = string.Empty;

    }
}
