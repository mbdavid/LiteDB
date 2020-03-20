---
title: 'Functions'
draft: false
weight: 21
---

### Aggregate Functions

* `COUNT(array)` - Returns the number of elements in `array`

* `MIN(array)` - Returns the lowest value in `array`

* `MAX(array)` - Returns the highest value in `array`

* `FIRST(array)` - Returns the first element in `array`

* `LAST(array)` - Returns the last element in `array`

* `AVG(array)` - Returns the average value of the numerical values in `array` (ignores non-numerical values)

* `SUM(array)` - Returns the sum of the numerical values in `array` (ignores non-numerical values)

* `ANY(array)` - Returns `true` if `array` has any elements


### DataType Functions

* `MINVALUE()` - Returns the singleton instance of `MinValue`

* `MAXVALUE()` - Returns the singleton instance of `MaxValue`

* `OBJECTID()` - Returns a new instance of `ObjectId`

* `GUID()` - Returns a new instance of `Guid`

* `NOW()` - Returns the current timestamp in local time

* `NOW_UTC()` - Returns the current timestamp in UTC

* `TODAY()` - Returns the current date at 00h00min00s

* `INT32(value)` - Returns `value` converted to `Int32`, or `null` if not possible

* `INT64(value)` - Returns `value` converted to `Int64`, or `null` if not possible

* `DOUBLE(value, culture)` - Returns `value` converted to `Double` according to the specified culture, or `null` if not possible

* `DECIMAL(value, culture)` - Returns `value` converted to `Decimal` according to the specified culture, or `null` if not possible

* `STRING(value)` - Returns the string representation of `value`

* `BINARY(value)` - Returns `value` converted to `BsonBinary`, or `null` if not possible 

* `OBJECTID(value)` - Returns `value` converted to `ObjectId`, or `null` if not possible 

* `GUID(value)` - Returns `value` converted to `Guid`, or `null` if not possible 

* `BOOLEAN(value)` - Returns `value` converted to `Boolean`, or `null` if not possible 

* `DATETIME(value, culture)` - Returns `value` converted to `DateTime` in local time according to the specified culture, or `null` if not possible

* `DATETIME_UTC(value, culture)` - Returns `value` converted to `DateTime` in UTC according to the specified culture, or `null` if not possible

* `DATETIME(year, month, day)` - Returns a new `DateTime` at 00h00min00s in local time based on the provided `year`, `month` and `day`

* `DATETIME_UTC(year, month, day)` - Returns a new `DateTime` at 00h00min00s in UTC based on the provided `year`, `month` and `day`

* `IS_MINVALUE(value)` - Returns `true` if `value` is `MinValue`, `false` otherwise

* `IS_MAXVALUE(value)` - Returns `true` if `value` is `MaxValue`, `false` otherwise

* `IS_NULL(value)` - Returns `true` if `value` is `null`, `false` otherwise

* `IS_INT32(value)` - Returns `true` if `value` is `Int32`, `false` otherwise

* `IS_INT64(value)` - Returns `true` if `value` is `Int64`, `false` otherwise

* `IS_DOUBLE(value)` - Returns `true` if `value` is `Double`, `false` otherwise

* `IS_DECIMAL(value)` - Returns `true` if `value` is `Decimal`, `false` otherwise

* `IS_NUMBER(value)` - Returns `true` if `value` is of a numerical type, `false` otherwise

* `IS_STRING(value)` - Returns `true` if `value` is `String`, `false` otherwise

* `IS_DOCUMENT(value)` - Returns `true` if `value` is `BsonDocument`, `false` otherwise

* `IS_arrayAY(value)` - Returns `true` if `value` is `Bsonarrayay`, `false` otherwise

* `IS_BINARY(value)` - Returns `true` if `value` is `BsonBinary`, `false` otherwise

* `IS_OBJECTID(value)` - Returns `true` if `value` is `ObjectId`, `false` otherwise

* `IS_GUID(value)` - Returns `true` if `value` is `Guid`, `false` otherwise

* `IS_BOOLEAN(value)` - Returns `true` if `value` is `Boolean`, `false` otherwise

* `IS_DATETIME(value)` - Returns `true` if `value` is `DateTime`, `false` otherwise


### Date Functions

* `YEAR(value)` - Returns the year of `value`, or `null` if it is not a `DateTime`

* `MONTH(value)` - Returns the month of `value`, or `null` if it is not a `DateTime`

