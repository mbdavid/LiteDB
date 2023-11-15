



internal unsafe class Disco
{

    public ValueTask Write(PageMemory* page)
    {
        var buffer = new byte[1024];
        var fil = File.OpenHandle(@"c:\LiteDB\temp\demo.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, FileOptions.DeleteOnClose);

        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

        var ptr = handle.AddrOfPinnedObject();


        //var bytes = (byte[])ptr;

        return RandomAccess.WriteAsync(fil, buffer, 0);




        //handle.Free();

    }
}

