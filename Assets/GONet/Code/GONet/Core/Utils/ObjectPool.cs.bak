/* GONet (TM, serial number 88592370), Copyright (c) 2019-2023 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@galoreinteractive.com
 * 
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 */

using System;
using System.Collections.Generic;

namespace GONet.Utils
{
    public interface IObjectPool<T>
    {
        T Borrow();

        void Return(T @object);
    }

    public class ObjectPool<T> : ObjectPoolBase<T> where T : class, new()
    {
        /// <summary>
        /// This class will keep a reference to every available and borrowed object at all times
        /// in order to perform if-borrowed checks upon return.
        /// </summary>
        /// <param name="objectInitializer">
        /// if provided, this is called just before serving up an object from BorrowObject to init it.
        /// </param>
        public ObjectPool(int initialSize, int growByCount, Action<T> objectInitializer = null)
            : base(initialSize, growByCount, objectInitializer) { }

        protected override T CreateSingleInstance()
        {
            return new T();
        }
    }

    /// <summary>
    /// A non-Threadsafe Object pool.
    /// Well, there are synchronized forms of borrow and return that uses basic object locking.
    /// </summary>
    public abstract class ObjectPoolBase<T> : IObjectPool<T> where T : class
    {
        static readonly CastToObjectEqualityOperatorComparer<T> equalityComparer = new CastToObjectEqualityOperatorComparer<T>();
        /// <summary>
        /// Helps speed up returns, by a big amount if large enough amount of items, which is the ONLY reason I allow storing extra data.
        /// </summary>
        protected readonly Dictionary<T, int> indexByCheckedOutObjectMap;

        /// <summary>
        /// The pooled objects, both available and borrowed (and unused slots also)
        /// </summary>
        protected T[] pool;
        
        protected int nextAvailableIndex;

        public int BorrowedCount => nextAvailableIndex;
        
        /// <summary>
        /// The number of object instances the pool can hold.
        /// </summary>
        protected int poolCapacity;

        public int Capacity => poolCapacity;

        /// <summary>
        /// the number of instances to grow the pool by when there are no more available
        /// </summary>
        protected int growByCount;

        Action<T> objectInitializer;

        public delegate void ObjectCheckoutStatusChangedDelegate(ObjectPoolBase<T> pool, T objectInPool);
        public event ObjectCheckoutStatusChangedDelegate ObjectBorrowed;
        public event ObjectCheckoutStatusChangedDelegate ObjectReturned;
        public event ObjectCheckoutStatusChangedDelegate ObjectCreated;

        #region statistics for occassional debugging output
        int totalBorrowed;
        int totalReturned;
        #endregion

        /// <summary>
        /// This class will keep a reference to every available and borrowed object at all times
        /// in order to perform if-borrowed checks upon return.
        /// </summary>
        /// <param name="objectInitializer">
        /// if provided, this is called just before serving up an object from BorrowObject to init it.
        /// </param>
        /// <param name="onObjectCreatedHandler">If the caller needs to register to <see cref="ObjectCreated"/> prior to the very first time <see cref="CreateAdditionalInstances(int)"/> is called, pass this is as non-null</param>
        public ObjectPoolBase(int initialSize, int growByCount, Action<T> objectInitializer = null, ObjectCheckoutStatusChangedDelegate onObjectCreatedHandler = null)
        {
            indexByCheckedOutObjectMap = new Dictionary<T, int>(initialSize, equalityComparer);
            if (onObjectCreatedHandler != null)
            {
                ObjectCreated += onObjectCreatedHandler;
            }

            BaseConstruct(initialSize, growByCount, objectInitializer);
        }

        protected void BaseConstruct(int initialSize, int growByCount, Action<T> objectInitializer = null)
        {
            this.growByCount = growByCount;
            this.objectInitializer = objectInitializer;
            poolCapacity = 0;
            nextAvailableIndex = 0;
            CreateAdditionalInstances(initialSize);
        }


        //readonly HashSet<System.Threading.Thread> uniqueThreadsEncountered = new HashSet<System.Threading.Thread>();

