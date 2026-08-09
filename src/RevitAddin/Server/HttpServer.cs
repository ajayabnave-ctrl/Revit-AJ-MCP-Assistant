using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RevitAJMCPAssistant.Server
{
    public class HttpServer
    {
        private HttpListener _listener;
        private bool _isRunning;
        private readonly Func<string, Task<string>> _requestHandler;

        public int Port { get; private set; }

        public HttpServer(int port, Func<string, Task<string>> requestHandler)
        {
            Port = port;
            _requestHandler = requestHandler;
        }

        public void Start()
        {
            if (_isRunning) return;

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{Port}/revit/v1/");
                _listener.Start();
                _isRunning = true;

                Task.Run(() => ListenAsync());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Revit-AJ-MCP] Failed to start HTTP Server: {ex.Message}");
            }
        }

        private async Task ListenAsync()
        {
            while (_isRunning && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = ProcessRequestAsync(context);
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Revit-AJ-MCP] HTTP Listen Error: {ex.Message}");
                }
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = (int)HttpStatusCode.OK;
                response.Close();
                return;
            }

            string responseBody = "{}";
            try
            {
                string requestBody = string.Empty;
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                if (_requestHandler != null)
                {
                    responseBody = await _requestHandler(requestBody);
                }

                byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                response.StatusCode = (int)HttpStatusCode.OK;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                byte[] errorBuffer = Encoding.UTF8.GetBytes($"{{\"error\":\"{ex.Message}\"}}");
                response.OutputStream.Write(errorBuffer, 0, errorBuffer.Length);
            }
            finally
            {
                response.Close();
            }
        }

        public void Stop()
        {
            _isRunning = false;
            if (_listener != null && _listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
            }
        }
    }
}
