namespace _290526_SushiRestaurantManagement_BE.Helpers
{
    public interface IPaginatedList
    {
        int PageNumber { get; set; }
        int PageSize { get; set; }
        int TotalItems { get; set; }
        int TotalPages { get; }
        bool HasPreviousPage { get; }
        bool HasNextPage { get; }
    }

    public class PaginatedList<T> : IPaginatedList
    {
        public List<T> Items { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages => (TotalItems + PageSize - 1) / PageSize;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public PaginatedList(List<T> items, int total, int pageNumber, int pageSize)
        {
            Items = items;
            TotalItems = total;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }

        public static PaginatedList<T> Create(IEnumerable<T> source, int pageNumber, int pageSize)
        {
            var count = source.Count();
            var totalPages = (count + pageSize - 1) / pageSize;

            pageNumber = Math.Max(1, pageNumber);
            if (totalPages > 0)
            {
                pageNumber = Math.Min(pageNumber, totalPages);
            }

            var items = source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            return new PaginatedList<T>(items, count, pageNumber, pageSize);
        }
    }
}
