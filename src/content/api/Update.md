---
title: 'UPDATE'
draft: false
weight: 3
---

The following structure defines the SQL update syntax in LiteDB. Keyworks and function names are case-insensitive.

```
  UPDATE <collection>
  SET <key0> = <exprValue0> [,<keyN> = <exprValueN>] | <newDoc>
[ WHERE <filterExpr> ]
 ```
 
- `collection` is the name of the collection where the documents will be inserted.
- Every `key` is the attribute name in the document and the corresponding `exprValue` is an expression that returns the desired value. For more info, see [Expressions](../../docs/expressions).
- `newDoc` is a valid JSON object.
- If the form `<key> = <exprValue>` is used in the `SET` clause, the informed fields will be updated or created in every document returned by the `WHERE` clause.
- If `newDoc` is used in the `SET` clause, the documents returned by the `WHERE` clause will be entirely replaced by `newDoc`.
- `filterExpr` is any valid filter expression. For more info, check [Where clause](../query#where)