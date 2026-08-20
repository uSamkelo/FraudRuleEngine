using System.Collections.Generic;

namespace FraudEngine.Api.Dtos
{
    /// <summary>
    /// Generic wrapper for paginated list responses.
    /// </summary>
    public class PagedResult<T>
    {
        public required IEnumerable<T> Items { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }
    }
}
