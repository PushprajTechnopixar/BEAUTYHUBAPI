using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models.Dtos
{
    public  class PaymentReceiptDTO
    {
        public int paymentReceiptId { get; set; }
        public string? userId { get; set; }
        public string? uaymentReceiptImage { get; set; }
        public DateTime createDate { get; set; }

    }
}
