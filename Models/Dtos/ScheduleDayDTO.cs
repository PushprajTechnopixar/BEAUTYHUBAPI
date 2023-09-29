using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models.Dtos
{
    public class ScheduleDayDTO
    {
        public int salonId { get; set; }
        public bool? monday { get; set; }
        public bool? tuesday { get; set; }
        public bool? wednesday { get; set; }
        public bool? thursday { get; set; }
        public bool? friday { get; set; }
        public bool? saturday { get; set; }
        public bool? sunday { get; set; }
        public string fromTime { get; set; }
        public string toTime { get; set; }
        // public bool UpdateStatus { get; set; }
    }
    public class ScheduleDayResonceDTO
    {
        public int salonId { get; set; }
        public bool? monday { get; set; }
        public bool? tuesday { get; set; }
        public bool? wednesday { get; set; }
        public bool? thursday { get; set; }
        public bool? friday { get; set; }
        public bool? saturday { get; set; }
        public bool? sunday { get; set; }
        public string fromTime { get; set; }
        public string toTime { get; set; }
        public bool updateStatus { get; set; }
    }
    public class TimeList
    {
        public string time { get; set; }
    }
}
