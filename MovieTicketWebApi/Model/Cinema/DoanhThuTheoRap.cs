namespace MovieTicketWebApi.Model.Cinema
{
    public class DoanhThuTheoRap
    {
        public string name { get; set; }
        public int quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public List<decimal> Avune { get; set; }
    }
}
