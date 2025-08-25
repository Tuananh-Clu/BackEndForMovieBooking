using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MovieTicketWebApi.Data;
using MovieTicketWebApi.Model;
using MovieTicketWebApi.Model.Cinema;
using System.Text;

namespace MovieTicketWebApi.Service.Voucher
{

    public class VoucherService
    {
        public readonly IMongoCollection<VoucherDb> _voucherCollection;
        private string NormalizeSimple(string input)
        {
            return input?.Normalize(NormalizationForm.FormC).Trim().ToLower();
        }

        public VoucherService(MongoDbContext dbContext)
        {
            _voucherCollection = dbContext.Voucher;
        }
        public async Task<List<VoucherDb>> GetAllVouchersAsync()
        {
            var filter = Builders<VoucherDb>.Filter.In(a => a.IsActive, new[] {"true","false"});
            var data = await _voucherCollection.Find(filter).ToListAsync();
            return data.Where(v =>
            {
                if (DateTime.TryParse(v.ExpirationDate, out var expirationDate))
                {
                    return expirationDate > DateTime.Now;
                }
                return false;
            }).ToList();
        }
        public async Task AddVoucher(VoucherDb voucher)
        {
            await _voucherCollection.InsertOneAsync(voucher);
        }
        public async Task ChangeProp(string voucherCode)
        {
            var filter = Builders<VoucherDb>.Filter.Eq(a => a.Code, voucherCode);
            var voucher = await _voucherCollection.Find(filter).FirstOrDefaultAsync();

            if (voucher == null)
            {
                Console.WriteLine("Không có dữ liệu");
                return;
            }

            var find = voucher.IsActive == "true";
            if (find)
            {
                var update = Builders<VoucherDb>.Update.Set(a => a.IsActive, "false");
                await _voucherCollection.UpdateOneAsync(filter, update);
            }
            else
            {
                var update = Builders<VoucherDb>.Update.Set(a => a.IsActive, "true");
                await _voucherCollection.UpdateOneAsync(filter, update);
            }
        }
        public async Task<string> GetGiaSauKhiGiam(string code, float price, string theaterName)
        {
            var filter = Builders<VoucherDb>.Filter.Eq(a => a.Code, code);
            var data = await _voucherCollection.Find(filter).FirstOrDefaultAsync();

            if (data == null)
                return "Voucher không tồn tại";
            var appliedTheater = NormalizeSimple(data.PhamViApDung);
            var currentTheater = NormalizeSimple(theaterName);

            if (appliedTheater != currentTheater)
                return "Không thể áp dụng voucher cho rạp bạn đang chọn";

            if (price < data.MinimumOrderAmount)
            {
                return $"Đơn hàng tối thiểu phải lớn hơn {data.MinimumOrderAmount}";
            }

            float finalPrice;

            if (data.LoaiGiam == "Value")
            {
                finalPrice = price - data.DiscountAmount;
            }
            else 
            {
                finalPrice = price - (price * data.DiscountAmount / 100f);
            }

            if (finalPrice < 0) finalPrice = 0;

            return finalPrice.ToString("0.##");
        }

    }
}
