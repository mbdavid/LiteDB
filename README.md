# LiteDB

This branch is current development of new version of LiteDB v6.

# Next steps
- SQL Parser
    - rename collection
- Unit tests for query


# Needs implementation

## Engine
- Implement RandomAccess and SafeHandle
- Return to async calls in managed memory
- CRC32

## Know Bugs
- The problem of COUNT($.phones)

## Operations
- Update
- DropIndex
- Batch
- Rebuild
- Vaccum?

## Master
- Review

## Query Engine
- Create IQuery and slipt query in Query GroupByQuery
- IntoPipe
- Distinct Pipe
- Virtual collections: $master, $file_json, ...

## Query Join
- DataStore Alias to support SELECT p._id FROM products p
- JoinPipeEnumerator (DataStore, PathExpression, Inner/Left)
- Link over _id only
```SQL
SELECT c, p
  FROM customers c
 INNER JOIN products p ON c.id_customer (always use products PK to link)
```
- Convert PipeValue in List<(DataStore, PipeValue)> in enumerators
- JoinDataStore - use only _id as key
- JoinPipeEnumerator keeps IndexNode and navigate in "continue mode"

## SubQuery
- Used in FROM (....)
- Used in SELECT (...) AS col
- Used in WHERE _id IN (...)

## SharedMode

## Services
- ErrorHandling review
- try/catch/deallocate
- Auto-Rebuid, Flag error

## BsonExpressions
- MakeDocument spread: { ...$ }
- MakeArray spread: [ ...phones ]
- CoalesceExpression:  a ?? 5 
- ANY: array ANY >= 8 // retorna TRUE se algum item de left (um array) satisfizer a operação com right (single::)

## BsonValue
- DateTimeOffset

## LiteDatabase
- BsonMapper
- LiteCollection, LiteQuery, ...
- LiteStorage

## LiteDB.TestSuite

## Exception
- Normalize all exception using ERR_xxx

## Performance
- Test use of `ref` in pipe context on movenext
- Create extenstion methods for linq
    - from _store.GetIndexes().FirstOrDefault(x => x.Expression == predicate.Left);
    - to _store.GetIndexes().FirstOrDefaultByExpression(predicate.Left);


## Performance boost (future)
- Read stream in extend size 64kb
- Async queue writer
- BsonDocumentReader
- BsonDocumentWriter
- Sort using unsafe (without BsonValue)
- [BsonDocumentGenerate] - map POCO class to BsonDocument and vice-versa
- Transaction MultiThread - a single bulk operation can use multiple threads (using `Parallel`)

## Documentation
- Define docs structure using a menu tree navigation and a single template
