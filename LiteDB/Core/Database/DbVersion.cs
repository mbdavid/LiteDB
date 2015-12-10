using System;

namespace LiteDB
{
    public partial class LiteDatabase : IDisposable
    {
        /// <summary>
        /// Virtual method for update database when a new version (from coneection string) was setted
        /// </summary>
        protected virtual void OnVersionUpdate(int newVersion)
        {
        }

        /// <summary>
        /// Loop in all registered versions and apply all that needs. Update dbversion
        /// </summary>
        private void UpdateDbVersion(ushort recent)
        {
            var dbparams = _engine.Value.GetDbParam();
            this.Version = dbparams.DbVersion;

            for (var newVersion = this.Version + 1; newVersion <= recent; newVersion++)
            {
                _log.Write(Logger.COMMAND, "update database version to {0}", newVersion);

                this.OnVersionUpdate(newVersion);

                this.Version = dbparams.DbVersion = (ushort)newVersion;
                _engine.Value.SetParam(dbparams);
            }
        }
    }
}