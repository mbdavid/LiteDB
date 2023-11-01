//var a = true;
//var x = false;
//var n = 10;

//var rr = a == true ? x == a ? 'r' : a == false ? 'f' : a == false && a == true ? 'x' + 'y' : 'z' : 'd';

//var ss = new string[]
//{
//    "a = true ? 's' : 'f'",
//    "a = true ? 't' : a = false ? 'f' : 'xx'",
//    "a = true ? a != false ? 'vv' : 'f' : 'ff'",
//    "a = true ? 's' : a = false ? 'f' : 'nenhum'",
//    "$.a=true?$.x=$.a?'r':$.a=false?'f':$.a=false AND $.a=true?'x'+'y':'z':'d'",
//    "(a = true) ? (x = a) ? 'r' : (a = false) ? 'f' : (a = false and a = true) ? 'x' + 'y' : 'z' : 'd'",
//    "(a = true) ? (x = a) ? (x = a) ? 'r' : (a = false) ? 'f' : (a = false and a = true) ? 'x' + 'y' : 'z' : 'd' : 'p'",
//    "n = 1 ? '1' : n = 2 ? '2' : n = 3 ? '3' : n = 4 ? '4' : n = 5 ? '5' : n = 6 ? '6' : 'outro'",
//    "'teste' + a = true ? 's' : 'f'",
//    "'teste' + (a = true ? 's' : 'f')",
//};


////Console.WriteLine(x.ToString());

//foreach (var s in ss)
//{
//    var doc = new BsonDocument { ["a"] = true };
//    var exp = BsonExpression.Create(s);

//    Console.WriteLine("Str : " + s);
//    Console.WriteLine("Expr: " + exp.ToString());
//    Console.WriteLine("Exec: " + exp.Execute(doc).ToString());
//    Console.WriteLine("----------------------------------------------");
//}


//Console.ReadKey();

//return;