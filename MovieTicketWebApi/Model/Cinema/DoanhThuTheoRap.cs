namespace MovieTicketWebApi.Model.Cinema
{
    public class DoanhThuTheoRap
    {
        public string name { get; set; }
        public int quantity { get; set; }
        public int TotalPrice { get; set; }
        public List<int> Avune { get; set; }
    }
}
