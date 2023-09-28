
using System.Security.Cryptography;
using System.Text;
using System;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json.Linq;
using BeautyHubAPI.Common;
using System.Globalization;

namespace BeautyHubAPI.Models.Helper
{
    public static class CommonMethod
    {
        private static Random random = new Random();
        public static string GenerateRandomPassword()
        {
            const int requiredLength = 4;

            using var rng = new RNGCryptoServiceProvider();
            var buffer = new byte[256];

            var password = new StringBuilder();

            while (password.Length < requiredLength)
            {
                rng.GetBytes(buffer);

                for (var i = 0; i < buffer.Length && password.Length < requiredLength; i++)
                {
                    var randomChar = (char)buffer[i];

                    // Only include printable ASCII characters
                    if (randomChar >= 33 && randomChar <= 126)
                    {
                        password.Append(randomChar);
                    }
                }
            }

            var uniqueChars = password.ToString().Distinct().Count();

            var generatedPassword = $"Jif@1" + password.ToString() + "L";

            return generatedPassword;
        }

        public static int GenerateOTP()
        {
            Random rnd = new Random();
            return rnd.Next(10000, 99999);
        }
        public static string GenerateOrderId()
        {
            Random random = new Random();
            string orderId = $"{DateTime.Now:yyyyMMddHHmmss}";//-{random.Next(10000, 99999)};
            return orderId;
        }
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789#@";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public static string EnsureCorrectFilename(string filename)
        {
            if (filename.Contains("\\"))
                filename = filename.Substring(filename.LastIndexOf("\\") + 1);
            return filename;
        }
        public static bool HasPort(string url)
        {
            Uri uri;
            if (Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                return uri.Port != -1;
            }
            return false;
        }
        public static string CalculateSHA256Hash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                // Convert the input string to bytes
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);

                // Compute the hash and get the result as bytes
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                // Convert the hash bytes to a hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2")); // "x2" means two hexadecimal digits with zero padding
                }

                return sb.ToString();
            }
        }
        public static string EncodeToBase64(string jsonData)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(jsonData);
            return Convert.ToBase64String(bytes);
        }
        public static string RenameFileName(string filename)
        {
            var outFileName = "";
            if (filename != null || filename != "")
            {
                var extension = System.IO.Path.GetExtension(filename);
                //filename = DateTime.Now.ToString() + DateTime.Now.Millisecond.ToString() + filename;
                //filename = filename.Replace(' ', '0').Replace(':', '1').Replace('/', '0');
                outFileName = Guid.NewGuid().ToString() + extension;
            }
            return outFileName;
        }
        public static string[] SplitStringToParts(string passedString)
        {
            return passedString.Split(" ", StringSplitOptions.None);
        }
        public static string GeneratePassword(PasswordOptions opts = null)
        {
            if (opts == null) opts = new PasswordOptions()
            {
                RequiredLength = 8,
                RequiredUniqueChars = 4,
                RequireDigit = true,
                RequireLowercase = true,
                RequireNonAlphanumeric = true,
                RequireUppercase = true
            };

            string[] randomChars = new[] {
            "ABCDEFGHJKLMNOPQRSTUVWXYZ",    // uppercase 
            "abcdefghijkmnopqrstuvwxyz",    // lowercase
            "0123456789",                   // digits
            "!@$?_-"                        // non-alphanumeric
        };

            Random rand = new Random(Environment.TickCount);
            List<char> chars = new List<char>();

            if (opts.RequireUppercase)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[0][rand.Next(0, randomChars[0].Length)]);

            if (opts.RequireLowercase)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[1][rand.Next(0, randomChars[1].Length)]);

            if (opts.RequireDigit)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[2][rand.Next(0, randomChars[2].Length)]);

            if (opts.RequireNonAlphanumeric)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[3][rand.Next(0, randomChars[3].Length)]);

            for (int i = chars.Count; i < opts.RequiredLength
                || chars.Distinct().Count() < opts.RequiredUniqueChars; i++)
            {
                string rcs = randomChars[rand.Next(0, randomChars.Length)];
                chars.Insert(rand.Next(0, chars.Count),
                    rcs[rand.Next(0, rcs.Length)]);
            }

            return new string(chars.ToArray());
        }
        public static double CalculateDistance(double startLat, double startLong, double endLat, double endLong)
        {
            const double EarthRadiusKm = 6371.0;
            var dLat = ConvertToRadians(endLat - startLat);
            var dLon = ConvertToRadians(endLong - startLong);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ConvertToRadians(startLat)) * Math.Cos(ConvertToRadians(endLat)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            var distance = EarthRadiusKm * c;
            return distance;
        }
        private static double ConvertToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }
        public static bool IsValidTimeFormat(string timeString)
        {
            DateTime dummyOutput;
            return DateTime.TryParseExact(timeString, "hh:mm tt", null, System.Globalization.DateTimeStyles.None, out dummyOutput);
        }
        public static bool IsValidTime24Format(string timeString)
        {
            DateTime dummyOutput;
            return DateTime.TryParseExact(timeString, "HH:mm", null, System.Globalization.DateTimeStyles.None, out dummyOutput);
        }
        public static bool IsValidDateFormat_ddmmyyyy(string timeString)
        {
            DateTime dummyOutput;
            return DateTime.TryParseExact(timeString, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dummyOutput);
        }
        public static DateTime ddMMyyyToDateTime(string timeString)
        {
            DateTime dummyOutput;
            return DateTime.ParseExact(timeString, "dd-MM-yyyy", null);
        }
        public static async Task<DistanceMatrixAPIResponse> GoogleDistanceMatrixAPILatLonAsync(double startLat, double startLong, double endLat, double endLong)
        {
            DistanceMatrixAPIResponse dmapir = new DistanceMatrixAPIResponse();

            using (HttpClient client = new HttpClient())
            {
                string apiUrl = $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={startLat},{startLong}&destinations={endLat},{endLong}&key={GlobalVariables.distancematrixAPIKey}";

                HttpResponseMessage response = await client.GetAsync(apiUrl);
                string responseContent = await response.Content.ReadAsStringAsync();
                // Parse the JSON response
                JObject responseJson = JObject.Parse(responseContent);

                // Extract distance and duration information
                JToken? rows = responseJson["rows"];
                JToken? elements = null;
                JToken? distance = null;
                JToken? duration = null;

                if (rows != null && rows.HasValues)
                {
                    elements = rows.First?["elements"];
                    if (elements != null && elements.HasValues)
                    {
                        distance = elements?.First?["distance"]?["text"];
                        duration = elements?.First?["duration"]?["text"];
                    }
                }

                // Display the extracted distance and duration
                if (distance != null && duration != null)
                {
                    dmapir.distance = distance.ToString();
                    dmapir.duration = duration.ToString();
                }
                return dmapir;
            }
        }

    }
}
