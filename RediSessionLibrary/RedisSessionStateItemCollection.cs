using BookSleeve;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Web.SessionState;


namespace RediSessionLibrary
{
    /// <summary>
    /// Controls access to Session items for a request
    /// </summary>
    public sealed class RedisSessionStateItemCollection : NameObjectCollectionBase, ISessionStateItemCollection
    {
        /// <summary>
        /// The structure that holds data regarding a particular item in Session
        /// </summary>
        internal class SessionItemSuperSet
        {
            public bool bDirty;		// Is the item dirty
            public bool bRemoved;	// has the item been removed (or nullified)
            public bool bAdded;		// has the item been added
            public object data;		// pointer to the item
            public byte[] orgData;	// contains serialized original data
        }

        #region private variables
        private string sessionID;					// the session ID
        private string sessionKey;					// contains the redis session key
        private bool bCleared;						// flag to determine if this session has been cleared (during the request)
        private bool bDirty;						// flag to determie if this session is dirty (needs persisting)
        private int timeoutMinutes;					// seesion timeout in minutes
        private RedisSessionSerializer serializer;	// Redis object serializer
        private ConcurrentDictionary<string, SessionItemSuperSet> items;  // container for the items
        private RedisConnection redisConnection;	// containts the connection to redis
        private Task<Dictionary<string, byte[]>> readTask;	// contains the initial read task
        private ConcurrentBag<Task> saveTasks;		// maintains a list of all object set commands
        #endregion

        /// <summary>
        /// Retrieves the Uhnique Session Key for the User
        /// </summary>
        private string SessionKey
        {
            get { return sessionKey; }
        }

        /// <summary>
        /// Creates an interface to access and set Session Items to a Redis NoSql Data store
        /// </summary>
        /// <param name="sessionId">the unique session id for the request</param>
        /// <param name="conn">Active and opened conection to the Redis Server</param>
        public RedisSessionStateItemCollection(string sessionId, RedisConnection conn, int timeoutInMinutes)
            : base()
        {
            items = new ConcurrentDictionary<string, SessionItemSuperSet>();
            saveTasks = new ConcurrentBag<Task>();
            serializer = new RedisSessionSerializer();
            bCleared = false;
            sessionID = sessionId;
            sessionKey = "SID:" + sessionID;
            redisConnection = conn;
            timeoutMinutes = timeoutInMinutes;
            // issue a read command (async)
            readTask = redisConnection.Hashes.GetAll(0, this.SessionKey); // issue the read command
        }

        /// <summary>
        /// Completes the reading task
        /// </summary>
        private void FinishRead()
        {
            if (readTask == null)
                return; // nothing to do

            readTask.Wait();
            Dictionary<string, byte[]> pSessItems = readTask.Result;
            readTask = null;

            if (pSessItems != null)
            {
                for (int i = 0; i < pSessItems.Count; i++)
                {
                    KeyValuePair<string, byte[]> item = pSessItems.ElementAt(i);

                    object oItem = serializer.Deserialize(item.Value);
                    items[item.Key] = new SessionItemSuperSet()
                    {
                        bDirty = false,
                        bAdded = false,
                        bRemoved = false,
                        data = oItem,
                        orgData = item.Value
                    };
                    BaseAdd(item.Key, oItem);
                }
            }
            // we are in a fresh state
            this.Dirty = this.bCleared = false;
        }

        /// <summary>
        /// Retrives an item from Session
        /// </summary>
        /// <param name="key">the unique key</param>
        /// <returns>null if the object does not exist, otherwise the object itself</returns>
        public object Get(string key)
        {
            // verify we have finished reading
            if (readTask != null)
                FinishRead();
            SessionItemSuperSet pItem = null;
            if (items.TryGetValue(key, out pItem))
                return pItem == null || pItem.bRemoved ? null : pItem.data;
            return null;
        }

