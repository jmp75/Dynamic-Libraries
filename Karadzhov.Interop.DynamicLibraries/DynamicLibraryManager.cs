﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace Karadzhov.Interop.DynamicLibraries
{
    public sealed class DynamicLibraryManager
    {
        #region Static

        private static DynamicLibraryManager Instance
        {
            get
            {
                return DynamicLibraryManager.instance.Value;
            }
        }

        /// <summary>
        /// Frees all loaded libraries.
        /// </summary>
        public static void Reset()
        {
            if (false == DynamicLibraryManager.instance.IsValueCreated)
                return;

            DynamicLibraryManager.Instance.InstanceReset();
        }

        /// <summary>
        /// Frees a specific library.
        /// </summary>
        /// <param name="library">The library.</param>
        /// <remarks>Throws exception if the library is not loaded. Use overload with additional boolean for different behavior.</remarks>
        public static void Reset(string library)
        {
            DynamicLibraryManager.Reset(library, throwIfNotFound: true);
        }

        /// <summary>
        /// Frees a specific library.
        /// </summary>
        /// <param name="library">The library.</param>
        /// <param name="throwIfNotFound">If true the method throws exception if the library is not loaded otherwise ignores the call.</param>
        public static void Reset(string library, bool throwIfNotFound)
        {
            if (DynamicLibraryManager.instance.IsValueCreated)
            {
                DynamicLibraryManager.Instance.InstanceReset(library, throwIfNotFound);
            }
            else if (throwIfNotFound)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Library '{0}' cannot be reset because it was not loaded in the first place.", library));
            }
        }

        /// <summary>
        /// Invokes the specified method from the given library.
        /// </summary>
        /// <typeparam name="TReturn">The return type of the method.</typeparam>
        /// <param name="library">The path to the library. Can be full path to a DLL/EXE or relative to the current directory.</param>
        /// <param name="method">The method name.</param>
        /// <param name="arguments">The arguments that will be passed to the method.</param>
        /// <returns>
        /// The result of the method.
        /// </returns>
        public static TReturn Invoke<TReturn>(string library, string method, params object[] arguments)
        {
            return (TReturn)DynamicLibraryManager.Invoke(library, method, typeof(TReturn), arguments);
        }

        /// <summary>
        /// Invokes the specified method from the given library.
        /// </summary>
        /// <param name="library">The path to the library. Can be full path to a DLL/EXE or relative to the current directory.</param>
        /// <param name="method">The method name.</param>
        /// <param name="returnType">The return type of the method.</param>
        /// <param name="arguments">The arguments that will be passed to the method.</param>
        /// <returns>
        /// The result of the method.
        /// </returns>
        public static object Invoke(string library, string method, Type returnType, params object[] arguments)
        {
            return DynamicLibraryManager.Instance.InstanceInvoke(library, method, returnType, arguments);
        }

        private static Lazy<DynamicLibraryManager> instance = new Lazy<DynamicLibraryManager>(() => new DynamicLibraryManager());

        #endregion

        #region Instance

        private DynamicLibraryManager()
        {
            this.loadedLibraries = new ConcurrentDictionary<string, DynamicLibrary>();
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The reference is stored in a dictionary and released upon Dispose.")]
        public object InstanceInvoke(string library, string method, Type returnType, params object[] arguments)
        {
            if (null == library)
                throw new ArgumentNullException("library");

            if (null == method)
                throw new ArgumentNullException("method");

            if (null == returnType)
                throw new ArgumentNullException("returnType");

            if (false == this.loadedLibraries.ContainsKey(library))
            {
                lock (this.loadedLibraries)
                {
                    if (false == this.loadedLibraries.ContainsKey(library))
                    {
                        this.loadedLibraries[library] = new DynamicLibrary(library);
                    }
                }
            }

            var result = this.loadedLibraries[library].Invoke(method, returnType, arguments);
            return result;
        }

        public void InstanceReset()
        {
            while (this.loadedLibraries.Keys.Count > 0)
            {
                this.InstanceReset(this.loadedLibraries.Keys.First(), throwIfNotFound: false);
            }
        }

        public void InstanceReset(string library, bool throwIfNotFound)
        {
            DynamicLibrary libraryToReset;
            if (this.loadedLibraries.TryGetValue(library, out libraryToReset))
            {
                this.loadedLibraries.Remove(library);
                libraryToReset.Dispose();
            }
            else if (throwIfNotFound)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Library '{0}' cannot be reset because it was not loaded in the first place.", library));
            }
        }

        private IDictionary<string, DynamicLibrary> loadedLibraries;

        #endregion
    }
}
