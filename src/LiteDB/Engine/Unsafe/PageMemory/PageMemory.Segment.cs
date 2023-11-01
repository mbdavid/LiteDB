namespace LiteDB.Engine;

unsafe internal partial struct PageMemory // PageMemory.Segment
{
    public static PageSegment* GetSegmentPtr(PageMemory* page, ushort index)
    {
        var segmentOffset = PAGE_SIZE - ((index + 1) * sizeof(PageSegment));

        var segment = (PageSegment*)((nint)page + segmentOffset);

        ENSURE(segment->Length < PAGE_CONTENT_SIZE, new { index, Length = segment->Length });
        ENSURE(segment->Location < PAGE_CONTENT_SIZE, new { index, Location = segment->Location });

        return segment;
    }

    public static PageSegment* InsertSegment(PageMemory* page, ushort bytesLength, ushort index, bool isNewInsert, out bool defrag, out ExtendPageValue newPageValue)
    {
        ENSURE(index < 300, new { bytesLength, index, isNewInsert });
        ENSURE(bytesLength % 8 == 0 && bytesLength > 0, new { bytesLength, index, isNewInsert });

        // mark page as dirty
        page->IsDirty = true;

        // get initial page value to check for changes
        var initialPageValue = page->ExtendPageValue;

        // update highst index if needed
        if (index > page->HighestIndex)
        {
            page->HighestIndex = (short)index;
        }

        // get continuous block
        var continuousBlocks = page->FreeBytes - page->FragmentedBytes;

        //TODO: converter em um ensure
        ENSURE(page->FreeBytes >= bytesLength);
        ENSURE(continuousBlocks == PAGE_SIZE - page->NextFreeLocation - page->FooterSize, "ContinuosBlock must be same as from NextFreePosition",
            new { continuousBlocks, isNewInsert });

        // if continuous blocks are not big enough for this data, must run page defrag
        defrag = bytesLength > continuousBlocks;

        if (defrag)
        {
            Defrag(page);
        }

        // get segment addresses
        var segment = PageMemory.GetSegmentPtr(page, index);

        ENSURE(segment->IsEmpty, "segment must be free in insert", *segment);

        // get next free location in page
        var location = page->NextFreeLocation;

        // update segment footer
        segment->Location = location;
        segment->Length = bytesLength;

        // update next free location and counters
        page->ItemsCount++;
        page->UsedBytes += bytesLength;
        page->NextFreeLocation += bytesLength;

        var footerPosition = PAGE_SIZE - page->FooterSize;

        ENSURE(location + bytesLength < footerPosition, "New buffer slice could not override footer area",
            new { location, bytesLength });

        // check for change on extend pageValue
        newPageValue = initialPageValue == page->ExtendPageValue ? ExtendPageValue.NoChange : page->ExtendPageValue;

        // create page segment based new inserted segment
        return segment;
    }

    /// <summary>
    /// Remove index slot about this page segment. Returns deleted page segment
    /// </summary>
    public static void DeleteSegment(PageMemory* page, ushort index, out ExtendPageValue newPageValue)
    {
        // mark page as dirty
        page->IsDirty = true;

        // get initial page value to check for changes
        var initialPageValue = page->ExtendPageValue;

        // read block position on index slot
        var segment = PageMemory.GetSegmentPtr(page, index);

        ENSURE(!segment->IsEmpty);
        ENSURE(!segment->AsSpan(page).IsFullZero());

        // add as free blocks
        page->ItemsCount--;
        page->UsedBytes -= segment->Length;

        // clean block area with \0
        segment->AsSpan(page).Fill(0);

        // check if deleted segment are at end of page
        var isLastSegment = (segment->EndLocation == page->NextFreeLocation);

        if (isLastSegment)
        {
            // update next free location with this deleted segment
            page->NextFreeLocation = segment->Location;
        }
        else
        {
            // if segment is in middle of the page, add this blocks as fragment block
            page->FragmentedBytes += segment->Length;
        }

        // if deleted if are HighestIndex, update HighestIndex
        if (page->HighestIndex == index)
        {
            PageMemory.RemoveHighestIndex(page);
        }

        // if there is no more blocks in page, clean FragmentedBytes and NextFreePosition
        if (page->ItemsCount == 0)
        {
            ENSURE(page->HighestIndex == -1, "if there is no items, HighestIndex must be clear");
            ENSURE(page->UsedBytes == 0, "should be no bytes used in clean page");

            page->NextFreeLocation = PAGE_HEADER_SIZE;
            page->FragmentedBytes = 0;
        }

        // clear both location/length
        segment->Location = 0;
        segment->Length = 0;

        // check for change on extend pageValue
        newPageValue = initialPageValue == page->ExtendPageValue ? ExtendPageValue.NoChange : page->ExtendPageValue;
    }

