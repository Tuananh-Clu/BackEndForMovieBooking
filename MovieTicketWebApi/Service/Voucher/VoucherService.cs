using MongoDB.Driver;
using MovieTicketWebApi.Data;
using MovieTicketWebApi.Model;

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
            return await _voucherCollection.Find(_ => true).ToListAsync();
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
    }
}
