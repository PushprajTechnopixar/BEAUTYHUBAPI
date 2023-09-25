using BeautyHubAPI.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeautyHubAPI.Repository.IRepository
{
    public interface IContentRepository
    {
        Task<List<ContentCountryViewModel>> GetCountries();
        Task<ContentCountryViewModel> GetCountryById(int? countryId);
        Task<ContentStateViewModel> GetStateById(int? stateId);
        Task<List<ContentStateViewModel>> GetStatesByCountryId(int CountryId);
        Task<List<ContentCityViewModel>> GetCitiesByStateId(int StateId);
        Task<List<ContentStateViewModel>> GetStates();

    }
}
