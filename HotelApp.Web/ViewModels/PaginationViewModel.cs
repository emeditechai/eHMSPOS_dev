using System;

namespace HotelApp.Web.ViewModels
{
    public class PaginationViewModel
    {
        public PaginationViewModel(int totalItems, int pageNumber, int pageSize)
        {
            if (pageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");
            }

            TotalItems = Math.Max(0, totalItems);
            PageSize = pageSize;
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
            PageNumber = Math.Min(Math.Max(pageNumber, 1), TotalPages);
        }

        public int PageNumber { get; }
        public int PageSize { get; }
        public int TotalItems { get; }
        public int TotalPages { get; }

        public bool HasPrevious => PageNumber > 1;
        public bool HasNext => PageNumber < TotalPages;
        public int PreviousPage => HasPrevious ? PageNumber - 1 : 1;
        public int NextPage => HasNext ? PageNumber + 1 : TotalPages;
        public int FirstItemIndex => TotalItems == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;
        public int LastItemIndex => TotalItems == 0 ? 0 : Math.Min(PageNumber * PageSize, TotalItems);
    }
}