        /// <summary>
        /// This method is to ensure the <see cref="nextAvailableIndex"/> is valid inside <see cref="pool"/> and contains teh next item to serve up for borrowing
        /// </summary>
        protected virtual void PreBorrow()
        {
            //uniqueThreadsEncountered.Add(System.Threading.Thread.CurrentThread);

            // First, try to get an instance from the available list.
            // If none there, then create some more.
            if (nextAvailableIndex == poolCapacity)
            {
                CreateAdditionalInstances(growByCount);
            }
        }

        public virtual T Borrow()
        {
            // TODO: SHAUN add in something that compares the current thread against the thread from when the constructor was invoked and either spit out a warning, throw an exception or return null....based on a configuration settting likely changed/set in constructor

            PreBorrow();

            int iObject = nextAvailableIndex;
            T @object = pool[nextAvailableIndex++];
            indexByCheckedOutObjectMap[@object] = iObject;

            objectInitializer?.Invoke(@object);

            ++totalBorrowed;

            ObjectBorrowed?.Invoke(this, @object);

            return @object;
        }

        /// <summary>
        /// A thread-safe way to borrow an object.
        /// </summary>
        public virtual T BorrowSynchronized()
        {
            lock (this)
            {
               return Borrow();
            }
        }

        /// <summary>
        /// When this returns true, the next call to <see cref="Borrow"/> will in turn force a call to <see cref="CreateAdditionalInstances(int)"/>, 
        /// ASSuming no call to <see cref="Return(T)"/>, <see cref="ReturnSynchronized(T)"/> or <see cref="ReturnAll"/> was made in between.
        /// </summary>
        public bool AreAllCurrentlyBorrowed()
        {
            return poolCapacity == nextAvailableIndex;
        }

        public int CurrentlyBorrowedCount => nextAvailableIndex;

        public virtual void Return(T @object)
        {
            // check that input is valid
            if ((object)@object == null)
            {
                throw new NullReferenceException();
            }
            
            //uniqueThreadsEncountered.Add(System.Threading.Thread.CurrentThread);

            --nextAvailableIndex; // IMPORTANT_NOTE456: until the swap actually occurs below, this value represents a checked out object that is NOT available....sorry :(

            int currentPoolIndex;
            if (!indexByCheckedOutObjectMap.TryGetValue(@object, out currentPoolIndex)) // this says the object is not currently checked out
            {
                ++nextAvailableIndex; // set it back to previous value
                throw new NotBorrowedFromPoolException(@object);
            }

            if (currentPoolIndex != nextAvailableIndex) // if false, there is nothing to swap...its going to same position
            {
                // Swap out the two objects so the pool[] always has each object (available and checked out alike),
                // so none are overwritten.  This is so the checks above for object borrowed from pool continue to work.
                PreObjectPositionSwap(currentPoolIndex, nextAvailableIndex);
                T swappie = pool[currentPoolIndex] = pool[nextAvailableIndex]; // IMPORTANT_NOTE456: NOW, nextAvailableIndex actually represents what it says....you're welcome ;-)
                // return the object to the pool, making it the next available object for borrowing
                pool[nextAvailableIndex] = @object;

                indexByCheckedOutObjectMap[swappie] = currentPoolIndex;
            }
            if (!indexByCheckedOutObjectMap.Remove(@object))
            {
                const string NOT = "Not able to remove object from pool.....not sure why.";
                UnityEngine.Debug.LogError(NOT);
            }
            ++totalReturned;

            ObjectReturned?.Invoke(this, @object);
        }

        /// <summary>
        /// A thread-safe way to return an object.
        /// </summary>
        public virtual void ReturnSynchronized(T @object)
        {
            lock (this)
            {
                Return(@object);
            }
        }

        /// <summary>
        /// Calls <see cref="Return(T)"/> for all currently checked out objects in this pool.
        /// </summary>
        /// <returns>the number returned in this call</returns>
        public int ReturnAll()
        {
            int returnedCount = 0;
            for (int i = nextAvailableIndex - 1; i >= 0; --i)
            {
                T @object = pool[i];
                Return(@object);
                ++returnedCount;
            }
            return returnedCount;
        }

