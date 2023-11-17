



internal class Disco
{

    public async ValueTask Write(Registro r)
    {
        var buffer = new byte[1024];
        var fil = File.OpenHandle(@"c:\LiteDB\temp\demo.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, FileOptions.DeleteOnClose);

        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

        var ptr = handle.AddrOfPinnedObject();


        this.Write2(r.Page);


        //var bytes = (byte[])ptr;

        await RandomAccess.WriteAsync(fil, buffer, 0);




        //handle.Free();

    }

    public unsafe void Write2(PageMemory* page)
    {
    }

}

unsafe internal class Registro
{
    public PageMemory* Page;
}