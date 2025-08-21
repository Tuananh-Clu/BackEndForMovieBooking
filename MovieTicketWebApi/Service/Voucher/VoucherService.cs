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
            _voucherCollection=dbContext.Voucher;
        }
        public async Task<List<VoucherDb>> GetAllVouchersAsync()
        {
            return await _voucherCollection.Find(_ => true).ToListAsync();
        }
        public async Task AddVoucher(VoucherDb voucher)
        { 
            await _voucherCollection.InsertOneAsync(voucher);
        }
    }
}
