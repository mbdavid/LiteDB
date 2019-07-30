---
title: 'Concurrency'
date: 2019-02-11T19:30:08+10:00
draft: false
weight: 1
---

LiteDB v4 supports both thread-safe and process-safe:

- You can create a new instance of `LiteRepository`, `LiteDatabase` or `LiteEngine` in each use (process-safe)
- You can share a single `LiteRepository`, `LiteDatabase` or `LiteEngine` instance across your threads (thread-safe)

In first option (process safe), you will always work disconnected from the datafile. Each use will open datafile, lock file (read or write mode), do your operation and then close datafile. Locks are implemented using `FileStream.Lock` for both read/write mode. It's very important in this way to always use `using` statement to close datafile.

In second option (thread-safe), LiteDB controls concurrency using `ReaderWriterLockSlim` .NET class. With this class it's possible manage multiple reads and an exclusive write. All threads share same instance and each method control concurrency. 

## Recommendation

Single instance (second option) is much faster than multi instances. In multi instances environment, each instance must do expensive data file operations: open, lock, unlock, read, close. Also, each instance has its own cache control and, if used only for a single operation, will discard all cached pages on close of datafile. In single instance, all pages in cache are shared between all read threads.

If your application works in a single process (like mobile apps, asp.net websites) prefer to use a single database instance and share across all threads.

You can use `Exclusive` mode (in connection string). Using exclusive mode, datafile will avoid checking header page for any any external change. Also, exclusive mode do not use Lock/Unlock in file, only in memory (using `ReaderWriterLockSlim` class).
