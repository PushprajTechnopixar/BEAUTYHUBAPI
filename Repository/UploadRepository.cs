using Amazon.S3;
using Amazon.S3.Model;
using BeautyHubAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeautyHubAPI.Repository
{
    public class UploadRepository : IUploadRepository
    {
        private readonly IAmazonS3 _s3Client;
        private Aws3Services _aws3Services { get; }

        public UploadRepository(IAmazonS3 s3Client, IOptions<Aws3Services> aws3Services)
        {
            _s3Client = s3Client;
            _aws3Services = aws3Services.Value;
        }

        public async Task<bool> UploadFilesToServer(
            IFormFile file,
            string prefix,
            string fileName
        )
        {
            prefix = "FileToSave/" + prefix;
            var bucketExists = await _s3Client.DoesS3BucketExistAsync(_aws3Services.BucketName);
            if (!bucketExists)
                return false;
            var request = new PutObjectRequest()
            {
                BucketName = _aws3Services.BucketName,
                Key = string.IsNullOrEmpty(prefix)
                    ? file.FileName
                    : $"{prefix?.TrimEnd('/')}/{fileName}",
                InputStream = file.OpenReadStream()
            };
            if (file.Name == "pdfapplication")
            {
                request.Metadata.Add("Content-Type", "application/pdf");
            }
            else if (file.Name == "binaryImages")
            {
                request.Metadata.Add("Content-Type", "image/png");
            }
            else
            {
                request.Metadata.Add("Content-Type", file.ContentType);
            }
            await _s3Client.PutObjectAsync(request);
            PutACLResponse response = await _s3Client.PutACLAsync(new PutACLRequest
            {
                BucketName = _aws3Services.BucketName,
                Key = request.Key,
                CannedACL = S3CannedACL.PublicRead
            });



            return true;
        }

        public async Task<bool> DeleteFilesFromServer(string fileName)
        {
            var prefix = "FileToSave/" + fileName;
            var bucketExists = await _s3Client.DoesS3BucketExistAsync(_aws3Services.BucketName);
            if (!bucketExists)
            {
                return false;
            }
            else
            {
                await _s3Client.DeleteObjectAsync(_aws3Services.BucketName, fileName);
            }
            return true;

        }



    }
}
