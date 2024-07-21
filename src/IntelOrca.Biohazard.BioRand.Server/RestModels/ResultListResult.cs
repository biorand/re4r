using System;
using System.Linq;
using IntelOrca.Biohazard.BioRand.Server.Services;

namespace IntelOrca.Biohazard.BioRand.Server.RestModels
{
    public class ResultListResult
    {
        public int Page { get; set; }
        public int PageCount { get; set; }
        public int TotalResults { get; set; }
        public int PageStart { get; set; }
        public int PageEnd { get; set; }
        public object[] PageResults { get; set; } = [];

        public static ResultListResult Map<TResult, TMapped>(
            int page,
            int itemsPerPage,
            LimitedResult<TResult> result,
            Func<TResult, TMapped> selector) where TMapped : notnull
        {
            return new ResultListResult()
            {
                Page = page,
                PageCount = (result.Total + itemsPerPage - 1) / itemsPerPage,
                TotalResults = result.Total,
                PageStart = result.From,
                PageEnd = result.To,
                PageResults = result.Results
                    .Select(x => (object)selector(x))
                    .ToArray()
            };
        }
    }
}
