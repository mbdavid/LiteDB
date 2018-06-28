--
-- SQL Tests
--
-- All command will be run at once, so always use ; after each command
-- If want test command result use "SET @par = [command]" to get result into "par" output parameter

SET @count1 = UPDATE person SET { age: 99 } WHERE _id = 1;

SET @newAge = SELECT age FROM person WHERE _id = 1;