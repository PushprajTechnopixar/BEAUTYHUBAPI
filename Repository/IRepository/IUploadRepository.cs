using BeautyHubAPI.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeautyHubAPI.Repository
{
    public interface IUploadRepository
    {
        Task<bool> UploadFilesToServer(IFormFile file, string prefix, string fileName);
        Task<bool> DeleteFilesFromServer(string fileName);
    }
}