        /// <summary>
        /// Provides a mechanism for child classes to perform some action
        /// before the call to <see cref="CreateAdditionalInstances(int)"/> has begun..
        /// </summary>
        protected virtual void OnGrowStart() { } // TODO consider making this an event anyone can subscribe to

        /// <summary>
        /// Provides a mechanism for child classes to perform some action
        /// after the call to <see cref="CreateAdditionalInstances(int)"/> is complete.
        /// </summary>
        protected virtual void OnGrowComplete(int previousPoolCapacity) { } // TODO consider making this an event anyone can subscribe to

        /// <summary>
        /// Called just before two objects will swap positions in the pool[]. 
        /// NOTE: Upon entry into this method, nextAvailableIndex will point to the next available AFTER the
        /// borrow is complete (assuming a call to borrow prompted this call).
        /// </summary>
        protected virtual void PreObjectPositionSwap(int poolIndex1, int poolIndex2) { }

        /// <summary>
        /// Create <count> instances (using newInstance()) of T and add them to the pool.
        /// This is the meat of what is surrounded by calls to <see cref="OnGrowStart"/> and <see cref="OnGrowComplete(int)"/>.
        /// </summary>
        protected void CreateAdditionalInstances(int count)
        {
            if (count > 0)
            {
                OnGrowStart();

                int prevPoolCapacity = poolCapacity;
                int newPoolCapacity = poolCapacity + count;

                // TODO check performance implications of each array creation options below
                T[] newObjects = new T[newPoolCapacity];

                // Create new instances
                for (int i = poolCapacity; i < newPoolCapacity; ++i)
                {
                    try
                    {
                        var newObject = CreateSingleInstance();
                        newObjects[i] = newObject;
                        ObjectCreated?.Invoke(this, newObject);
                    }
                    catch (Exception e)
                    {
                        throw new UnableToCreatePoolInstancesException(typeof(T), e);
                    }
                }
                // Add the already existent instances
                if (pool != null)
                { // first time through, this will be null, so dont do anything.
                    Array.Copy(pool, 0, newObjects, 0, poolCapacity);
                }
                // Change pointers to complete
                pool = newObjects;
                // update the overall capacity
                poolCapacity = newPoolCapacity;

                // allow custom reaction to this grow
                OnGrowComplete(prevPoolCapacity);

                //UnityEngine.Debug.Log(this.ToString()); // little debuggery for analysis to make sure pools get utilized as expected....this info hopefully provides insight into how best tune/tweak the initial values
            }
        }

        protected abstract T CreateSingleInstance();

        public override string ToString()
        {
            const string Type = "ObjectPool object type: ";
            const string Hash = ", hashCode: ";
            const string Capacity = ", capacity: ";
            const string BorrowedCurrent = ", current # borrowed: ";
            const string BorrowedTotal = ", total # borrowed: ";
            const string Returned = ", total # returned: ";
            
            //const string UniqueThreads = ", # unique threads using this instance: ";
            //string statement = string.Concat(Type, typeof(T).Name, Hash, GetHashCode(), Capacity, pool.Length, BorrowedCurrent, nextAvailableIndex, BorrowedTotal, totalBorrowed, Returned, totalReturned, UniqueThreads, uniqueThreadsEncountered.Count);
            
            string statement = string.Concat(Type, typeof(T).Name, Hash, GetHashCode(), Capacity, pool.Length, BorrowedCurrent, nextAvailableIndex, BorrowedTotal, totalBorrowed, Returned, totalReturned);
            return statement;
        }
    }

    public class NotBorrowedFromPoolException : Exception
    {
        private const string MSG = "Object being returned to the pool was never borrowed from the pool, object[{0}].";
        public NotBorrowedFromPoolException(object @object)
            : base(string.Format(MSG, @object != null ? @object.ToString() : null)) { }
    }

    public class UnableToCreatePoolInstancesException : Exception
    {
        private const string MSG = "ObjectPool cannot instantiate instances of the template type, templateType[{0}].";
        public UnableToCreatePoolInstancesException(Type templateType, Exception cause)
            : base(string.Format(MSG, templateType != null ? templateType.FullName : null), cause) { }
    }
}
