﻿namespace MVSystemApi.Model
{
    public class PagingDTO
    {
        //public int PageIndex { get; set; }
        //public int PageSize { get; set; }
        public int Line { get; set; }
        public int LastLine { get; set; }
        public int TotalRecord { get; set; }
    }
}
