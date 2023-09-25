using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using BeautyHubAPI.ViewModels;
using Microsoft.EntityFrameworkCore;
using BeautyHubAPI.Data;
using BeautyHubAPI.Repository.IRepository;

namespace BeautyHubAPI.Repositories
{
    public class ContentRepository : IContentRepository
    {
        private ApplicationDbContext _context;

        public ContentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ContentCountryViewModel>> GetCountries()
        {
            var countries = await _context.CountryMaster
                .Select(item => new ContentCountryViewModel { countryId = item.CountryId, countryName = item.CountryName })
                .ToListAsync();
            return countries;
        }

        public async Task<ContentCountryViewModel> GetCountryById(int? countryId)
        {
            var country = await _context.CountryMaster.Where(u => u.CountryId == countryId)
                .Select(item => new ContentCountryViewModel { countryId = item.CountryId, countryName = item.CountryName })
                .FirstOrDefaultAsync();
            return country;
        }

        public async Task<ContentStateViewModel> GetStateById(int? stateId)
        {
            var country = await _context.StateMaster.Where(u => u.StateId == stateId)
                .Select(item => new ContentStateViewModel { stateId = item.StateId, stateName = item.StateName })
                .FirstOrDefaultAsync();
            return country;
        }

        public async Task<List<ContentStateViewModel>> GetStatesByCountryId(int CountryId)
        {
            return await _context.StateMaster
                .Where(x => x.CountryId == CountryId)
                .Select(x => new ContentStateViewModel { stateId = x.StateId, stateName = x.StateName })
                .OrderBy(a => a.stateId)
                .ToListAsync();
        }
        public async Task<List<ContentCityViewModel>> GetCitiesByStateId(int StateId)
        {
            return await _context.CityMaster
                .Where(x => x.StateId == StateId)
                .Select(x => new ContentCityViewModel { cityId = x.Id, cityName = x.Name })
                .ToListAsync();
        }

        public async Task<List<ContentStateViewModel>> GetStates()
        {
            return await _context.StateMaster
                .Select(x => new ContentStateViewModel { stateId = x.StateId, stateName = x.StateName })
                .ToListAsync();
        }
    }
}
