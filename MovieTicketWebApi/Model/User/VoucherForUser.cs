namespace MovieTicketWebApi.Model.User
{
    public class VoucherForUser
    {
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string LoaiGiam { get; set; }
        public int DiscountAmount { get; set; }
        public string ExpirationDate { get; set; }
        public int MinimumOrderAmount { get; set; }
        public string PhamViApDung { get; set; }
        public string SoLuotUserDuocDung { get; set; }
        public string used { get; set; } 
    }
}
