using LiteDB.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class LiteDatabase : IDisposable
    {
        private void InitializeDbVersion()
        {
            var dbv = new DbVersion();

            this.OnDbVersionUpdate(dbv);

            dbv.Apply(this, _engine.Value);
        }

        /// <summary>
        /// Override this to register your database changes in schemas. Use DbVersion to Register all versions updates needs
        /// </summary>
        protected virtual void OnDbVersionUpdate(DbVersion dv)
        {
        }
    }

    /// <summary>
    /// Represents all versions tasks updates when database is out of date
    /// </summary>
    public class DbVersion
    {
        internal SortedDictionary<ushort, Action<LiteDatabase>> _versions = new SortedDictionary<ushort, Action<LiteDatabase>>();

        /// <summary>
        /// Register a new version update tasks when database is out of date.
        /// </summary>
        /// <param name="version">Number version. Must starts in 1 and steps in 1</param>
        /// <param name="action">An action method that will be executed when database is out of date</param>
        public void Register(ushort version, Action<LiteDatabase> action)
        {
            if (version < 1) throw new ArgumentException("version must be equals or greater than 1");
            if (_versions.ContainsKey(version)) throw new ArgumentException("version " + version + " already exists");

            _versions.Add(version, action);
        }

        /// <summary>
        /// Loop in all registered versions and apply all that needs. Update dbversion
        /// </summary>
        internal void Apply(LiteDatabase db, DbEngine engine)
        {
            if (_versions.Count == 0) return;

            var dbparams = engine.GetDbParams();
            var updated = false;

            // apply all action version updates
            foreach(var version in _versions.Where(x => x.Key > dbparams.DbVersion))
            {
                version.Value(db);
                dbparams.DbVersion = version.Key;
                updated = true;
            }

            if(updated)
            {
                engine.SetDbParams(dbparams);
            }
        }
    }
}
