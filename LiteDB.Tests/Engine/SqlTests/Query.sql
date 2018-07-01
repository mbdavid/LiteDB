--
-- SQL Tests
--
-- All command will be run at once, so always use ; after each command
-- If want test command result use "SET @par = [command]" to get result into "par" output parameter

-- For this tests, database have "person" collection with 200 documents like this:
-- { _id: <int>, "name": "Kelsey Garza", "age": 66, "phone": "624-744-6218", "email": "Kelly@suscipit.edu", "address": "62702 West Bosnia and Herzegovina Way", "city": "Wheaton", "state": "MO", "date": { "$date": "1950-08-07"}, "active": true }

-- ORDER BY With LIMIT/OFFSET
SET @nameAll[] = SELECT name FROM person ORDER BY name;
SET @name100   = SELECT name FROM person  ORDER BY name LIMIT 1 OFFSET 100;

-- SELECT INTO another collection
SELECT { name, age, email: LOWER(email) } INTO col2:int FROM person WHERE age > 20;

SET @col2count = SELECT ALL COUNT(age) FROM col2;

-- SELECT ALL
SET @agesInCA = SELECT ALL { first: name, ages: SUM(age) } FROM person WHERE state = 'CA';

-- SELECT FOR UPDATE
SELECT $ FROM person FOR UPDATE;

-- SELECT with GROUP BY and ARRAY/DOCUMENT aggregation (will return domail name + array with users user + name + age)
SET @top10Domains[] =
SELECT { 
           domain: SUBSTRING(email, INDEXOF(email, '@') + 1), 
           users: [{
               user: LOWER(SUBSTRING(email, 0, INDEXOF(email, '@'))), 
               name, 
               age
           }]
       }
  FROM person
 GROUP BY SUBSTRING(email, INDEXOF(email, '@') + 1)
 ORDER BY LENGTH(users) DESC
 LIMIT 10;

-- SELECT with GROUP BY and HAVING
SET @groupBy1[] = 
	SELECT { state, count: COUNT($) }
	  FROM person
	 WHERE state IN ["FL", "CA", "MN", "TX", "MS"]
	 GROUP BY state
	HAVING count > 5
