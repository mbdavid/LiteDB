---
title: 'INSERT'
draft: false
weight: 2
---

The following structure defines the SQL insert syntax in LiteDB. Keyworks are case-insensitive.

```
INSERT INTO {collection}[: {autoIdType}]
VALUES {doc0} [, {docN}]
 ```
 
- `collection` is the name of the collection where the documents will be inserted
- `autoIdType` is one of the supported auto id types supported (`GUID`, `INT`, `LONG`, `OBJECTID`). If this construct is not present, the default value is `OBJECTID`.
- Every document after keyword `VALUES` must be a valid JSON object. Documents must be comma-separated. 