* `DAY(value)` - Returns the day of `value`, or `null` if it is not a `DateTime`

* `HOUR(value)` - Returns the hour of `value`, or `null` if it is not a `DateTime`

* `MINUTE(value)` - Returns the minutes of `value`, or `null` if it is not a `DateTime`

* `SECOND(value)` - Returns the seconds of `value`, or `null` if it is not a `DateTime`

* `DATEADD(dateInterval, amount, value)`
	- `dateInterval` is one of the following: `y|year`, `M|month`, `d|day`, `h|hour`, `m|minute`, `s|second`
	- `amount` is the amount of units defined by `dateInterval` to be added to `value`

* DATEDIFF(dateInterval, start, end)
	- `dateInterval` is one of the following: `y|year`, `M|month`, `d|day`, `h|hour`, `m|minute`, `s|second`
	- `start` and `end` are dates
	- The function returns the difference between the dates in units defines by `dateInterval`

* TO_LOCAL(date) - Returns `date` converted to local time, or `null` if is not a `DateTime`

* TO_UTC(date) - Returns `date` converted to UTC, or `null` if is not a `DateTime`


### Math Functions

* `ABS(value)` - Returns the absolute value of `value`, or `null` if it is not a numerical value

* `ROUND(value, digits)` - Returns `value` rounded to `digits` of precision, or `null` if it is not a numerical value

* `POW(x, y)` - Returns `x` to the power of `y` (always as a `Double`), or `null` if either of them is not a numerical value



### String Functions

* `LOWER(value)` - Returns `value` in lower case, or `null` if it is not a `String`

* `UPPER(value)` - Returns `value` in upper case, or `null` if it is not a `String`

* `LTRIM(value)` - Returns a new string with leading whitespaces removed, or `null` if it is not a `String`

* `RTRIM(value)` - Returns a new string with trailing whitespaces removed, or `null` if it is not a `String`

* `TRIM(value)` - Returns a new string with leading and trailing whitespaces removed, or `null` if it is not a `String`

* `INDEXOF(value, match)` - Returns the zero-based index of the first occurrence of `match` in `value`

* `INDEXOF(value, match, startIndex)`- Returns the zero-based index of the first occurrence of `match` in `value`. The search starts in `startIndex`

* `SUBSTRING(value, startIndex)` - Returns the substring of `value` from `startIndex` to the end

* `SUBSTRING(value, startIndex, length)` - Returns the substring of `value` starting from `startIndex` and with length specified by `length`

* `SUBSTRING(value, oldValue, newValue)` - Returns a new string with occurrences of `oldValue` replaced by `newValue`

* `LPAD(value, totalWidth, paddingChar)` - Returns a new string left-padded to `totalWidth` length with `paddingChar`

* `RPAD(value, totalWidth, paddingChar)` - Returns a new string right-padded to `totalWidth` length with `paddingChar`

* `SPLIT(value, separator)` - Returns an arrayay containing the substrings of `value` split by `separator`

* `FORMAT(value, format)` - Returns the string representation of `value` with the provided format

* `JOIN(array)` - Takes an array of string and returns those strings joined by `,`

* `JOIN(array, separator)` - Takes an array of string and returns those strings joined by `separator`


### Misc Functions

* `JSON(value)` - Takes a string representation of a JSON and returns a parsed `BsonValue`

* `EXTEND(source, extend)` - Merges two documents into one, copying their attributes.

* `CONCAT(array1, array2)` - Returns a new array containt the concatenation of `array1` and `array2`

* `KEYS(document)` - Returns an array containing every key in `document`

* `OID_CREATIONTIME(objectId)` - Returns the creation time of `objectId`

* `IIF(predicate, ifTrue, ifFalse)` - Returns `ifTrue` if `predicate` evaluates to `true`, `false` otherwise

* `COALESCE(left, right)` - Returns `left` if it is not `null`, `right` otherwise

* `LENGTH(value)` - Returns the lenght of `value` (if value is `String`, `Binary`, `Array` or `Document`)

* `TOP(values, num)` - Returns the first `num` elements from `values`

* `UNION(array1, array2)` - Returns the set union between `array1` and `array2`

* `EXCEPT(array1, array2)` - Returns the set difference between `array1` and `array2`

* `DISTINCT(array)` - Returns the distinct elements from `array`

* `RANDOM()` - Returns a random `Int32`

* `RANDOM(min, max)` - Returns a random `Int32` between `min` and `max`