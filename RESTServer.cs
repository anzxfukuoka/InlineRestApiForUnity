using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NoTAUnityClient.Net
{
    /// <summary>
    /// This class represents a simple REST server implemented in Unity using MonoBehaviour.
    /// It handles HTTP requests and responses using routes defined by the user.
    /// </summary>
    /// <example>
    /// <h3>Example 1: Using RouteParams</h3>
    /// 
    /// 1. Adding routes
    /// 
    /// <code>
    /// <![CDATA[
    ///void Awake()
    ///{
    ///    AddRoute(new Route("api/{object_id}/color/",
    ///        Get: new HttpMethodDelegateWrapper<string, string>
    ///            (async (RouteParams RouteParams, string Data) =>
    ///        {
    ///            Debug.Log("Get!\nRP:" + RouteParams.PathParams["object_id"]
    ///                        + "\nQP:" + RouteParams.QueryParams["hex"]);
    ///
    ///            return @"{'return': 'value'}";
    ///        }),
    ///        Post: new HttpMethodDelegateWrapper<string, string>
    ///        (async (RouteParams RouteParams, string Data) =>
    ///        {
    ///            Debug.Log("Post!\nData:" + Data);
    ///
    ///            return "Success!";
    ///        })
    ///    ));
    ///
    ///    /*
    ///     > curl -X get localhost:6666/api/cube/color/?hex=c0ffee
    ///     {'return': 'value'}
    ///     > curl -X post localhost:6666/api/cube/color/ -d "{id: 12345678}"
    ///     Success!
    ///     */
    ///
    ///    // Error: route with this path already exsists
    ///    //AddRoute(new Route("api/{object_id}/color/"));
    ///}
    /// ]]>
    /// </code>
    /// 
    /// 2. Testing with curl
    /// 
    /// GET request:
    /// 
    /// <code>
    /// <![CDATA[
    /// > curl -X get localhost:6666/api/cube/color/?hex=c0ffee
    /// ]]>
    /// </code>
    /// 
    /// This request is sent to <![CDATA[/api/cube/color/?hex=c0ffee]]> with the request 
    /// parameter hex=c0ffee. In response you will receive:
    /// 
    /// <code>
    /// <![CDATA[
    /// {'return': 'value'}
    /// ]]>
    /// </code>
    /// 
    /// POST request: 
    /// 
    /// <code>
    /// <![CDATA[
    /// > curl -X post localhost:6666/api/cube/color/ -d "{id: 12345678}"
    /// ]]></code>
    /// 
    /// This request is sent to the same address, but with the request 
    /// body <![CDATA[{id: 12345678}.]]> In response you will receive:
    /// 
    /// <code>
    /// <![CDATA[
    /// Success!
    /// ]]></code>
    /// 
    /// <h3>Example 2: Json serialization</h3>
    /// 
    /// The example demonstrates handling GET and POST requests, where the return 
    /// types specified in the delegate (other than string and its equivalents) are 
    /// serialized to JSON, and argument types are deserialized from JSON strings. 
    /// If the user does not need to read or send data in a request, they can specify 
    /// the type string for T or TResult respectively and return an empty string.
    /// 
    /// 1. Adding routes
    /// 
    /// <code>
    /// <![CDATA[
    ///public void Awake()
    ///{
    ///    Server.AddRoute(new Route("api/avatar/expression/",
    ///        Get: new HttpMethodDelegateWrapper<String, string[]>
    ///            (async (RouteParams RouteParams, string Data) =>
    ///            {
    ///                var ExpressionList = await ThreadingHelper.RunInMainThread<string[]>(
    ///                        ExpressionController.GetExpressionsList);
    ///
    ///                Debug.Log("Sending expression list...");
    ///
    ///                return ExpressionList;
    ///            }),
    ///        Post: new HttpMethodDelegateWrapper<Dictionary<string, string>, Dictionary<string, string>>
    ///            (async (RouteParams RouteParams, Dictionary<string, string> Data) =>
    ///            {
    ///                var ExpName = Data["expression_name"];
    ///
    ///                await ThreadingHelper.RunInMainThread(() => ExpressionController.PlayExpression(ExpName));
    ///
    ///                var msg = $"Set expression: {ExpName}";
    ///
    ///                Debug.Log(msg);
    ///
    ///                return new Dictionary<string, string>
    ///                {
    ///                    { "status", msg }
    ///                };
    ///            })
    ///        ));
    ///
    ///    /*
    ///     * > curl -X get localhost:6666/api/avatar/expression/
    ///     * ["neutral","angry","joy","fun","sorrow","suprised"]
    ///     * curl -X post localhost:6666/api/avatar/expression/ -d "{'expression_name': 'joy'}"
    ///     * OK
    ///     */
    ///}
    /// ]]>
    /// </code>
    /// 
    /// 2. Testing with curl
    /// 
    /// GET request:
    /// 
    /// <code>
    /// <![CDATA[
    /// > curl -X get localhost:6666/api/avatar/expression/
    /// ]]>
    /// </code>
    ///
    /// Response:
    /// 
    /// <code>
    /// <![CDATA[
    /// ["neutral","angry","joy","fun","sorrow","suprised"]
    /// ]]>
    /// </code>
    /// 
    /// POST request: 
    /// 
    /// <code>
    /// <![CDATA[
    /// > curl -X post localhost:6666/api/avatar/expression/ -d "{'expression_name': 'joy'}"
    /// ]]></code>
    /// 
    /// Response:
    /// 
    /// <code>
    /// <![CDATA[
    /// {"status":"Set expression: joy"}
    /// ]]></code> 
    /// </example>
    public class RESTServer : MonoBehaviour
    {
        /// <summary>
        /// The port on which the server will listen for HTTP requests.
        /// </summary>
        public int Port = 6666;

        /// <summary>
        /// A dictionary to store the routes and their corresponding handlers.
        /// </summary>
        private Dictionary<string, Route> Routes;

        private HttpListener Listener;
        private Thread ListenerThread;

        /// <summary>
        /// Adds a route to the server's routes
        /// </summary>
        /// <param name="Route">The route to be added.</param>
        public void AddRoute(Route Route) 
        {
            if (Routes == null) 
            {
                Routes = new Dictionary<string, Route>();
            }

            if (Routes.TryGetValue(Route.Path, out Route tmp))
            {
                Debug.LogError($"Route {Route.Path} already exsists");
            }
            else 
            {
                Routes.Add(Route.Path, Route);
            }
        }

        private void Awake()
        {
            
        }

        void Start()
        {
            ListenerThread = new Thread(StartListener);
            ListenerThread.Start();
            
        }

        void Update()
        {

        }

        /// <summary>
        /// Starts the HTTP listener and processes incoming requests
        /// </summary>
        async private void StartListener()
        {
            Listener = new HttpListener();
            Listener.Prefixes.Add($"http://localhost:{Port}/");
            Listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            Listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            Listener.Start();

            Debug.Log($"REST Server is running on: {$"http://127.0.0.1:{Port}/"}");

            try
            {
                while (true)
                {
                    var context = await Listener.GetContextAsync();
                    Task.Run(async () =>
                    {
                        using (context.Response)
                        {
                            await HandleRequestAsyncRun(context);
                        }

                    });
                }
            }
            catch (Exception e)
            {
                // Debug.LogError($"Listener error: {e.Message}");
            }
            finally
            {
                Listener.Stop();
            }
        }

        /// <summary>
        /// Handles the incoming HTTP requests
        /// </summary>
        /// <param name="context">The context of the incoming HTTP request.</param>
        private async Task HandleRequestAsyncRun(HttpListenerContext context)
        {
            // Unity just swallows exeptions in non-main threads
            // AppDomain.UnhandledException is fired in IL2CPP but not in Mono nor in editor
            try 
            {
                var RequestPath = context.Request.Url.LocalPath;

                var QueryParams = context.Request.QueryString.Keys.Cast<string>()
                .ToDictionary(k => k, v => context.Request.QueryString[v]);

                var HttpMethod = context.Request.HttpMethod.ToUpper().Trim();
                var RequestData = await new StreamReader(context.Request.InputStream,
                                        context.Request.ContentEncoding).ReadToEndAsync();

                if (Routes != null)
                {
                    foreach ((var Path, var Route) in Routes)
                    {
                        if (Route.Match(RequestPath, QueryParams))
                        {
                            var ResposeData = await Route.InvokeRespectiveMethod(HttpMethod, RequestData);

                            await SendResponse(context.Response, HttpStatusCode.OK, ResposeData);

                            break;
                        }
                        else
                        {
                            Debug.LogWarning($"No matching route found: {RequestPath}. Are you forgot to put \"/\" at the end?");
                            await SendResponse(context.Response, HttpStatusCode.NotFound, "Invalid path");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            
                
        }

        /// <summary>
        /// Sends the response to the client.
        /// </summary>
        /// <param name="Response">The HttpListenerResponse object.</param>
        /// <param name="StatusCode">The HTTP status code to be sent.</param>
        /// <param name="ResposeData">The data to be sent in the response body.</param>
        public async Task SendResponse(HttpListenerResponse Response, HttpStatusCode StatusCode, string ResposeData)
        {
            Response.ContentType = "application/json";
            Response.StatusCode = (int)StatusCode;

            byte[] ResponseBody;

            ResponseBody = System.Text.Encoding.UTF8.GetBytes(ResposeData);
            Response.ContentLength64 = ResponseBody.Length;

            await Response.OutputStream.WriteAsync(ResponseBody, 0, ResponseBody.Length);

        }

        private void OnApplicationQuit()
        {
            Listener?.Stop();
        }
    }
}