        /// <summary>
        /// Stores an item in Session
        /// </summary>
        /// <param name="key">the unique key</param>
        /// <param name="value">null to clear it, otherwise the object itself</param>
        public void Set(string key, object value)
        {
            // verify we have finished reading
            if (readTask != null)
                FinishRead();

            SessionItemSuperSet pItem = null;
            if (items.TryGetValue(key, out pItem))
            {
                pItem.data = value;
                BaseSet(key, value);
                this.Dirty = true;
                pItem.bDirty = true;
            }
            else
            {
                pItem = new SessionItemSuperSet()
                {
                    bDirty = true,
                    bAdded = true,
                    data = value
                };
                items[key] = pItem;
                BaseAdd(key, value);
                this.Dirty = true;
            }
            pItem.bRemoved = (value == null);

            // now send an update to this item
            SendUpdate(key, pItem);
        }

        /// <summary>
        /// Sends an update to the session server
        /// </summary>
        /// <param name="name">the key of the item</param>
        /// <param name="pItem">The structure that defines this element</param>
        private void SendUpdate(string name, SessionItemSuperSet pItem)
        {
            if (!pItem.bRemoved)
            {
                byte[] rawData = serializer.Serialize(pItem.data);
                if (pItem.bAdded || pItem.orgData == null || !pItem.orgData.SequenceEqual(rawData))
                {
                    // send a request to update the data stores for this hash key
                    Dictionary<string, byte[]> sets = new Dictionary<string, byte[]>();
                    sets.Add(name, serializer.Serialize(pItem.data));
                    saveTasks.Add(redisConnection.Hashes.Set(0, this.SessionKey, sets));
                    pItem.orgData = rawData;
                }
            }
            else
            {
                // send a request to remove this hash key
                saveTasks.Add(redisConnection.Hashes.Remove(0, this.SessionKey, name));
            }
            pItem.bDirty = pItem.bAdded = pItem.bRemoved = false; // no longer dirty, added or removed
        }

        /// <summary>
        /// Retrieves all the tasks that need to be waited on to finish this request.
        /// </summary>
        /// <returns>null if there are no tasks, otherise a list of tasks</returns>
        public Task[] GetAllAsyncWriteTasks()
        {
            // this is called when the request is ending...
            if (ItemsNeedPersisting())
            {
                if (bCleared) // if this session is cleared
                {
                    // remove all elements by clearing the keys
                    saveTasks.Add(redisConnection.Keys.Remove(0, this.SessionKey));
                }
                else
                {
                    Dictionary<string, byte[]> sets = new Dictionary<string, byte[]>();
                    IEnumerator<KeyValuePair<string, SessionItemSuperSet>> iter = items.GetEnumerator();

                    while (iter.MoveNext())
                    {
                        if (DoesItemNeedPersisting(iter.Current.Value))
                        {
                            if (!iter.Current.Value.bRemoved)
                                sets.Add(iter.Current.Key, serializer.Serialize(iter.Current.Value.data));
                            else
                                saveTasks.Add(redisConnection.Hashes.Remove(0, this.SessionKey, iter.Current.Key));
                        }
                    }
                    if (sets.Count > 0)
                        saveTasks.Add(redisConnection.Hashes.Set(0, this.SessionKey, sets));
                }
            }

            // finally add an expiration timeout on the keys
            saveTasks.Add(redisConnection.Keys.Expire(0, this.SessionKey, timeoutMinutes * 60));

            // return all awaitable tasks
            return saveTasks.ToArray();
        }

        /// <summary>
        /// Determines if aything needs persisting
        /// </summary>
        /// <returns>true if so, false otherwise</returns>
        private bool ItemsNeedPersisting()
        {
            if (bCleared)
                return true;

            IEnumerator<KeyValuePair<string, SessionItemSuperSet>> iter = items.GetEnumerator();
            bool bDirtyified = false;
            while (iter.MoveNext())
            {
                if (DoesItemNeedPersisting(iter.Current.Value))
                    iter.Current.Value.bDirty = bDirtyified = true;
            }
            return bDirtyified;
        }

