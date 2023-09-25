namespace BeautyHubAPI.Models.Helper
{
    public class FilterationResponseModel<T>
    {
        public int totalCount { get; set; }
        public int pageSize { get; set; }
        public int currentPage { get; set; }
        public int totalPages { get; set; }
        public string previousPage { get; set; }
        public string nextPage { get; set; }
        public string searchQuery { get; set; }
        public List<T> dataList { get; set; }
    }

}
