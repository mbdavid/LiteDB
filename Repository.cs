// Dream Code for a Generic Repository Pattern, inspired in NPoco
namespace LiteDB
{
    public interface IRepository : IDisposable
    {
		T Insert<T>(T entity)
		int Insert<T>(IEnumerable<T> entities)
		
		T Update<T>(T entity)
		int Update<T>(IEnumerable<T> entities)
		
		bool Delete<T>(T entity)
		bool Delete<T>(params object[] keys);
		
		T SingleById<T>(params object[] keys);
		
		T First<T>(Expression<Func<T, bool>> predicate);
		T FirstOrDefault<T>(Expression<Func<T, bool>> predicate);
		T Single<T>(Expression<Func<T, bool>> predicate);
		T SingleOrDefault<T>(Expression<Func<T, bool>> predicate);
		
		int Count<T>(Expression<Func<T, bool>> predicate);
		bool Exists<T>(Expression<Func<T, bool>> predicate);		
		
		IQuery Query<T>(params Expression<Func<T, object>> includes);
		
		ITransaction GetTransaction();
    }
	
	public interface IQuery<T>()
	{
		IQuery<T> Where(Expression<Func<T, bool>> predicate)
		
		T First<T>(Expression<Func<T, bool>> predicate);
		T FirstOrDefault<T>(Expression<Func<T, bool>> predicate);
		T Single<T>(Expression<Func<T, bool>> predicate);
		T SingleOrDefault<T>(Expression<Func<T, bool>> predicate);
		
		// order by?
		// paged?
		
		IEnumerable<T> ToEnumerable<T>();
		T[] ToArray<T>();
		List<T> ToList<T>();
	}
	
	public interface ITransaction : IDisposable
	{
		void Complete();
	}
	
	/// impl
	
	public class LiteRepository : IRepository
	{
		public LiteRepository(string connectionString)
		public LiteRepository(LiteDatabase db)
		...
	}
	
	/// using
	using(var db = new LiteRepository("C:\temp\mydb.db"))
	{
		db.Insert<Customer>(new Customer { Name = "John" });
		
		var c = db.Query<Customer>()
			.Include(x => x.Orders)
			.Where(x => x.Name.StartWith("John"))
			.ToList();
			
		using(var t = db.GetTransaction())
		{
			db.Insert<Customer>(listOfCustomers);
		}
			
	}
	
}