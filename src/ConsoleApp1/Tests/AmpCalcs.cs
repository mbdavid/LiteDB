//using static LiteDB.Constants;


//var tests = new uint[] { 1, 2, 8, 9, 16, 17, 16319, 16320, 16321, 16322, 32641, 32642, 32643, 32650, 32651, 16333, 32656 };

//Console.WriteLine("AM_EXTEND_COUNT: {0}", AM_EXTEND_COUNT);
//Console.WriteLine("AM_PAGE_STEP: {0}", AM_PAGE_STEP);
//Console.WriteLine("AM_MAP_PAGES_COUNT: {0}", AM_MAP_PAGES_COUNT);

//foreach (var test in tests)
//{
//    var page = new
//    {
//        PageID = test
//    };

//    var allocationMapID = (int)(page.PageID / AM_PAGE_STEP);
//    var extendIndex = (page.PageID - 1 - allocationMapID * AM_PAGE_STEP) / AM_EXTEND_SIZE;
//    var pageIndex = page.PageID - 1 - allocationMapID * AM_PAGE_STEP - extendIndex * AM_EXTEND_SIZE;

//    Console.WriteLine("{0}: {1}, {2}, {3}", page.PageID, allocationMapID, extendIndex, pageIndex);
//    Console.WriteLine("{0}: {1}", page.PageID, GetBlockPageID(allocationMapID, extendIndex, pageIndex));
//}

//uint GetBlockPageID(int allocationMapID, long extendIndex, long pageIndex)
//{
//    return (uint)
//        (allocationMapID * AM_PAGE_STEP +
//            extendIndex * AM_EXTEND_SIZE +
//            pageIndex + 1);
//}

