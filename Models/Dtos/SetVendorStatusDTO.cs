using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BeautyHubAPI.Models
{
    public class SetVendorStatusDTO
    {
        public string vendorId { get; set; }
        public int salonId { get; set; }
        public int status { get; set; }
    }
}
