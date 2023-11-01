namespace LiteDB;

unsafe internal static class PageDump
{
    public static string Render(PageMemory* page)
    {
        var sb = StringBuilderCache.Acquire();

        sb.AppendLine($"# PageBuffer.: {{ UniqueID = {page->UniqueID}, PositionID = {Dump.PageID(page->PositionID)}, SharedCounter = {page->ShareCounter}, IsDirty = {page->IsDirty} }}");

        sb.AppendLine();

        if (page->PageType == PageType.AllocationMap)
        {
            RenderAllocationMapPage(page, sb);
        }
        else if (page->PageType == PageType.Data)
        {
            RenderDataPage(page, sb);
        }
        else if (page->PageType == PageType.Index)
        {
            RenderIndexPage(page, sb);
        }

        sb.AppendLine();

        RenderPageDump(page, sb);

        var output = StringBuilderCache.Release(sb);

        return output;
    }

    private static void RenderAllocationMapPage(PageMemory* page, StringBuilder sb)
    {
        var allocationMapID = PageMemory.GetAllocationMapID(page->PageID);

        for (var i = 0; i < AM_EXTEND_COUNT; i++)
        {
            var extendLocation = new ExtendLocation((int)allocationMapID, i);
            var extendValue = page->Extends[i];

            var extendID = extendLocation.ExtendID.ToString().PadLeft(4, ' ');
            var colID = (extendValue >> 24).ToString().PadLeft(3, ' ');
            var firstPageID = Dump.PageID(extendLocation.FirstPageID);
            var ev = Dump.ExtendValue(extendValue);

            sb.AppendLine($"[{extendID}] = {colID} = [{firstPageID}] => {ev}");
        }
    }

    private static void RenderDataPage(PageMemory* page, StringBuilder sb)
    {
        var reader = new BsonReader();

        if (page->HighestIndex == ushort.MaxValue)
        {
            sb.AppendLine("# No items");
            return;
        }

        sb.AppendLine("# Segments...:");

        for (ushort i = 0; i <= page->HighestIndex; i++)
        {
            var segment = PageMemory.GetSegmentPtr(page, i);

            var index = i.ToString().PadRight(3, ' ');

            if (!segment->IsEmpty)
            {
                var dataBlock = (DataBlock*)(page + segment->Location);

                var dataContent = dataBlock + sizeof(DataBlock);
                var len = segment->Length - sizeof(DataBlock);
                var span = new Span<byte>(dataContent, len);

                var result = reader.ReadDocument(span, Array.Empty<string>(), false, out _);

                var content = result.Value.ToString() +
                    (result.Fail ? "..." : "");

                sb.AppendLine($"[{index}] = {*segment} => {dataBlock->NextBlockID} = {content}");
            }
            else
            {
                sb.AppendLine($"[{index}] = {*segment}");
            }
        }
    }

    private static void RenderIndexPage(PageMemory* page, StringBuilder sb)
    {
        if (page->HighestIndex == byte.MaxValue)
        {
            sb.AppendLine("# No items");
            return;
        }

        sb.AppendLine("# Segments...:");

        for (ushort i = 0; i < page->HighestIndex; i++)
        {
            var segment = PageMemory.GetSegmentPtr(page, i);

            var index = i.ToString().PadRight(3, ' ');

            if (!segment->IsEmpty)
            {
                var indexNode = (IndexNode*)(page + segment->Location);

                sb.AppendLine($"[{index}] = {*segment} => {*indexNode}");
            }
            else
            {
                sb.AppendLine($"[{index}] = {*segment}");
            }
        }
    }

    private static void RenderPageDump(PageMemory* page, StringBuilder sb) 
    {
        sb.Append("# Page Dump..:");

        for (var i = 0; i < PAGE_SIZE; i++)
        {
            if (i % 32 == 0)
            {
                sb.AppendLine();
                sb.Append("[" + i.ToString().PadRight(4, ' ') + "] ");
            }
            if (i % 32 != 0 && i % 8 == 0) sb.Append(" ");
            if (i % 32 != 0 && i % 16 == 0) sb.Append(" ");

            sb.AppendFormat("{0:X2} ", *(page + i));
        }
    }
}