        /// <summary>
        /// Determines if an item needs persisting by comparing associated flags or the serialzed content.
        /// This is done on request session end to verify child objects are persisted it they are altered during the query
        /// </summary>
        /// <param name="pSet">The <seealso cref="SessionItemSuperSet"/></param>
        /// <returns>true if the item should be persisted</returns>
        private bool DoesItemNeedPersisting(SessionItemSuperSet pSet)
        {
            //return true;
            // no need to remove items that were not there to begin with (added and removed) cuts down on chatter
            if (pSet.bDirty || (pSet.bAdded && !pSet.bRemoved) || (!pSet.bAdded && pSet.bRemoved))
                return true;

            if (pSet.data != null && pSet.orgData != null)
            {
                byte[] rawData = serializer.Serialize(pSet.data);
                if (!pSet.orgData.SequenceEqual(rawData)) // if the serialized object is different, persist
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Not impleneted
        /// </summary>
        /// <param name="c">ignored</param>
        /// <param name="index">ignored</param>
        public void CopyTo(System.Array c, int index)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// errata
        /// </summary>
        public Object SyncRoot
        {
            get { return this; }
        }

        /// <summary>
        /// Retrieves the count of the current collection
        /// </summary>
        public override int Count
        {
            get
            {
                // verify we have finished reading
                if (readTask != null)
                    FinishRead();

                return base.Count;
            }
        }


        /// <summary>
        /// Retrieves an iterator of the Keys.
        /// </summary>
        public override NameObjectCollectionBase.KeysCollection Keys
        {
            get
            {
                // verify we have finished reading
                if (readTask != null)
                    FinishRead();
                // create a copied collection (so that it's thread safe)
                var copiedCollection = new NameValueCollection();
                foreach (var key in BaseGetAllKeys())
                    copiedCollection.Add(key, null);

                return copiedCollection.Keys;
            }
        }

        /// <summary>
        /// Returns an iterator 
        /// </summary>
        /// <returns></returns>
        public override IEnumerator GetEnumerator()
        {
            // verify we have finished reading
            if (readTask != null)
                FinishRead();

            return base.GetEnumerator();
        }

        /// <summary>
        /// Force non blocking access
        /// </summary>
        public bool IsSynchronized
        {
            get { return true; }
        }

        #region ISessionStateItemCollection functions implemented
        /// <summary>
        /// Clears all entries in the current session
        /// </summary>
        public void Clear()
        {
            // verify we have finished reading
            if (readTask != null)
                FinishRead();

            bCleared = true;
            items.Clear();
            BaseClear();
        }

        /// <summary>
        /// deterines if the current session is dirty
        /// </summary>
        public bool Dirty
        {
            get { return (bDirty | bCleared); }
            set { bDirty = value; }
        }

        /// <summary>
        /// Removes an item in the Hash
        /// </summary>
        /// <param name="name"></param>
        public void Remove(string name)
        {
            // verify we have finished reading
            if (readTask != null)
                FinishRead();

            if (items.ContainsKey(name))
            {
                SessionItemSuperSet pItem = items[name];
                pItem.bRemoved = true;
                BaseRemove(name);
            }
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="index">ignored</param>
        public void RemoveAt(int index)
        {
            throw new NotImplementedException(); // not allowed!
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="index"></param>
        /// <returns>null</returns>
        public object this[int index]
        {
            get { throw new NotImplementedException(); } // not allowed!
            set { throw new NotImplementedException(); } // not allowed!
        }

        /// <summary>
        /// Retrieves the current item that is within the key
        /// </summary>
        /// <param name="name">the unique key</param>
        /// <returns>null, if nothing is there</returns>
        public object this[string name]
        {
            get { return Get(name); }
            set { Set(name, value); }
        }
        #endregion
    }
}
