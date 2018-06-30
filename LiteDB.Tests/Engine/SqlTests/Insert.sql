--
-- SQL Tests
--
-- All command will be run at once, so always use ; after each command
-- If want test command result use "SET @par = [command]" to get result into "par" output parameter

-- INSERT single document
SET @insert1 = INSERT INTO col1 VALUES { name: "John" };

-- INSERT multiple documents
SET @insert3 = INSERT INTO col1 VALUES 
	{ name: "Doe", age: 22 }, 
	{ name: "Carlos", age: 42 }, 
	{ name: "Marcus", age: 58 };

-- INSERT using defined AutoId
INSERT INTO col_datatype:int      VALUES { is_int: true };
INSERT INTO col_datatype:long     VALUES { is_long: true };
INSERT INTO col_datatype:date     VALUES { is_date: true };
INSERT INTO col_datatype:guid     VALUES { is_guid: true };
INSERT INTO col_datatype:objectid VALUES { is_objectid: true };

-- SELECT data into output parameters (SELECT always return an BsonArray value)
SET @int      = SELECT ALL COUNT($) FROM col_datatype WHERE is_int = true;
SET @long     = SELECT ALL COUNT($) FROM col_datatype WHERE is_long = true;
SET @date     = SELECT ALL COUNT($) FROM col_datatype WHERE is_date = true;
SET @guid     = SELECT ALL COUNT($) FROM col_datatype WHERE is_guid = true;
SET @objectid = SELECT ALL COUNT($) FROM col_datatype WHERE is_objectid = true;

