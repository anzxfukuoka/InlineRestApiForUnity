using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

namespace NoTAUnityClient.Net
{
    /// <summary>
    /// HttpMethodDelegateWrapper interface
    /// </summary>
    public interface IHttpMethodDelegateWrapper
    {
        /// <summary>
        /// Gets the trivial delegate which handles HTTP methods with string data.
        /// </summary>
        /// <returns>The trivial delegate.</returns>
        HttpMethodDelegate GetTrivialDelegate();
    }

    /// <summary>
    /// Wrapper class for handling HTTP method delegates with generic request and response data types.
    /// </summary>
    /// <typeparam name="T">The type of the request data.</typeparam>
    /// <typeparam name="TResult">The type of the response data. This must be an object
    /// that can be serialized without needing the main thread.</typeparam>
    public class HttpMethodDelegateWrapper<T, TResult> : IHttpMethodDelegateWrapper
    {
        private readonly HttpMethodDelegate TrivialDelegate;

        /// <summary>
        /// Initializes a new instance of the HttpMethodDelegateWrapper class.
        /// </summary>
        /// <param name="MethodDelegate">The delegate to be wrapped, which handles the HTTP method 
        /// with generic request and response data.</param>
        public HttpMethodDelegateWrapper(HttpMethodGenericDelegate<T, TResult> MethodDelegate)
        {
            TrivialDelegate = async (RouteParams Params, string RequestDataStr) =>
            {
                T RequestData;

                if (typeof(T) != typeof(string))
                {
                    RequestData = JsonConvert.DeserializeObject<T>(RequestDataStr);

                }
                else
                {
                    RequestData = (T)(object)RequestDataStr;
                }

                TResult ResponseData = await MethodDelegate.Invoke(Params, RequestData);

                string JsonStr;

                if (typeof(TResult) != typeof(string))
                {
                    JsonStr = JsonConvert.SerializeObject(ResponseData).ToString();
                }
                else
                {
                    JsonStr = ResponseData.ToString();
                }

                return JsonStr;
            };

            //Debug.Log(TrivialDelegate);
        }

        /// <summary>
        /// Gets the trivial delegate which handles HTTP methods with string data.
        /// </summary>
        /// <returns>The trivial delegate.</returns>
        public HttpMethodDelegate GetTrivialDelegate()
        {
            return TrivialDelegate;
        }
    }
}