    /// <summary>
    /// </summary>
    public static PageSegment* UpdateSegment(PageMemory* page, ushort index, ushort bytesLength, out bool defrag, out ExtendPageValue newPageValue)
    {
        ENSURE(bytesLength % 8 == 0 && bytesLength > 0, new { bytesLength });

        // mark page as dirty
        page->IsDirty = true;

        // get initial page value to check for changes
        var initialPageValue = page->ExtendPageValue;

        // read page segment
        var segment = PageMemory.GetSegmentPtr(page, index);

        ENSURE(page->FreeBytes - segment->Length >= bytesLength, $"There is no free space in page {page->PageID} for {bytesLength} bytes required (free space: {page->FreeBytes}");

        // check if current segment are at end of page
        var isLastSegment = (segment->EndLocation == page->NextFreeLocation);

        // best situation: same length
        if (bytesLength == segment->Length)
        {
            defrag = false;
            newPageValue = ExtendPageValue.NoChange;

            return segment;
        }
        // when new length are less than original length (will fit in current segment)
        else if (bytesLength < segment->Length)
        {
            var diff = (ushort)(segment->Length - bytesLength); // bytes removed (should > 0)

            ENSURE(diff % 8 == 0, "diff must be padded", new { diff, segment = *segment, bytesLength });

            if (isLastSegment)
            {
                // if is at end of page, must get back unused blocks 
                page->NextFreeLocation -= diff;
            }
            else
            {
                // is this segment are not at end, must add this as fragment
                page->FragmentedBytes += diff;
            }

            // less blocks will be used
            page->UsedBytes -= diff;

            // update length
            segment->Length = bytesLength;

            // clear fragment bytes
            var fragment = (byte*)((nint)page + segment->Location + bytesLength);

            MarshalEx.FillZero(fragment, diff);

            defrag = false;

            // check for change on extend pageValue
            newPageValue = initialPageValue == page->ExtendPageValue ? ExtendPageValue.NoChange : page->ExtendPageValue;

            return segment;
        }
        // when new length are large than current segment must remove current item and add again
        else
        {
            // clear current block
            segment->AsSpan(page).Fill(0);

            page->ItemsCount--;
            page->UsedBytes -= segment->Length;

            if (isLastSegment)
            {
                // if segment is end of page, must update next free location to current segment location
                page->NextFreeLocation = segment->Location;
            }
            else
            {
                // if segment is on middle of page, add content length as fragment bytes
                page->FragmentedBytes += segment->Length;
            }

            // clear slot index location/length
            segment->Location = 0;
            segment->Length = 0;

            // call insert
            return InsertSegment(page, bytesLength, index, false, out defrag, out newPageValue);
        }
    }

    /// <summary>
    /// Defrag method re-organize all byte data content removing all fragmented data. This will move all page blocks
    /// to create a single continuous content area (just after header area). No index block will be changed (only positions)
    /// </summary>
    private static void Defrag(PageMemory* page)
    {
        ENSURE(page->FragmentedBytes > 0);
        ENSURE(page->ItemsCount > 0);
        ENSURE(page->HighestIndex > 0);

        // first get all blocks inside this page sorted by location (location, index)
        var blocks = new SortedList<ushort, ushort>(page->ItemsCount);

        // get first segment
        var segment = PageMemory.GetSegmentPtr(page, 0);

        // read all segments from footer
        for (ushort index = 0; index <= page->HighestIndex; index++)
        {
            // get only used index
            if (!segment->IsEmpty)
            {
                // sort by position
                blocks.Add(segment->Location, index);
            }

            segment--;
        }

        // here first block position
        var next = (ushort)PAGE_HEADER_SIZE;

        // now, list all segments order by location
        foreach (var slot in blocks)
        {
            var index = slot.Value;
            var location = slot.Key;

            // get segment address
            var addr = PageMemory.GetSegmentPtr(page, index);

            // if current segment are not as excpect, copy buffer to right position (excluding empty space)
            if (location != next)
            {
                ENSURE(location > next, "current segment position must be greater than current empty space", new { location, next });

                // copy from original location into new (correct) location
                var sourceSpan = addr->AsSpan(page); // new Span<byte>((byte*)((nint)page + location), addr->Length);
                var destSpan = new Span<byte>((byte*)((nint)page + next), addr->Length);

                sourceSpan.CopyTo(destSpan);

                // update new location for this index (on footer page)
                addr->Location = next;
            }

            next += addr->Length;
        }

        // fill all non-used content area with 0
        var endContent = PAGE_SIZE - page->FooterSize;

        // fill new are with 0
        var emptyAddr = (byte*)((nint)page + next);
        var emptyLength = endContent - next;

        MarshalEx.FillZero(emptyAddr, emptyLength);

        // clear fragment blocks (page are in a continuous segment)
        page->FragmentedBytes = 0;
        page->NextFreeLocation = next;
    }

    /// <summary>
    /// Get a free index slot in this page
    /// </summary>
    public static ushort GetFreeIndex(PageMemory* page)
    {
        // get first pointer do 0 index segment
        var segment = PageMemory.GetSegmentPtr(page, 0);
        var index = (ushort)0;

        // loop, in pointers, to check for empty segment
        while (!segment->IsEmpty)
        {
            segment--;
            index++;

            ENSURE(index <= 300);
        }

        return index;
    }


    /// <summary>
    /// Update HighestIndex based on current HighestIndex (step back looking for next used slot)
    /// * Used only in Delete() operation *
    /// </summary>
    private static void RemoveHighestIndex(PageMemory* page)
    {
        ENSURE(page->HighestIndex >= 0);

        var index = page->HighestIndex;
        var segment = PageMemory.GetSegmentPtr(page, (ushort)index);

        // reset current index
        segment->Reset();

        // look for first empty index (top to bottom)
        while (segment->IsEmpty && index >= 0)
        {
            segment++;
            index--;
        }

        // update highest index
        page->HighestIndex = index;

        ENSURE(index >= -1 && index < 300);
    }
}
