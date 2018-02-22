using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class LiteEngine
    {
        public delegate void UpdateHandler(object sender, string collectionName);

        private Dictionary<string,List<UpdateHandler>> _delegates = new Dictionary<string, List<UpdateHandler>>();
        /// <summary>
        /// Add a notification for a specific collection
        /// </summary>
        /// <param name="collectionName">Name of the collection</param>
        /// <param name="handler">Delegate handler to call on Update/Insert</param>
        public void RegisterNotification(string collectionName, UpdateHandler handler)
        {

            if (!_delegates.ContainsKey(collectionName))
            {
                _delegates.Add(collectionName, new List<UpdateHandler>());
            }
            if (handler != null)
            {
                _delegates[collectionName].Add(handler);
            }
        }

        /// <summary>
        /// De register all notifications
        /// </summary>
        public void DeRegisterAllNotifications()
        {
            _delegates = new Dictionary<string, List<UpdateHandler>>();
        }

        /// <summary>
        /// De register all notifications for a collection
        /// </summary>
        /// <param name="collectionName">Name of the collection</param>
        public void DeRegisterNotifications(string collectionName)
        {
            if (_delegates.ContainsKey(collectionName))
            {
                _delegates.Remove(collectionName);
            }
        }

        /// <summary>
        /// De register a specific call notification for collection
        /// </summary>
        /// <param name="collectionName"></param>
        /// <param name="handler"></param>
        public void DeRegisterNotification(string collectionName, UpdateHandler handler)
        {

            if (_delegates.ContainsKey(collectionName))
            {
                //Make a copy so we can update the main list
                var handlers = new List<UpdateHandler>(_delegates[collectionName]);
                foreach (UpdateHandler lphandler in handlers)
                {
                    if(lphandler == handler)
                    {
                        _delegates[collectionName].Remove(lphandler);
                    }
                }
                //If we don't have any remaining for this collection, remove the collection from the dictionary
                if (_delegates[collectionName].Count == 0)
                {
                    _delegates.Remove(collectionName);
                }
                
            }
            if (handler != null)
            {
                _delegates[collectionName].Add(handler);
            }
        }

        /// <summary>
        /// Call any notification handlers registered for this collection
        /// </summary>
        /// <param name="collectionName">Name of the collection to check</param>
        private void CheckNotification(string collectionName)
        {
            if (_delegates != null && _delegates.ContainsKey(collectionName))
            {
                foreach (UpdateHandler handler in _delegates[collectionName])
                {
                    handler(this, collectionName);
                }
            }
        }
    }
}
