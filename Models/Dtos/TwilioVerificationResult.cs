using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeautyHubAPI.Dtos
{
    public class TwilioVerificationResult
    {
        public TwilioVerificationResult(string sid)
        {
            Sid = sid;
            IsValid = true;
        }

        public TwilioVerificationResult(List<string> errors)
        {
            Errors = errors;
            IsValid = false;
        }

        public bool IsValid { get; set; }

        public string Sid { get; set; }

        public List<string> Errors { get; set; }
    }
}
