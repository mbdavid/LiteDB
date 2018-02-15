using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Run an include action in each document returned by Find(), FindById(), FindOne() and All() methods to load DbRef documents
        /// Returns a new Collection with this action included
        /// </summary>
        public LiteCollection<T> Include<K>(Expression<Func<T, K>> path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var value = _visitor.GetPath(path);

            return this.Include(value);
        }

        /// <summary>
        /// Run an include action in each document returned by Find(), FindById(), FindOne() and All() methods to load DbRef documents
        /// Returns a new Collection with this action included
        /// </summary>
        public LiteCollection<T> Include(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            return Include( new string[] { path } );
        }

        public LiteCollection<T> Include(string[] paths)
        {
            if(paths == null)
            {
                throw new ArgumentNullException( nameof( paths ) );
            }

            // cloning this collection and adding this include
            var newcol = new LiteCollection<T>( _name, _engine, _mapper, _log );

            newcol._includes.AddRange( _includes );

            //
            // Add all paths that are not null nor empty due to previous check
            //
            newcol._includes.AddRange( paths.Where( query => !String.IsNullOrEmpty( query ) ) );

            return newcol;
        }


        public LiteCollection<T> IncludeAll()
        {
            //
            // Get the `EntityMapper` of `T` to get all classes in the database
            //
            IEnumerable<MemberMapper> memberMappers = _mapper.GetEntityMapper( typeof( T ) )
                .Members
                .Where( query => query.IsDbRef );

            List<string> fields = new List<string>();

            //
            // Cycle through each `MemberMapper` and get the field name,
            //   simulating `LiteCollection<T> Include<K>( Expression<Func<T, K>> path )`.
            //
            foreach( MemberMapper memberMapper in memberMappers )
            {
                bool isdbref = false;

                string field = _visitor.GetField( memberMapper, true, ref isdbref );

                fields.Add( $"$.{field}" );
            }

            //
            // Include the fields
            //
            return Include( fields.ToArray() );
        }
    }
}