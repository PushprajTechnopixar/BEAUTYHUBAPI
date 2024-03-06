using BeautyHubAPI.Models;
using BeautyHubAPI.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace BeautyHubAPI.Repository.IRepository
{
    public interface ICategoryRepository
    {
        Task<Object> AddCategory(AddCategoryDTO model, string currentUserId);
        Task<Object> UpdateCategory(UpdateCategoryDTO model, string currentUserId);
        Task<Object> GetSubCategoryType(int mainCategoryId);
        Task<Object> GetCategoryList(GetCategoryRequestDTO model, string currentUserId);
        Task<Object> GetCategoryDetail(GetCategoryDetailRequestDTO model);
        Task<Object> DeleteCategory(DeleteCategoryDTO model);
        Task<Object> GetCategoryRequests(string currentUserId);
        Task<Object> SetCategoryStatus(CategoryStatusRequestDTO model);
    }
}
