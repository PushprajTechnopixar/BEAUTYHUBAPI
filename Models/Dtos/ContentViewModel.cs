using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeautyHubAPI.ViewModels
{
    public class ContentCountryViewModel
    {
        public int countryId { get; set; }
        public string countryName { get; set; }
    }
    public class ContentStateViewModel
    {
        public int stateId { get; set; }
        public string stateName { get; set; }
    }
    public class ContentCityViewModel
    {
        public int cityId { get; set; }
        public string cityName { get; set; }
    }
}
