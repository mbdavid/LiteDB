# LiteDB

This branch is current development of new version of LiteDB v6.

# Next Steps
- Finish sql methods
- Implement RandomAccess e SafeHandle
- Return to async calls in managed memory


# Need implement

## Pragma
- Replace for a simple `struct` and ref in page header

## Master
- Review

## Query Engine
- Create IQuery and slipt query in Query GroupByQuery
- IntoPipe
- Distinct Pipe
- Virtual collections: $master, $file_json, ...

## SQL Parser


## Operations
- Update
- DeleteMany
- DropIndex
- DropCollection
- Batch
- Rebuild
- Vaccum?

## SharedMode

## Services
- ErrorHandling review

## Page Structures
- FileHeader (define big/little endian machine)

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
