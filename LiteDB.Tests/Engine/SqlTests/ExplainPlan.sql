--
-- SQL Tests
--
-- All command will be run at once, so always use ; after each command
-- If want test command result use "SET @par = [command]" to get result into "par" output parameter

-- For this tests, database have "person" collection with 200 documents like this:
-- { _id: <int>, "name": "Kelsey Garza", "age": 66, "phone": "624-744-6218", "email": "Kelly@suscipit.edu", "address": "62702 West Bosnia and Herzegovina Way", "city": "Wheaton", "state": "MO", "date": { "$date": "1950-08-07"}, "active": true }

-- Creating some indexes
CREATE INDEX idx_state ON person (state);
CREATE INDEX idx_age ON person (age);

SET @explain1 = EXPLAIN SELECT $ FROM person WHERE age = 66;

SET @explain2 = EXPLAIN SELECT $ FROM person WHERE age = 66 and state = 'CA';

SET @explain3 = EXPLAIN SELECT age FROM person;

SET @explain4 = EXPLAIN SELECT age FROM person ORDER BY _id DESC;

SET @explain5 = EXPLAIN
	SELECT { state, count: COUNT($) }
	  FROM person
	 WHERE state IN ["FL", "CA", "MN", "TX", "MS"]
	 GROUP BY state
	HAVING count > 5
