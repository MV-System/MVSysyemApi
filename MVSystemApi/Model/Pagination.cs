using DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MVSystemApi.Model
{
    public class Pagination<T>: List<T>
    {

        public int CurrentPage { get; private set; }
        public int TotalPages { get;  set; }
        public int PageSize { get;  set; }
        public int TotalCount { get;  set; }

        //public int PageIndex { get; private set; }

        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
        public Pagination( List<T> items, int count, int pageNumber,int pageSize)
        {

            TotalCount = count;
            PageSize = pageSize;
            CurrentPage = pageNumber;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            AddRange(items);
        }
        //public bool PrevousPage
        //{
        //    get { 
        //        return PageIndex > 1;
        //    }
        //}
        //public bool NextPage
        //{
        //    get { return PageIndex < TotalPages; }
        //}
        public static  Pagination<T> GetPagination(List<T> source, int pageNumber, int pageSize)
        {

            var count = source.Count();
            var items = source.Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new Pagination<T>(items, count, pageNumber, pageSize);
        }
    }
}
