namespace LiteDB.Engine;

internal class DataService : IDataService
{
    // dependency injection
    private readonly IBsonReader _bsonReader;
    private readonly IBsonWriter _bsonWriter;
    private readonly ITransaction _transaction;

    public DataService(
        IBsonReader bsonReader,
        IBsonWriter bsonWriter,
        ITransaction transaction)
    {
        _bsonReader = bsonReader;
        _bsonWriter = bsonWriter;
        _transaction = transaction;
    }

    /// <summary>
    /// Insert BsonDocument into new data pages
    /// </summary>
    public async ValueTask<RowID> InsertDocumentAsync(byte colID, BsonDocument doc)
    {
        using var _pc = PERF_COUNTER(20, nameof(InsertDocumentAsync), nameof(DataService));

        var docLength = doc.GetBytesCount();

        //if (bytesLeft > MAX_DOCUMENT_SIZE) throw new LiteException(0, "Document size exceed {0} limit", MAX_DOCUMENT_SIZE);

        // rent an array to fit all document serialized
        using var bufferDoc = SharedArray<byte>.Rent(docLength);

        // write all document into buffer doc before copy to pages
        _bsonWriter.WriteDocument(bufferDoc.AsSpan(), doc, out _);

        // get first page
        var ptr = await _transaction.GetFreeDataPageAsync(colID);

        // keep last instance to update nextBlock
        var lastDataBlock = DataBlockResult.Empty;

        // return dataBlockID - will be update in first insert
        var firstDataBlockID = RowID.Empty;

        var bytesLeft = docLength;
        var position = 0;
        var extend = false;
        var defrag = false;

        while (true)
        {
            unsafe
            {
                var page = (PageMemory*)ptr;

                // get how many avaiable bytes (excluding new added record) this page contains
                var pageAvailableSpace =
                    page->FreeBytes -
                    sizeof(DataBlock) - // new data block fixed syze
                    (sizeof(PageSegment) * 2) - // footer (*2 to align)
                    8; // extra align

                var bytesToCopy = Math.Min(pageAvailableSpace, bytesLeft);

                var dataBlock = PageMemory.InsertDataBlock(page, bufferDoc.AsSpan(position, bytesToCopy), extend, out defrag, out var newPageValue);

                if (newPageValue != ExtendPageValue.NoChange)
                {
                    _transaction.UpdatePageMap(page->PageID, newPageValue);
                }

                if (lastDataBlock.IsEmpty == false)
                {
                    ENSURE(dataBlock.Page->PageID != lastDataBlock.Page->PageID);

                    // update NextDataBlock from last page
                    lastDataBlock.DataBlock->NextBlockID = dataBlock.DataBlockID;
                }
                else
                {
                    // get first dataBlock dataBlockID
                    firstDataBlockID = dataBlock.DataBlockID;
                }

                bytesLeft -= bytesToCopy;
                position += bytesToCopy;

                ENSURE(bytesToCopy > 0);

                if (bytesLeft == 0) break;

                // keep last instance
                lastDataBlock = dataBlock;

            }

            ptr = await _transaction.GetFreeDataPageAsync(colID);

            // mark next data block as extend
            extend = true;
        }

        return firstDataBlockID;
    }

    /// <summary>
    /// Update existing document in a single or multiple pages
    /// </summary>
    public async ValueTask UpdateDocumentAsync(RowID dataBlockID, BsonDocument doc)
    {
        var docLength = doc.GetBytesCount();

        //if (bytesLeft > MAX_DOCUMENT_SIZE) throw new LiteException(0, "Document size exceed {0} limit", MAX_DOCUMENT_SIZE);

        // rent an array to fit all document serialized
        using var bufferDoc = SharedArray<byte>.Rent(docLength);

        // write all document into buffer doc before copy to pages
        _bsonWriter.WriteDocument(bufferDoc.AsSpan(), doc, out _);

        // get current datablock (for first one)
        var ptr = await _transaction.GetPageAsync(dataBlockID.PageID);

//        //TODO: SOMENTE PRIMEIRA PAGINA
        unsafe
        {
            var page = (PageMemory*)ptr;

            PageMemory.UpdateDataBlock(page, dataBlockID.Index, bufferDoc.AsSpan(), RowID.Empty, out var _, out var newPageValue);

            if (newPageValue != ExtendPageValue.NoChange)
            {
                _transaction.UpdatePageMap(page->PageID, newPageValue);
            }
        }
    }

