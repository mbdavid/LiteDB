---
title: 'MISC'
draft: false
weight: 5
---

### Collection Renaming
```
RENAME COLLECTION <collection> TO <newName>
``` 
- `collection` is the current name of the collection.
- `newName` is the new name of the collection.

### Drop
```
DROP INDEX <collection>.<indexName>
DROP COLLECTION <collection>
```
- `collection` is the name of the collection.
- `indexName` is the name of the index to be dropped.

### Create
```
CREATE [ UNQIUE ] INDEX {indexName} ON {collection} ({indexExpr})
```
- `indexName` is the name of the index being created.
- `collection` is the name of the collection.
- `indexExpr` is the expression being indexed. For more info, see [Indexes](../../docs/indexes).

### Begin
```
BEGIN [ TRANS | TRANSACTION ]
```

### Rebuild
```
REBUILD <rebuildOptions>
```
- For more info, see [Rebuild Options](../../docs/pragmas#rebuildOptions).