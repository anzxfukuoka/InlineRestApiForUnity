using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace NoTAUnityClient.Net
{
    // url example: localhost:6666/path/{path_param}/path2/{path_param2}?query_param=query_value
    /// <summary>
    /// This class holds the parameters extracted from the route path and query string of an HTTP request.
    /// </summary>
    public class RouteParams
    {
        /// <summary>
        /// A dictionary to store parameters extracted from the path.
        /// </summary>
        public Dictionary<string, string> PathParams;
        /// <summary>
        /// A dictionary to store parameters extracted from the query string.
        /// </summary>
        public Dictionary<string, string> QueryParams;

        public RouteParams()
        {
            PathParams = new Dictionary<string, string>();
            QueryParams = new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Delegate type for handling HTTP methods.
    /// </summary>
    /// <param name="Params">Route parameters extracted from the path and query string.</param>
    /// <param name="RequestData">The data sent with the request.</param>
    /// <returns>The response data as a string.</returns>
    public delegate Task<string> HttpMethodDelegate(RouteParams Params, string RequestData);

    /// <summary>
    /// Delegate type for handling HTTP methods with generic request and response data.
    /// </summary>
    /// <typeparam name="T">The type of the request data.</typeparam>
    /// <typeparam name="TResult">The type of the response data.</typeparam>
    /// <param name="Params">Route parameters extracted from the path and query string.</param>
    /// <param name="RequestData">The data sent with the request.</param>
    /// <returns>The response data.</returns>
    public delegate Task<TResult> HttpMethodGenericDelegate<T, TResult>(RouteParams Params, T RequestData);

    /// <summary>
    /// This class defines a route and its associated HTTP methods handlers.
    /// </summary>
    public class Route
    {
        /// <summary>
        /// The path of the route.
        /// </summary>
        public readonly string Path;
        

        /// <summary>
        /// Handler for GET requests.
        /// </summary>
        public HttpMethodDelegate Get;

        /// <summary>
        /// Handler for POST requests.
        /// </summary>
        public HttpMethodDelegate Post;

        /// <summary>
        /// Handler for PUT requests.
        /// </summary>
        public HttpMethodDelegate Put;

        /// <summary>
        /// Handler for DELETE requests.
        /// </summary>
        public HttpMethodDelegate Delete;

        private string PathTemplate;
        private List<string> PathParamNames;
        private RouteParams Params;

        /// <summary>
        /// Initializes a new instance of the Route class with the specified path and handlers for HTTP methods.
        /// </summary>
        /// <param name="Path">The path template for the route.</param>
        /// <param name="Get">The handler for GET requests.</param>
        /// <param name="Post">The handler for POST requests.</param>
        /// <param name="Put">The handler for PUT requests.</param>
        /// <param name="Delete">The handler for DELETE requests.</param>
        public Route(string Path,
            IHttpMethodDelegateWrapper Get = null,
            IHttpMethodDelegateWrapper Post = null,
            IHttpMethodDelegateWrapper Put = null,
            IHttpMethodDelegateWrapper Delete = null)
        {
            this.Path = Path;

            this.Get = Get?.GetTrivialDelegate();
            this.Post = Post?.GetTrivialDelegate();
            this.Put = Put?.GetTrivialDelegate();
            this.Delete = Delete?.GetTrivialDelegate();

            Params = new RouteParams();
            ProcessPath();
        }

        /// <summary>
        /// Processes the path template to identify path parameters.
        /// </summary>
        private void ProcessPath()
        {
            PathTemplate = "";
            PathParamNames = new List<string>();

            string[] Segments = Path.Split('/');

            foreach (string Segment in Segments)
            {
                if (Segment.Equals(String.Empty))
                {
                    continue;
                }

                if (IsSegmentPathParam(Segment))
                {
                    //consists of one or more characters but "/".
                    PathTemplate += "([^/]+)" + "/"; 

                    PathParamNames.Add(Segment.Replace("{", "").Replace("}", ""));
                }
                else
                {
                    PathTemplate += Segment + "/";
                }
            }
        }

        /// <summary>
        /// Checks if a segment of the path is a parameter.
        /// </summary>
        /// <param name="Segment">The segment to check.</param>
        /// <returns>True if the segment is a path parameter, otherwise false.</returns>
        private bool IsSegmentPathParam(string Segment)
        {
            return Segment.StartsWith("{") && Segment.EndsWith("}");
        }

        /// <summary>
        /// Processes (future: and validates) query parameters.
        /// </summary>
        /// <param name="QueryParams">The query parameters to process.</param>
        private void ProcessQueryParams(Dictionary<string, string> QueryParams)
        {
            /* todo: написать валидацию
             * here supposed to be validation
             */

            Params.QueryParams = QueryParams;
        }

        /// <summary>
        /// Processes path parameters from the regex match.
        /// </summary>
        /// <param name="Match">The regex match containing the path parameters.</param>
        private void ProcessPathParams(Match Match)
        {
            try
            {
                for (int i = 0; i < PathParamNames.Count; i++)
                {
                    var key = PathParamNames[i];
                    /*
                     * If the regular expression engine can find a match, the first element 
                     * of the GroupCollection object (the element at index 0) returned by the 
                     * Groups property contains a string that matches the entire regular 
                     * expression pattern.
                     */
                    var value = Match.Groups[i + 1].Value;

                    Params.PathParams[key] = value;

                    //Debug.Log($"{key}: {value}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }

        /// <summary>
        /// Invokes the appropriate handler for the HTTP method.
        /// </summary>
        /// <param name="HttpMethod">The HTTP method (GET, POST, etc.).</param>
        /// <param name="RequestData">The data sent with the request.</param>
        /// <returns>The response data as a string.</returns>
        public async Task<string> InvokeRespectiveMethod(string HttpMethod, string RequestData)
        {
            object result;

            switch (HttpMethod)
            {
                case "GET":
                    result = Get?.Invoke(Params, RequestData);
                    break;
                case "POST":
                    result = Post?.Invoke(Params, RequestData);
                    break;
                case "PUT":
                    result = Put?.Invoke(Params, RequestData);
                    break;
                case "DELETE":
                    result = Delete?.Invoke(Params, RequestData);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported HttpMethod: {HttpMethod}");
            }

            if (result is Task<string> task) return await task;
            return (string)result;

        }

        /// <summary>
        /// Checks if the request path match this route.
        /// </summary>
        /// <param name="RequestPath">The path of the request.</param>
        /// <param name="QueryParams">The query parameters of the request.</param>
        /// <returns>True if the request matches this route, otherwise false.</returns>
        public bool Match(string RequestPath, Dictionary<string, string> QueryParams)
        {
            var Regex = new Regex(PathTemplate);

            //Debug.Log(PathTemplate);
            //Debug.Log(RequestPath);

            Match Match = Regex.Match(RequestPath);

            if (Match.Success)
            {
                ProcessQueryParams(QueryParams);
                ProcessPathParams(Match);
            }

            return Match.Success;
        }
    }
}
