using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_STANDALONE_WIN
using System.Runtime.InteropServices;
#endif

namespace BlackTundra.Foundation.System {

    /// <summary>
    /// Class used to call system functions.
    /// This will have different implementations of some methods for different operating systems.
    /// </summary>
    public static class SystemUtility {

        #region constant

        /// <summary>
        /// Number of ticks that pass per second.
        /// </summary>
#if UNITY_STANDALONE_WIN
        public const long TicksPerSecond = 1000L;
#endif

        #endregion

        #region logic

        #region GetTickCount64
        // add code for other operating systems here
#if UNITY_STANDALONE_WIN

        [DllImport("kernel32")]
        public static extern ulong GetTickCount64();

#endif
        #endregion

        #region GetDecoratedTypes

        /// <returns>
        /// Returns every <see cref="Type"/> that is decorated with an annotation of type
        /// <typeparamref name="T"/> as an <see cref="IEnumerable{T}"/>.
        /// </returns>
        public static IEnumerable<Type> GetDecoratedTypes<T>() where T : Attribute {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Type context = typeof(T);
            foreach (Type type in assembly.GetTypes()) {
                if (type.GetCustomAttributes(context, true).Length > 0)
                    yield return type;
            }
        }

        #endregion

        #region GetImplementations

        /// <returns>
        /// Returns all objects that inherit from <typeparamref name="T"/>.
        /// </returns>
        public static IEnumerable<Type> GetImplementations<T>() where T : class {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Type context = typeof(T);
            foreach (Type type in assembly.GetTypes()) {
                if (type.IsClass && type.IsSubclassOf(context))
                    yield return type;
            }
        }

        #endregion

        #region GetMethods

        public static IEnumerable<MethodInfo> GetMethods<T>(BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) where T : Attribute => AppDomain.CurrentDomain.GetAssemblies() // return all currently loaded assemblies
            .SelectMany(x => x.GetTypes()) // get all types defined in the assembly
            .Where(x => x.IsClass) // only return classes
            .SelectMany(x => x.GetMethods(bindingFlags)) // get all static methods defined in the class
            .Where(x => x.GetCustomAttributes(typeof(T), false).FirstOrDefault() != null); // only return methods with the correct attribute

        #endregion

        #region InvokeMethods

        public static void InvokeMethods<T>(in object[] args = null) where T : Attribute {
            IEnumerable<MethodInfo> methods = GetMethods<T>();
            foreach (MethodInfo method in methods) {
                try {
                    method.Invoke(null, args);
                } catch (Exception exception) {
                    Console.Error($"Failed to invoked method \"{method.Name}\".", exception);
                }
            }
        }

        #endregion

        #region GetDelegateInfo

        /// <returns>
        /// Returns <see cref="MethodInfo"/> for a delegate of type <typeparamref name="T"/>.
        /// </returns>
        public static MethodInfo GetDelegateInfo<T>() where T : Delegate {
            MemberInfo[] memberInfo = typeof(T).GetMember("Invoke");
            if (memberInfo.Length == 0) throw new InvalidOperationException();
            return (MethodInfo)memberInfo[0];
        }

        #endregion

        #endregion

    }

}