using PimDeWitte.UnityMainThreadDispatcher;
using System;
using System.Threading.Tasks;

namespace NoTAUnityClient.Net.Threading
{
    /// <summary>
    /// This class provides helper methods to run actions and functions on the main thread in Unity.
    /// It uses UnityMainThreadDispatcher from: https://github.com/PimDeWitte/UnityMainThreadDispatcher
    /// </summary>
    public class ThreadingHelper
    {
        /// <summary>
        /// Runs a given action on the main thread.
        /// </summary>
        /// <param name="Action">The action to be run on the main thread.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public static async Task RunInMainThread(System.Action Action)
        {
            await RunInMainThread<object>(() => { Action(); return null as object; });
        }

        /// <summary>
        /// Runs a given function on the main thread and returns its result.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="Function">The function to be run on the main thread.</param>
        /// <returns>A Task representing the asynchronous operation, containing the result of the function.</returns>
        public static async Task<T> RunInMainThread<T>(Func<T> Function)
        {
            Task<T> FunctionTask = new Task<T>(Function);

            T result;

            UnityMainThreadDispatcher.Instance().Enqueue(
                 () =>
                 {
                     // Ordinarily, tasks are executed asynchronously on a thread pool thread and
                     // do not block the calling thread. Tasks executed by calling the
                     // RunSynchronously() method are associated with the current TaskScheduler
                     // and are run on the calling thread. 
                     FunctionTask.RunSynchronously();
                 });

            result = await FunctionTask;

            return result;
        }
    }
}