    /// <summary>
    /// Read a single document in a single/multiple pages
    /// </summary>
    public async ValueTask<BsonReadResult> ReadDocumentAsync(RowID dataBlockID, string[] fields)
    {
        using var _pc = PERF_COUNTER(30, nameof(ReadDocumentAsync), nameof(DataService));

        var ptr = await _transaction.GetPageAsync(dataBlockID.PageID);

        // get data block segment
        var dataBlock = new DataBlockResult(ptr, dataBlockID);

        unsafe
        {
            if (dataBlock.DataBlock->NextBlockID.IsEmpty)
            {
                // get content buffer inside dataBlock 
                var resultSingle = _bsonReader.ReadDocument(dataBlock.AsSpan(), fields, false, out _);

                return resultSingle;
            }
        }

        // get a full array to read all document chuncks
        using var docBuffer = SharedArray<byte>.Rent(dataBlock.DocumentLength);

        // copy datablock content to new in memory buffer
        dataBlock.AsSpan().CopyTo(docBuffer.AsSpan());

        var position = dataBlock.ContentLength;

        ENSURE(dataBlock.DocumentLength > 0, new { dataBlock });

        RowID nextBlockID;
        
        unsafe
        {
            nextBlockID = dataBlock.DataBlock->NextBlockID;
        }

        while (nextBlockID.IsEmpty)
        {
            ptr = await _transaction.GetPageAsync(dataBlock.DataBlockID.PageID);

            dataBlock = new DataBlockResult(ptr, nextBlockID);

            // copy datablock content to new in memory buffer
            dataBlock.AsSpan().CopyTo(docBuffer.AsSpan(position));

            position += dataBlock.ContentLength;

            unsafe
            {
                // retain nextBlockID before change page
                nextBlockID = dataBlock.DataBlock->NextBlockID;
            }
        }

        var result = _bsonReader.ReadDocument(docBuffer.AsSpan(), fields, false, out _);

        return result;
    }

    /// <summary>
    /// Delete a full document from a single or multiple pages
    /// </summary>
    public async ValueTask DeleteDocumentAsync(RowID dataBlockID)
    {
        while (true)
        {
            // get page from dataBlockID
            var ptr = await _transaction.GetPageAsync(dataBlockID.PageID);

            unsafe
            {
                var page = (PageMemory*)ptr;
                var dataBlock = new DataBlockResult(page, dataBlockID);

                // copy values before delete
                var nextBlockID = dataBlock.DataBlock->NextBlockID;

                // delete dataBlock (do not use "dataBlock" after here because are "fully zero")
                PageMemory.DeleteSegment(page, dataBlockID.Index, out var newPageValue);

                // checks if extend pageValue changes
                if (newPageValue != ExtendPageValue.NoChange)
                {
                    // update allocation map after change page
                    _transaction.UpdatePageMap(page->PageID, newPageValue);
                }

                // stop if there is not block to delete
                if (nextBlockID.IsEmpty) break;

                // go to next block
                dataBlockID = nextBlockID;
            }
        }
    }

    /*

        /// <summary>
        /// Update document using same page position as reference
        /// </summary>
        public void Update(CollectionPage col, RowID blockAddress, BsonDocument doc)
        {
            var bytesLeft = doc.GetBytesCount(true);

            if (bytesLeft > MAX_DOCUMENT_SIZE) throw new LiteException(0, "Document size exceed {0} limit", MAX_DOCUMENT_SIZE);

            __DataBlock lastBlock = null;
            var updateAddress = blockAddress;

            IEnumerable <BufferSlice> source()
            {
                var bytesToCopy = 0;

                while (bytesLeft > 0)
                {
                    // if last block contains new block sequence, continue updating
                    if (updateAddress.IsEmpty == false)
                    {
                        var dataPage = _snapshot.GetPage<DataPage>(updateAddress.PageID);
                        var currentBlock = dataPage.GetBlock(updateAddress.Index);

                        // try get full page size content (do not add DATA_BLOCK_FIXED_SIZE because will be added in UpdateBlock)
                        bytesToCopy = Math.Min(bytesLeft, dataPage.FreeBytes + currentBlock.Buffer.Count);

                        var updateBlock = dataPage.UpdateBlock(currentBlock, bytesToCopy);

                        _snapshot.AddOrRemoveFreeDataList(dataPage);

                        yield return updateBlock.Buffer;

                        lastBlock = updateBlock;

                        // go to next address (if exists)
                        updateAddress = updateBlock.NextBlock;
                    }
                    else
                    {
                        bytesToCopy = Math.Min(bytesLeft, MAX_DATA_BYTES_PER_PAGE);
                        var dataPage = _snapshot.GetFreeDataPage(bytesToCopy + __DataBlock.DATA_BLOCK_FIXED_SIZE);
                        var insertBlock = dataPage.InsertBlock(bytesToCopy, true);

                        if (lastBlock != null)
                        {
                            lastBlock.SetNextBlock(insertBlock.Position);
                        }

                        _snapshot.AddOrRemoveFreeDataList(dataPage);

                        yield return insertBlock.Buffer;

                        lastBlock = insertBlock;
                    }

                    bytesLeft -= bytesToCopy;
                }

                // old document was bigger than current, must delete extend blocks
                if (lastBlock.NextBlock.IsEmpty == false)
                {
                    var nextBlockAddress = lastBlock.NextBlock;

                    lastBlock.SetNextBlock(RowID.Empty);

                    this.Delete(nextBlockAddress);
                }
            }

            // consume all source bytes to write BsonDocument direct into PageBuffer
            // must be fastest as possible
            using (var w = new BufferWriter(source()))
            {
                // already bytes count calculate at method start
                w.WriteDocument(doc, false);
                w.Consume();
            }
        }

    */
}