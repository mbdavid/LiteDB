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

            dbv.Apply(this, _engine.Value, _log);
        }

        /// <summary>
        /// Override this to register your database changes in schemas. Use DbVersion to Register all versions updates needs
        /// </summary>
        protected virtual void OnDbVersionUpdate(DbVersion ver)
        {
        }
    }

    /// <summary>
    /// Represents all versions tasks to upgrade a database after schema changes and a database are out of date. When open a database, if internal version are lower than all registered updates, run each task to update database.
    /// </summary>
    public class DbVersion
    {
        internal SortedDictionary<ushort, Action<LiteDatabase>> _versions = new SortedDictionary<ushort, Action<LiteDatabase>>();

        /// <summary>
        /// Register a new database version tasks to update schema when database is out of date.
        /// </summary>
        /// <param name="version">Number version. Must starts in 1 and steps in 1</param>
        /// <param name="action">An action method that will be executed when database is out of date.</param>
        public void Register(ushort version, Action<LiteDatabase> action)
        {
            if (version < 1) throw new ArgumentException("version must be equals or greater than 1");
            if (_versions.ContainsKey(version)) throw new ArgumentException("version " + version + " already exists");

            _versions.Add(version, action);
        }

        /// <summary>
        /// Loop in all registered versions and apply all that needs. Update dbversion
        /// </summary>
        internal void Apply(LiteDatabase db, DbEngine engine, Logger log)
        {
            if (_versions.Count == 0) return;

            var dbparams = engine.GetDbParam();
            var updated = false;

            // apply all action version updates
            foreach(var version in _versions.Where(x => x.Key > dbparams.DbVersion))
            {
                log.Write(Logger.COMMAND, "update database version to {0}", version.Key);
                version.Value(db);
                dbparams.DbVersion = version.Key;
                updated = true;
            }

            if(updated)
            {
                engine.SetParam(dbparams);
            }
        }
    }
}
