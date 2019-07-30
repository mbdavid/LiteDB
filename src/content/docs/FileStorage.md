---
title: 'FileStorage'
date: 2019-02-11T19:30:08+10:00
draft: false
weight: 1
---

To keep its memory profile slim, LiteDB has a limited document size of 1Mb. For text documents, this is a huge size. But for many binary files, 1Mb is too small. LiteDB therefore implements `FileStorage`, a custom collection to store files and streams.

LiteDB uses two special collections to split file content in chunks:

- `_files` collection stores file reference and metadata only

```JS
{
    _id: "my-photo",
    filename: "my-photo.jpg",
    mimeType: "image/jpg",
    length: { $numberLong: "2340000" },
    uploadDate: { $date: "2015-01-01T00:00:00Z" },
    metadata: { "key1": "value1", "key2": "value2" }
}
```

- `_chunks` collection stores binary data in 1MB chunks.

```JS
{
    _id: "my-photo\00001",
    data: { $binary: "VHlwZSAob3Igc ... GUpIGhlcmUuLi4" }
}
{
    _id: "my-photo\00002",
    data: { $binary: "pGaGhlcmUuLi4 ... VHlwZSAob3Igc" }
}
{
   ...
}
```

Files are identified by an `_id` string value, with following rules:

- Starts with a letter, number, `_`, `-`, `$`, `@`, `!`, `+`, `%`, `;` or `.`
- If contains a `/`, must be sequence with chars above 

To better organize many files, you can use `_id` as a `directory/file_id`. This will be a great solution to quickly find all files in a directory using the `Find` method.

Example: `$/photos/2014/picture-01.jpg`

The `FileStorage` collection contains simple methods like:

- **`Upload`**: Send file or stream to database. Can be used with file or `Stream`. If file already exists, file content is overwritten.
- **`Download`**: Get your file from database and copy to `Stream` parameter
- **`Delete`**: Delete a file reference and all data chunks
- **`Find`**: Find one or many files in `_files` collection. Returns `LiteFileInfo` class, that can be download data after.
- **`SetMetadata`**: Update stored file metadata. This method doesn't change the value of the stored file.  It updates the value of `_files.metadata`.
- **`OpenRead`**: Find file by `_id` and returns a `LiteFileStream` to read file content as stream

```C#
// Upload a file from file system
db.FileStorage.Upload("$/photos/2014/picture-01.jpg", @"C:\Temp\picture-01.jpg");

// Upload a file from a Stream
db.FileStorage.Upload("$/photos/2014/picture-01.jpg", "picture-01.jpg", stream);

// Find file reference only - returns null if not found
LiteFileInfo file = db.FileStorage.FindById("$/photos/2014/picture-01.jpg");

// Now, load binary data and save to file system
file.SaveAs(@"C:\Temp\new-picture.jpg");

// Or get binary data as Stream and copy to another Stream
file.CopyTo(Response.OutputStream);

// Find all files references in a "directory"
var files = db.FileStorage.Find("$/photos/2014/");
```

`FileStorage` does not support transactions to avoid putting all of the file in memory before storing it on disk. Transactions *are* used per chunk. Each uploaded chunk is committed in a single transaction.

`FileStorage` keeps `_files.length` at `0` when uploading each chunk. When all chunks finish uploading `FileStorage` updates `_files.length` by aggregating the size of each chunk. If you try to download a zero bytes length file, the uploaded file is corrupted. A corrupted file doesn't necessarily mean a corrupt database. 
