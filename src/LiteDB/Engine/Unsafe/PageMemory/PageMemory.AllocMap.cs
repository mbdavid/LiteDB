namespace LiteDB.Engine;

unsafe internal partial struct PageMemory // PageMemory.AllocMap
{
    public static (int extendIndex, int pageIndex, bool isNew) GetFreeExtend(PageMemory* page, int currentExtendIndex, byte colID, PageType type)
    {
        ENSURE(page->PageType == PageType.AllocationMap);

        while (currentExtendIndex < AM_EXTEND_COUNT)
        {
            // get extend value as uint
            var extendValue = page->Extends[currentExtendIndex];

            var (pageIndex, isNew) = HasFreeSpaceInExtend(extendValue, colID, type);

            // current extend contains a valid page
            if (pageIndex >= 0)
            {
                return (currentExtendIndex, pageIndex, isNew);
            }

            // test if current extend are not empty (create extend here)
            if (extendValue == 0)
            {
                // update extend value with only colID value in first 1 byte (shift 3 bytes)
                page->Extends[currentExtendIndex] = (uint)(colID << 24);

                return (currentExtendIndex, 0, true);
            }

            // go to next index
            currentExtendIndex++;
        }

        return (-1, 0, false);
    }

    /// <summary>
    /// Update extend value based on extendIndex (0-2031) and pageIndex (0-7)
    /// </summary>
    public static void UpdateExtendPageValue(PageMemory* page, int extendIndex, int pageIndex, ExtendPageValue pageValue)
    {
        ENSURE(extendIndex <= 2031);
        ENSURE(pageIndex <= 7);

        // get extend value from array
        var value = page->Extends[extendIndex];

        // update value (3 bits) according pageIndex
        var extendValue = pageIndex switch
        {
            0 => (value & 0b11111111_000_111_111_111_111_111_111_111) | ((uint)pageValue << 21),
            1 => (value & 0b11111111_111_000_111_111_111_111_111_111) | ((uint)pageValue << 18),
            2 => (value & 0b11111111_111_111_000_111_111_111_111_111) | ((uint)pageValue << 15),
            3 => (value & 0b11111111_111_111_111_000_111_111_111_111) | ((uint)pageValue << 12),
            4 => (value & 0b11111111_111_111_111_111_000_111_111_111) | ((uint)pageValue << 9),
            5 => (value & 0b11111111_111_111_111_111_111_000_111_111) | ((uint)pageValue << 6),
            6 => (value & 0b11111111_111_111_111_111_111_111_000_111) | ((uint)pageValue << 3),
            7 => (value & 0b11111111_111_111_111_111_111_111_111_000) | ((uint)pageValue),
            _ => throw new InvalidOperationException()
        };

        // update extend array value
        page->Extends[extendIndex] = extendValue;

        // mark page as dirty (in map page this should be manual)
        page->IsDirty = true;

    }

    #region Static Helpers

    /// <summary>
    /// Check if extend value contains a page that fit on request (type/length)
    /// Returns pageIndex if found or -1 if this extend has no space. Returns if page is new (empty)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (int pageIndex, bool isNew) HasFreeSpaceInExtend(uint extendValue, byte colID, PageType type)
    {
        // extendValue (colID + 8 pages values)

        //  01234567   01234567   01234567   01234567
        // [________] [________] [________] [________]
        //  ColID      00011122   23334445   55666777

        // 000 - empty
        // 001 - data 
        // 010 - index 
        // 011 - reserved
        // 100 - reserved
        // 101 - reserved
        // 110 - reserved
        // 111 - full

        // check for same colID
        if (colID != extendValue >> 24) return (-1, false);

        uint result;

        if (type == PageType.Data)
        {
            // 000 - empty
            // 001 - data

            var notA = (extendValue & 0b00000000_100_100_100_100_100_100_100_100) ^ 0b00000000_100_100_100_100_100_100_100_100;
            var notB = (extendValue & 0b00000000_010_010_010_010_010_010_010_010) ^ 0b00000000_010_010_010_010_010_010_010_010;

            notB <<= 1;

            result = notA & notB;
        }
        else if (type == PageType.Index)
        {
            // 000 - empty
            // 010 - index

            var notA = (extendValue & 0b00000000_100_100_100_100_100_100_100_100) ^ 0b00000000_100_100_100_100_100_100_100_100;
            var notC = (extendValue & 0b00000000_001_001_001_001_001_001_001_001) ^ 0b00000000_001_001_001_001_001_001_001_001;

            notC <<= 2;

            result = notA & notC;
        }
        else
        {
            return (-1, false);
        }

        if (result > 0)
        {
            var pageIndex = result switch
            {
                <= 31 => 7,       // 2^(3+2)-1
                <= 255 => 6,      // 2^(6+2)-1
                <= 2047 => 5,     // 2^(9+2)-1
                <= 16383 => 4,    // 2^(12+2)-1
                <= 131071 => 3,   // 2^(15+2)-1
                <= 1048575 => 2,  // 2^(18+2)-1
                <= 8388607 => 1,  // 2^(21+2)-1
                <= 67108863 => 0, // 2^(24+2)-1
                _ => throw new NotSupportedException()
            };

            var isEmpty = (extendValue & (0b111 << ((7 - pageIndex) * 3))) == 0;

            return (pageIndex, isEmpty);
        }
        else
        {
            return (-1, false);
        }
    }

    /// <summary>
    /// Returns a AllocationMapID from a allocation map pageID. Must return 0, 1, 2, 3
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetAllocationMapID(uint pageID)
    {
        return (pageID - AM_FIRST_PAGE_ID) % AM_EXTEND_COUNT;
    }

    /// <summary>
    /// Get a value (0-7) thats represent diferent page types/avaiable spaces
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ExtendPageValue GetExtendPageValue(PageType pageType, int freeBytes)
    {
        return (pageType, freeBytes) switch
        {
            (_, PAGE_CONTENT_SIZE) => ExtendPageValue.Empty,
            (PageType.Data, >= AM_DATA_PAGE_FREE_SPACE) => ExtendPageValue.Data,
            (PageType.Index, >= AM_INDEX_PAGE_FREE_SPACE) => ExtendPageValue.Index,
            _ => ExtendPageValue.Full
        };
    }

    #endregion
}
