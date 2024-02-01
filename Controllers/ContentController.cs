
using BeautyHubAPI.Data;
using BeautyHubAPI.Models.Helper;
using BeautyHubAPI.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using BeautyHubAPI.Helpers;
using AutoMapper;
using BeautyHubAPI.Models;
using BeautyHubAPI.Models.Dtos;
using BeautyHubAPI.Repository;
using BeautyHubAPI.Common;

namespace BeautyHubAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContentController : ControllerBase
    {
        private IContentRepository _ContentRepository;

        private ApplicationDbContext _context;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        private readonly IBannerRepository _bannerRepository;


        public ContentController(
            ApplicationDbContext context,
            IMapper mapper,
            IContentRepository contentRepository,
            IBannerRepository bannerRepository


        )
        {
            _ContentRepository = contentRepository;
            _context = context;
            _response = new();
            _mapper = mapper;
            _bannerRepository = bannerRepository;

        }

        #region GetCountries
        /// <summary>
        ///  Get country list.
        /// </summary>
        /// <param  name="searchQuery"> Search by country name</param>
        /// <returns></returns>
        [HttpGet]
        [Route("GetCountries")]
        public async Task<ActionResult> GetCountries(string? searchQuery)
        {
            try
            {
                var countryList = await _ContentRepository.GetCountries();
                if (!String.IsNullOrEmpty(searchQuery))
                {
                    countryList = countryList.Where(s => s.countryName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Messages = ResponseMessages.msgShownSuccess;
                _response.Data = countryList;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgSomethingWentWrong;
                return Ok(_response);
            }
        }
        #endregion

        #region GetStates
        /// <summary>
        ///  Get state list.
        /// </summary>
        /// <param  name="countryId"> The id of country</param>
        /// <param  name="searchQuery"> Search by state name</param>
        /// <returns></returns>
        [HttpGet]
        [Route("GetStates")]
        public async Task<ActionResult> GetStatesByCountryId(int countryId, string? searchQuery)
        {
            try
            {
                var stateList = await _ContentRepository.GetStatesByCountryId(countryId);
                if (!String.IsNullOrEmpty(searchQuery))
                {
                    stateList = stateList.Where(s => s.stateName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Messages = ResponseMessages.msgShownSuccess;
                _response.Data = stateList;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgSomethingWentWrong;
                return Ok(_response);
            }
        }
        #endregion

        // #region GetCities
        // /// <summary>
        // ///  Get state list.
        // /// </summary>
        // /// <param  name="stateId"> The id of state</param>
        // /// <param  name="searchQuery"> Search by city name</param>
        // /// <returns></returns>
        // [HttpGet]
        // [Route("GetCities")]
        // public async Task<ActionResult> GetCitiesByStateId(int stateId, string? searchQuery)
        // {
        //     try
        //     {
        //         var cityList = await _ContentRepository.GetCitiesByStateId(stateId);
        //         if (!String.IsNullOrEmpty(searchQuery))
        //         {
        //             cityList = cityList.Where(s => s.cityName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
        //             ).ToList();
        //         }
        //         _response.StatusCode = HttpStatusCode.OK;
        //         _response.IsSuccess = true;
        //         _response.Messages = "Shown successfully.";
        //         _response.Data = cityList;
        //         return Ok(_response);
        //     }
        //     catch (Exception ex)
        //     {
        //         _response.StatusCode = HttpStatusCode.InternalServerError;
        //         _response.IsSuccess = false;
        //         _response.Messages = "Something went wrong.";
        //         return Ok(_response);
        //     }
        // }
        // #endregion

        // #region GetAllStates
        // /// <summary>
        // ///  Get state list.
        // /// </summary>
        // /// <param  name="searchQuery"> Search by state name</param>
        // /// <returns></returns>
        // [HttpGet]
        // [Route("GetAllStates")]
        // public async Task<ActionResult> GetAllStates(string? searchQuery)
        // {
        //     try
        //     {
        //         var stateList = await _ContentRepository.GetStates();
        //         if (!String.IsNullOrEmpty(searchQuery))
        //         {
        //             stateList = stateList.Where(s => s.stateName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
        //             ).ToList();
        //         }
        //         _response.StatusCode = HttpStatusCode.OK;
        //         _response.IsSuccess = true;
        //         _response.Messages = "Shown successfully.";
        //         _response.Data = stateList;
        //         return Ok(_response);
        //     }
        //     catch (Exception ex)
        //     {
        //         _response.StatusCode = HttpStatusCode.InternalServerError;
        //         _response.IsSuccess = false;
        //         _response.Messages = "Something went wrong.";
        //         return Ok(_response);
        //     }
        // }
        // #endregion

        #region GetBannerList
        /// <summary>
        ///  Get banner list.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("GetBannerList")]
        public async Task<ActionResult> GetBannerList()
        {
            try
            {
                var bannerList = (await _bannerRepository.GetAllAsync()).OrderBy(u => u.CreateDate).ToList();
                if (bannerList.Count > 0)
                {
                    var bannerListResponse = _mapper.Map<List<BannerDTO>>(bannerList);

                    foreach (var item in bannerListResponse)
                    {
                        item.createDate = Convert.ToDateTime(item.createDate).ToString(@"dd-MM-yyyy");

                    }

                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Messages = "List" + ResponseMessages.msgShownSuccess;
                    _response.Data = bannerListResponse;
                    return Ok(_response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgNotFound + "record.";
                _response.Data = new Object { };
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgSomethingWentWrong;
                return Ok(_response);
            }
        }
        #endregion

        #region GetBannerDetail
        /// <summary>
        ///  Get banner detail.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("GetBannerDetail")]
        public async Task<ActionResult> GetBannerDetail(int bannerId)
        {
            try
            {
                var banner = await _bannerRepository.GetAsync(u => u.BannerId == bannerId);
                if (banner != null)
                {
                    var bannerResponse = _mapper.Map<BannerDTO>(banner);
                    bannerResponse.createDate = Convert.ToDateTime(banner.CreateDate).ToString(@"dd-MM-yyyy");

                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Messages = "Detail" + ResponseMessages.msgShownSuccess;
                    _response.Data = bannerResponse;
                    return Ok(_response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgNotFound + "record.";
                _response.Data = new Object { };
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgSomethingWentWrong;
                return Ok(_response);
            }
        }
        #endregion

    }
}
