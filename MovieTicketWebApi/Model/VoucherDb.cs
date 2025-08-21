namespace MovieTicketWebApi.Model
{
    public class VoucherDb
    {
        public string ? Code { get; set; }
        public string? VoucherCode { get; set; }
        public string? Description { get; set; }
        public int DiscountAmount { get; set; } // Giảm giá theo số tiền
        public DateTime ExpirationDate { get; set; }
        public bool IsActive { get; set; }
        public int MinimumOrderAmount { get; set; }
        public int usageCount { get; set; } // Số lần đã sử dụng
    }
}
