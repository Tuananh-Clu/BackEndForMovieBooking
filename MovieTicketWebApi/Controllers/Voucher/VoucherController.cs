using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MovieTicketWebApi.Model;
using MovieTicketWebApi.Service.Voucher;

namespace MovieTicketWebApi.Controllers.Voucher
{
    [Route("api/[controller]")]
    [ApiController]

    public class VoucherController : ControllerBase
    {
        public readonly VoucherService _voucherCollection;
        public VoucherController(VoucherService service)
        {
            _voucherCollection = service;
        }
        [HttpGet("GetVoucher")]
        public async Task<IActionResult> GetAllVouchersAsync()
        {
            var vouchers = await _voucherCollection.GetAllVouchersAsync();
            return Ok(vouchers);
        }
        [HttpGet("GetVoucherActive")]
        public async Task<IActionResult> GetAllVouchersActive()
        {
            var vouchers = await _voucherCollection.GetAllVouchersActive();
            return Ok(vouchers);
        }
        [HttpPost("AddVoucher")]
        public async Task<IActionResult> AddVoucher([FromBody] VoucherDb voucher)
        {
            if (voucher == null)
            {
                return BadRequest("Voucher cannot be null.");
            }
            await _voucherCollection.AddVoucher(voucher);
            return Ok("Voucher added successfully.");
        }
        [HttpPost("Change")]
        public async Task<IActionResult> ChangeProp([FromQuery]string VoucherCode)
        {
            await _voucherCollection.ChangeProp(VoucherCode);
            return Ok("Ok");
        }
        [HttpGet("LayGiaSauGiam")]
        public async Task<IActionResult> LayGiaSauGiam([FromQuery] string role,[FromQuery]string VoucherCode, [FromQuery]float GiaTien, [FromQuery]string theaterName)
        {
            var data = await _voucherCollection.GetGiaSauKhiGiam(role,VoucherCode, GiaTien,theaterName);
            return Ok(data);
        }


    }
}
