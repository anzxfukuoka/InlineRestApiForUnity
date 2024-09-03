using NotACore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace NoTAUnityClient.Net
{
    /// <summary>
    /// A MonoBehaviour component for making RESTful HTTP requests.
    /// Provides methods for GET and POST requests with JSON serialization support.
    /// </summary>
    /// <example>
    /// Usage Example:
    /// <code>
    /// <![CDATA[
    /// void Start()
    /// {
    ///     var resourceUri = new Uri(new Uri(MainServerApiUri), "Chat");
    ///     
    ///     
    ///     RESTClient.Instance.Get<ChatMessage[]>(resourceUri.ToString(),
    ///         success: (ChatMessage[] result) =>
    ///         {
    ///             Debug.Log("Received messages: " + result.Length);
    ///         },
    ///         error: (UnityWebRequest webRequest) =>
    ///         {
    ///             Debug.LogError("GET request error: " + webRequest.error);
    ///         });
    ///         
    ///     
    ///     RESTClient.Instance.Post<ChatMessage>(resourceUri.ToString(), 
    ///         new ChatMessage(MessageType.User, "Hello, world!", DateTime.Now),
    ///         success: (UnityWebRequest webRequest) =>
    ///         {
    ///             Debug.Log("Message sent successfully");
    ///         },
    ///         error: (UnityWebRequest webRequest) =>
    ///         {
    ///             Debug.LogError("POST request error: " + webRequest.error);
    ///         });
    /// }
    /// ]]>
    /// </code>
    /// </example>
    public class RESTClient : MonoBehaviour
    {
        public static string MainServerApiUri = "https://localhost:7119/api/";

        #region GetMethods

        /// <summary>
        /// Sends a GET request to the specified URI.
        /// </summary>
        /// <param name="Uri">The URI to send the request to.</param>
        /// <param name="Success">Action to execute on a successful response.</param>
        /// <param name="Error">Action to execute on an error response.</param>
        public void Get(string Uri, 
            Action<UnityWebRequest> Success, Action<UnityWebRequest> Error = null) 
        {
            StartCoroutine(GetRequest(Uri, Success, Error));
        }

        /// <summary>
        /// Sends a GET request to the specified URI and deserializes the JSON response to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the JSON response to.</typeparam>
        /// <param name="Uri">The URI to send the request to.</param>
        /// <param name="Success">Action to execute on a successful response, with the deserialized object.</param>
        /// <param name="Error">Action to execute on an error response.</param>
        public void Get<T>(string Uri, 
            Action<T> Success, Action<UnityWebRequest> Error = null) 
        {
            Get(Uri: Uri,
                Success: (UnityWebRequest webRequest) =>
                {
                    T value = JsonConvert.DeserializeObject<T>(webRequest.downloadHandler.text);
                    Success(value);
                },
                Error: (UnityWebRequest webRequest) =>
                {
                    Error(webRequest);
                });
        }

        IEnumerator GetRequest(string Uri, 
            Action<UnityWebRequest> Success, Action<UnityWebRequest> Error = null)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(Uri))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                string[] pages = Uri.Split('/');
                int page = pages.Length - 1;

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                    case UnityWebRequest.Result.ProtocolError: // HTTP Error
                        Error(webRequest);
                        break;
                    case UnityWebRequest.Result.Success:
                        Success(webRequest);
                        break;
                }
            }
        }

        #endregion GetMethods

        #region PostMethods

        /// <summary>
        /// Sends a POST request with the specified data to the specified URI.
        /// </summary>
        /// <param name="Uri">The URI to send the request to.</param>
        /// <param name="Data">The data to send in the POST request body.</param>
        /// <param name="Success">Action to execute on a successful response.</param>
        /// <param name="Error">Action to execute on an error response.</param>
        public void Post(string Uri, string Data, 
            Action<UnityWebRequest> Success = null, Action<UnityWebRequest> Error = null) 
        {
            StartCoroutine(PostRequest(Uri, Data, Success, Error));
        }

        /// <summary>
        /// Sends a POST request with the specified object serialized to JSON to the specified URI.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize and send.</typeparam>
        /// <param name="Uri">The URI to send the request to.</param>
        /// <param name="Data">The object to serialize and send in the POST request body.</param>
        /// <param name="Success">Action to execute on a successful response.</param>
        /// <param name="Error">Action to execute on an error response.</param>
        public void Post<T>(string Uri, T Data, 
            Action<UnityWebRequest> Success = null, Action<UnityWebRequest> Error = null) 
        {
            var serializedData = JsonConvert.SerializeObject(Data).ToString();
            Post(Uri, serializedData, Success, Error);
        }

        IEnumerator PostRequest(string Uri, string Data, 
            Action<UnityWebRequest> Success = null, Action<UnityWebRequest> Error = null)
        {
            //using (UnityWebRequest webRequest = UnityWebRequest.Post(Uri, Data)) this one will escape charaters in postData
            using (UnityWebRequest webRequest = new UnityWebRequest(Uri, "POST"))
            {
                byte[] array = null;
                array = Encoding.UTF8.GetBytes(Data);
                webRequest.uploadHandler = new UploadHandlerRaw(array);
                webRequest.uploadHandler.contentType = "application/json";

                yield return webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Error.Invoke(webRequest);
                }
                else
                {
                    Success.Invoke(webRequest);
                }
            }
        }

        #endregion PostMethods
    }
}
