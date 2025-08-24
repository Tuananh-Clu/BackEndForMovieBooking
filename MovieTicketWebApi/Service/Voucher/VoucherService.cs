using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MovieTicketWebApi.Data;
using MovieTicketWebApi.Model;
using MovieTicketWebApi.Model.Cinema;

namespace MovieTicketWebApi.Service.Voucher
{

    public class VoucherService
    {
        public readonly IMongoCollection<VoucherDb> _voucherCollection;
        public VoucherService(MongoDbContext dbContext)
        {
            _voucherCollection = dbContext.Voucher;
        }
        public async Task<List<VoucherDb>> GetAllVouchersAsync()
        {
            var filter = Builders<VoucherDb>.Filter.Eq(a => a.IsActive, "true");
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
        public async Task<string> GetGiaSauKhiGiam(string code,float price, string theaterName)
        {
            var filter=Builders<VoucherDb>.Filter.Eq(a=>a.Code, code);
            var data=await _voucherCollection.Find(filter).FirstOrDefaultAsync();
            float Price;
            string note = "Không thể áp dụng voucher cho rạp bạn đang chọn";
            if (!data.PhamViApDung.Trim().ToLower().Contains(theaterName.Trim().ToLower())) {
                return (note);
            }
            else {
                if (price < data.MinimumOrderAmount)
                {
                    Price = price;
                }
                if (data.LoaiGiam == "Value")
                {
                    Price = price - data.DiscountAmount;
                }
                else
                {
                    Price = price - (price * data.DiscountAmount / 100);
                }
                if (Price < 0) Price = 0;
                return (Price.ToString());
            }
        }
    }
}
