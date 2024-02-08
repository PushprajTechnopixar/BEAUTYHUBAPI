using BeautyHubAPI.Models;
using BeautyHubAPI.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace BeautyHubAPI.Repository.IRepository
{
    public interface ICategoryRepository
    {
        Task<Object> AddCategory([FromBody] AddCategoryDTO model, string currentUserId);
        Task<Object> UpdateCategory([FromBody] UpdateCategoryDTO model, string currentUserId);
        Task<Object> GetSubCategoryType(int mainCategoryId);
        Task<Object> GetCategoryList([FromQuery] GetCategoryRequestDTO model, string currentUserId);
        Task<Object> GetCategoryDetail([FromQuery] GetCategoryDetailRequestDTO model);
        Task<Object> DeleteCategory([FromQuery] DeleteCategoryDTO model);
        Task<Object> GetCategoryRequests(string currentUserId);
        Task<Object> SetCategoryStatus([FromBody] CategoryStatusRequestDTO model);
    }
}
