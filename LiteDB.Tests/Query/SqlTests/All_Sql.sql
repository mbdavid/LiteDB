--
-- SQL Parser Tests
--
-- All command will be run at once, so always use ; after each command
-- If want test command result use "SET @par = [command]" to get result into "par" output parameter

-- For this tests, database have "person" collection with 1000 documents like this:
-- { _id: <int>, "name": "Kelsey Garza", "age": 66, "phone": "624-744-6218", "email": "Kelly@suscipit.edu", "address": "62702 West Bosnia and Herzegovina Way", "city": "Wheaton", "state": "MO", "date": { "$date": "1950-08-07"}, "active": true }

-- INSERT
SET @insert1 = INSERT INTO col0 VALUES { name: "John" };

-- INSERT multiple documents
SET @insert3 = INSERT INTO col0 VALUES { name: "Doe", age: 22 }, { name: "Carlos", age: 42 }, { name: "Marcus", age: 58 };

-- INSERT using defined AutoId
INSERT INTO col_datatype:int      VALUES { is_int: true };
INSERT INTO col_datatype:long     VALUES { is_long: true };
INSERT INTO col_datatype:date     VALUES { is_date: true };
INSERT INTO col_datatype:guid     VALUES { is_guid: true };
INSERT INTO col_datatype:objectid VALUES { is_objectid: true };

SET @int      = SELECT ALL COUNT($) FROM col_datatype WHERE is_int = true;
SET @long     = SELECT ALL COUNT($) FROM col_datatype WHERE is_long = true;
SET @date     = SELECT ALL COUNT($) FROM col_datatype WHERE is_date = true;
SET @guid     = SELECT ALL COUNT($) FROM col_datatype WHERE is_guid = true;
SET @objectid = SELECT ALL COUNT($) FROM col_datatype WHERE is_objectid = true;

-- CREATE INDEX
SET @index       = CREATE INDEX idx_name ON person (name);

-- CREATE UNIQUE INDEX
SET @uniqueIndex = CREATE UNIQUE INDEX idx_email ON person (email);

-- UPDATE
SET @update = UPDATE person SET { age: age + 5, email: LOWER(email) } WHERE age > 0;

-- BEGIN TRANSACTION
SET @trans = BEGIN;

INSERT INTO col1 VALUES {a:1};

-- COMMIT (return true)
SET @commit = COMMIT;

BEGIN;

INSERT INTO col1 VALUES {a:2};

-- ROLLBACK (return true)
SET @rollback = ROLLBACK;

SET @col1 = SELECT ALL COUNT($) FROM col1;

-- SET USERVERSION
SET USERVERSION  = 99;

SET @userversion = SELECT userVersion FROM $database;

-- SELECT SIMPLE
SET @simple[]  = SELECT $ FROM person;

-- SELECT INTO
SET @into      = SELECT { name, age } INTO person_ca:int FROM person WHERE state = 'CA';

-- SELECT ORDER BY/LIMIT/OFFSET
SET @orderBy[] = SELECT $ FROM person ORDER BY name LIMIT 10 OFFSET 10;

-- SELECT FOR UPDATE
SET @forUpdate = SELECT $ FROM person FOR UPDATE;

-- SELECT INCLUDE
SET @include   = SELECT $ FROM person INCLUDE inc1, inc2;

-- SELECT GROUP BY/HAVING
SET @groupBy1[] = 
	SELECT { state, count: COUNT($) }
	  FROM person
	 WHERE state IN ["FL", "CA", "MN", "TX", "MS"]
	 GROUP BY state
	HAVING count > 5;

-- SELECT ALL
SET @count   = SELECT ALL COUNT(_id) FROM person;    

-- EXPLAIN
SET @explain = EXPLAIN SELECT $ FROM person WHERE _id = 1;

-- DELETE
SET @delete  = DELETE person WHERE _id IN [1, 2, 3];



-- CHECKPOINT, VACCUM, ANALYZE, SHRINK
-- CHECKPOINT;
-- 
-- VACCUM;
-- 
-- ANALYZE;
-- 
-- SHRINK;
