namespace MovieTicketWebApi.Model
{
    public class VoucherDb
    {
        public string ? Code { get; set; }
        public string? Description { get; set; }
        public string LoaiGiam { get; set; }
        public int DiscountAmount { get; set; } 
        public string ExpirationDate { get; set; }
        public string IsActive { get; set; }
        public int MinimumOrderAmount { get; set; }
        public int UsageCount { get; set; }
        public string NgayBatDau { get; set; }
        public string PhamViApDung { get; set; }
        public string SoLuotUserDuocDung { get; set; }
    }
}
