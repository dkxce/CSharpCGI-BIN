using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web;
using System.Net;
using System.Net.Sockets;

namespace cgi_bin_cs
{
    class Program
    {        
        [STAThread]
        static void Main(string[] args)
        {
            if ((args != null) && (args.Length > 0) && (args[0] == "/call"))
                CGIBINCaller.Test();
            else if ((args != null) && (args.Length > 0) && (args[0].StartsWith("/web")))
                CGIBINModule.WebTest(args[0].Length > 5 ? int.Parse(args[0].Substring(5)) : 80);                
            else if ((args != null) && (args.Length > 0) && ((args[0] == "/help") || (args[0] == "/?")))
            {
                Console.WriteLine("To test use /call");
                Console.WriteLine("To test use /web");
                Console.WriteLine("To test use /web=port");
            }
            else
                CGIBINModule.Test();            
        }
    }

    /// <summary>
    ///    Module for CGI-BIN implementation of executable file
    /// </summary>
    public class CGIBINModule
    {
        [DllImport("kernel32", SetLastError = true)]
        private static extern int SetConsoleMode(int hConsoleHandle, int dwMode);

        public Dictionary<string, object> Variables = new Dictionary<string, object>();

        public System.Collections.Specialized.NameValueCollection QUERY_PARAMS = new System.Collections.Specialized.NameValueCollection();
        public System.Collections.Specialized.NameValueCollection GET_PARAMS { get { return QUERY_PARAMS; } }

        public System.Collections.Specialized.NameValueCollection CONTENT_PARAMS = new System.Collections.Specialized.NameValueCollection();
        public System.Collections.Specialized.NameValueCollection POST_PARAMS { get { return CONTENT_PARAMS; } }
        public string POST_DATA { get { if ((CONTENT_DATA != null) && (CONTENT_DATA.Length > 0)) return System.Text.Encoding.ASCII.GetString(CONTENT_DATA); else return ""; } }
        public string POST_DATA_W1251 { get { if ((CONTENT_DATA != null) && (CONTENT_DATA.Length > 0)) return System.Text.Encoding.GetEncoding(1251).GetString(CONTENT_DATA); else return ""; } }
        public string POST_DATA_UTF8 { get { if ((CONTENT_DATA != null) && (CONTENT_DATA.Length > 0)) return System.Text.Encoding.UTF8.GetString(CONTENT_DATA); else return ""; } }
        public byte[] POST_DATA_BYTES { get { return CONTENT_DATA; } }
        public int POST_DATA_LENGTH { get { return CONTENT_LENGTH; } }

        public string GATEWAY_INTERFACE;

        public string SERVER_NAME;
        public string SERVER_SOFTWARE;
        public string SERVER_PROTOCOL;
        public int SERVER_PORT = 80;

        public string PATH_INFO;
        public string PATH_TRANSLATED;
        public string SCRIPT_NAME;
        public string DOCUMENT_ROOT; 

        public string REQUEST_METHOD;
        public string REQUEST_URI;
        public string QUERY_STRING;
        public string GET_STRING { get { return QUERY_STRING; } }
              
        public string REMOTE_HOST;
        public string REMOTE_ADDR;

        public string AUTH_TYPE;
        public string REMOTE_USER;

        public string CONTENT_TYPE;
        public byte[] CONTENT_DATA = null;
        public int CONTENT_LENGTH = 0;        

        public string HTTP_ACCEPT;
        public string HTTP_USER_AGENT;
        public string HTTP_REFERER;
        public string HTTP_COOKIE;
        public string HTTPS;

        private bool _canwriteheader = true;

        public CGIBINModule()
        {
            SetConsoleMode(3, 0);
            ReadVars();            
        }

        private void ReadVars()
        {
            // Get Post Data
            Variables.Add("CONTENT_TYPE",      CONTENT_TYPE = System.Environment.GetEnvironmentVariable("CONTENT_TYPE"));
            int.TryParse(System.Environment.GetEnvironmentVariable("CONTENT_LENGTH"), out CONTENT_LENGTH);
            Variables.Add("CONTENT_LENGTH",    CONTENT_LENGTH);
            if (CONTENT_LENGTH > 0)
            {
                CONTENT_DATA = new byte[CONTENT_LENGTH];
                for (int i = 0; i < CONTENT_LENGTH; i++)
                    CONTENT_DATA[i] = (byte)Console.Read();
            };
            Variables.Add("CONTENT_DATA",      CONTENT_DATA);
			Variables.Add("GATEWAY_INTERFACE", GATEWAY_INTERFACE = System.Environment.GetEnvironmentVariable("GATEWAY_INTERFACE"));
            Variables.Add("SERVER_NAME",       SERVER_NAME = System.Environment.GetEnvironmentVariable("SERVER_NAME"));
            Variables.Add("SERVER_SOFTWARE",   SERVER_SOFTWARE = System.Environment.GetEnvironmentVariable("SERVER_SOFTWARE"));
            Variables.Add("SERVER_PROTOCOL",   SERVER_PROTOCOL = System.Environment.GetEnvironmentVariable("SERVER_PROTOCOL"));
            int.TryParse(System.Environment.GetEnvironmentVariable("SERVER_PORT"), out SERVER_PORT);
            Variables.Add("SERVER_PORT",       SERVER_PORT); 
            Variables.Add("PATH_INFO",         PATH_INFO = System.Environment.GetEnvironmentVariable("PATH_INFO"));
            Variables.Add("PATH_TRANSLATED",   PATH_TRANSLATED = System.Environment.GetEnvironmentVariable("PATH_TRANSLATED"));
            Variables.Add("SCRIPT_NAME",       SCRIPT_NAME = System.Environment.GetEnvironmentVariable("SCRIPT_NAME"));
            Variables.Add("DOCUMENT_ROOT",     DOCUMENT_ROOT = System.Environment.GetEnvironmentVariable("DOCUMENT_ROOT"));
            Variables.Add("REQUEST_METHOD",    REQUEST_METHOD = System.Environment.GetEnvironmentVariable("REQUEST_METHOD"));
            Variables.Add("REQUEST_URI",       REQUEST_URI = System.Environment.GetEnvironmentVariable("REQUEST_URI"));
            Variables.Add("QUERY_STRING",      QUERY_STRING = System.Environment.GetEnvironmentVariable("QUERY_STRING"));
            Variables.Add("REMOTE_HOST",       REMOTE_HOST = System.Environment.GetEnvironmentVariable("REMOTE_HOST"));
            Variables.Add("REMOTE_ADDR",       REMOTE_ADDR = System.Environment.GetEnvironmentVariable("REMOTE_ADDR"));
            Variables.Add("AUTH_TYPE",         AUTH_TYPE = System.Environment.GetEnvironmentVariable("AUTH_TYPE"));
            Variables.Add("REMOTE_USER",       REMOTE_USER = System.Environment.GetEnvironmentVariable("REMOTE_USER")); 
            Variables.Add("HTTP_ACCEPT",       HTTP_ACCEPT = System.Environment.GetEnvironmentVariable("HTTP_ACCEPT"));
            Variables.Add("HTTP_USER_AGENT",   HTTP_USER_AGENT = System.Environment.GetEnvironmentVariable("HTTP_USER_AGENT"));
            Variables.Add("HTTP_REFERER",      HTTP_REFERER = System.Environment.GetEnvironmentVariable("HTTP_REFERER")); 
            Variables.Add("HTTP_COOKIE",       HTTP_COOKIE = System.Environment.GetEnvironmentVariable("HTTP_COOKIE"));
            Variables.Add("HTTPS",             HTTPS = System.Environment.GetEnvironmentVariable("HTTPS"));    
     
            if(QUERY_STRING != null)
                QUERY_PARAMS = HttpUtility.ParseQueryString(QUERY_STRING);

            if ((CONTENT_DATA != null) && (CONTENT_DATA.Length > 0))
            {
                string cd = System.Text.Encoding.UTF8.GetString(CONTENT_DATA);
                try { CONTENT_PARAMS = HttpUtility.ParseQueryString(cd); }
                catch { };
            };
        }

        public string VariableToString(object value)
        {
            if (value == null) return "";
            Type valueType = value.GetType();
            if (valueType.IsArray && (value.ToString() == "System.Byte[]"))
                return System.Text.Encoding.UTF8.GetString((byte[])value);
            return value.ToString();
        }

        public void WriteReponseHeader(string header)
        {
            if (_canwriteheader)
                Console.Out.Write(header + "\n");
            else
                throw new System.IO.EndOfStreamException("Write Headers before GetResponseStream");
        }

        public void WriteReponseHeader(System.Collections.Specialized.NameValueCollection headers)
        {
            if (_canwriteheader)
            {
                if(headers.Count > 0)
                    foreach(string key in headers.AllKeys)
                        Console.Out.Write(key + ": " + headers[key] + "\n");
            }
            else
                throw new System.IO.EndOfStreamException("Write Headers before GetResponseStream");
        }

        public void WriteReponseHeader(IDictionary<string,string> headers)
        {
            if (_canwriteheader)
            {
                if (headers.Count > 0)
                    foreach (KeyValuePair<string,string> nv in headers)
                        Console.Out.Write(nv.Key + ": " + nv.Value + "\n");
            }
            else
                throw new System.IO.EndOfStreamException("Write Headers before GetResponseStream");
        }

        public void WriteReponseHeader(KeyValuePair<string,string> header)
        {
            if (_canwriteheader)
                Console.Out.Write(header.Key + ": " + header.Value + "\n");
            else
                throw new System.IO.EndOfStreamException("Write Headers before GetResponseStream");
        }

        public void WriteReponseHeader(string name, string value)
        {
            if(_canwriteheader)
                Console.Out.Write(name + ": " + value + "\n");
            else
                throw new System.IO.EndOfStreamException("Write Headers before GetResponseStream");
        }

        public System.IO.Stream GetResponseStream()
        {
            if (_canwriteheader) { Console.Out.Write("\n"); Console.Out.Flush(); };
            _canwriteheader = false;
            return Console.OpenStandardOutput();
        }

        public System.IO.Stream GetResponseStream(System.Text.Encoding encoding)
        {
            if (_canwriteheader) { Console.Out.Write("\n"); Console.Out.Flush(); };
            _canwriteheader = false;
            Console.OutputEncoding = encoding;
            return Console.OpenStandardOutput();
        }

        public System.IO.StreamWriter GetResponseStreamWriter()
        {
            if (_canwriteheader) { Console.Out.Write("\n"); Console.Out.Flush(); };
            _canwriteheader = false;
            return new System.IO.StreamWriter(Console.OpenStandardOutput());
        }

        public System.IO.StreamWriter GetResponseStreamWriter(System.Text.Encoding encoding)
        {
            if (_canwriteheader) { Console.Out.Write("\n"); Console.Out.Flush(); };
            _canwriteheader = false;
            Console.OutputEncoding = encoding;
            return new System.IO.StreamWriter(Console.OpenStandardOutput(), encoding);
        }

        public void WriteResponse(string response)
        {
            { Console.Out.Write("\n"); Console.Out.Flush(); };
            _canwriteheader = false;
            Console.Write(response);
        }

        public void WriteResponse(byte[] data)
        {
            { Console.Out.Write("\n"); Console.Out.Flush(); };
            _canwriteheader = false;
            Console.OpenStandardOutput().Write(data, 0, data.Length);
        }

        public void CloseResponse()
        {
            if (_canwriteheader)
               Console.Out.Write("\n");
            Console.Out.Close();
        }

        public static void Test()
        {
            CGIBINModule cgi = new CGIBINModule();

            // WRITE HEADERS FIRST
            cgi.WriteReponseHeader("Status: 201 Created");
            cgi.WriteReponseHeader("CGI-Script: Demo C# CGI-bin Test");
            cgi.WriteReponseHeader("Content-Type: text/html;charset=utf-8");

            // WRITE BODY NEXT

            System.IO.StreamWriter response = cgi.GetResponseStreamWriter(System.Text.Encoding.UTF8);
            
            response.Write("<html><head><title>CGI in C#</title></head><body>CGI Environment Variables (–усо)<br />");
            response.Write("<table border=\"1\">");
            { // LIST Environment Variables
                int del = 1;
                foreach (KeyValuePair<string, object> kv in cgi.Variables)
                    response.Write("<tr><td>" + (del++).ToString("00") + "</td><td>" + kv.Key + "</td><td>" + cgi.VariableToString(kv.Value) + "</td></tr>");
            };
            { // LIST GET QUERY
                if (cgi.GET_PARAMS.Count > 0)
                    foreach (string q in cgi.GET_PARAMS.AllKeys)
                        response.Write("<tr><td>GET</td><td>" + q + "</td><td>" + cgi.GET_PARAMS[q] + "</td></tr>");
            };
            { // LIST POST QUERY
                if (cgi.POST_PARAMS.Count > 0)
                    foreach (string q in cgi.POST_PARAMS.AllKeys)
                        response.Write("<tr><td>POST</td><td>" + q + "</td><td>" + cgi.POST_PARAMS[q] + "</td></tr>");
            };
            response.Write("<tr><td colspan=\"3\"><form method=\"post\"><input type=\"text\" name=\"param1\"/><input type=\"submit\"/></form></td></tr>");
            response.Write("</table></body></html>");

            response.Close();
            cgi.CloseResponse();

            // Exit Environment
            Environment.Exit(0);
        }


        public static void WebTest(int port)
        {
            SimpleTCPServer stcps = new SimpleTCPServer(port);
            stcps.Start();
            Console.WriteLine("Listen: http://127.0.0.1:{0}/", port);
            Console.WriteLine("Press Enter to Exit");
            Console.ReadLine();
            stcps.Stop();
        }

        private class SimpleTCPServer
        {
            private Thread mainThread = null;
            private TcpListener mainListener = null;
            private IPAddress ListenIP = IPAddress.Any;
            private int ListenPort = 80;
            private bool isRunning = false;

            public SimpleTCPServer() { }
            public SimpleTCPServer(int Port) { this.ListenPort = Port; }
            public SimpleTCPServer(IPAddress IP, int Port) { this.ListenIP = IP; this.ListenPort = Port; }

            public bool Running { get { return isRunning; } }
            public IPAddress ServerIP { get { return ListenIP; } }
            public int ServerPort { get { return ListenPort; } set { ListenPort = value; } }

            public void Dispose() { Stop(); }
            ~SimpleTCPServer() { Dispose(); }

            public virtual void Start()
            {
                if (isRunning) throw new Exception("Server Already Running!");

                isRunning = true;
                mainThread = new Thread(MainThread);
                mainThread.Start();
            }

            private void MainThread()
            {
                mainListener = new TcpListener(this.ListenIP, this.ListenPort);
                mainListener.Start();
                while (isRunning)
                {
                    try
                    {
                        GetClient(mainListener.AcceptTcpClient());
                    }
                    catch { };
                    Thread.Sleep(1);
                };
            }

            public virtual void Stop()
            {
                if (!isRunning) return;

                isRunning = false;

                if (mainListener != null) mainListener.Stop();
                mainListener = null;

                mainThread.Join();
                mainThread = null;
            }

            private static void GetHTTP_Params(string Request, ref string method, ref string query, ref byte[] BODY)
            {
                if ((Request.Length >= 4) && (Request.IndexOf("\n") > 0))
                {
                    string body = "";
                    int hi = Request.IndexOf("HTTP");
                    if (hi > 0)
                    {
                        if (Request.IndexOf("GET") == 0)
                        {
                            method = "GET";
                            query = Request.Substring(4, hi - 4).Trim();
                        }
                        else if (Request.IndexOf("POST") == 0)
                        {
                            method = "POST";
                            query = Request.Substring(5, hi - 5).Trim();
                        }
                        if (method != "")
                        {
                            int qsw = query.IndexOf("?");
                            if (qsw < 0) query = ""; else query = query.Substring(qsw + 1);
                            int db = Request.IndexOf("\r\n\r\n");
                            if (db > 0)
                                body = Request.Substring(db + 4);
                            else
                            {
                                db = Request.IndexOf("\n\n");
                                if (db > 0)
                                    body = Request.Substring(db + 2);
                            };
                        };
                    };
                    if (body.Length > 0) BODY = System.Text.Encoding.ASCII.GetBytes(body);
                };
            }

            private static void GetHTTP_AUAR(string Request, ref string accept, ref string userAgent, ref string referer)
            {
                {
                    int si = Request.IndexOf("Accept:");
                    if (si > 0)
                    {
                        int li = Request.IndexOf("\n", si + 7);
                        referer = Request.Substring(si + 7, li - (si + 7)).Trim('\n').Trim('\r').Trim();
                    };
                };
                {
                    int si = Request.IndexOf("User-Agent:");
                    if (si > 0)
                    {
                        int li = Request.IndexOf("\n", si + 11);
                        userAgent = Request.Substring(si + 11, li - (si + 11)).Trim('\n').Trim('\r').Trim();
                    };
                };
                {
                    int si = Request.IndexOf("Referer:");
                    if (si > 0)
                    {
                        int li = Request.IndexOf("\n", si + 8);
                        accept = Request.Substring(si + 8, li - (si + 8)).Trim('\n').Trim('\r').Trim();
                    };
                };
            }

            private static string GetHTTP_Header(string Request, string Key)
            {
                if (Request == null) return null;
                if (Request == "") return null;
                if (Request.Length < Key.Length) return null;

                int si = Request.IndexOf(Key);
                if (si > 0)
                {
                    int li = Request.IndexOf("\n", si);
                    return Request.Substring(si, li - si).Trim('\n').Trim('\r').Trim();
                };
                return null;
            }

            private static int GetHTTP_RespStatus(ref string Request)
            {
                int result = 200;
                if ((Request != null) && (Request.Length > 1) && (Request.IndexOf("Status:") >= 0))
                {
                    int si = Request.IndexOf("Status:");
                    int li = Request.IndexOf(" ", si + 8);
                    int.TryParse(Request.Substring(si + 8, li - (si + 8)).Trim(), out result);
                    Request = Request.Remove(si, Request.IndexOf('\n', si) + 1);
                };
                return result;
            }

            public virtual void GetClient(TcpClient Client)
            {
                string Request = "";
                byte[] Buffer = new byte[4096];
                int Count;

                // GET INCOMING DATA
                while ((Count = Client.GetStream().Read(Buffer, 0, Buffer.Length)) > 0)
                {
                    Request += Encoding.ASCII.GetString(Buffer, 0, Count);
                    if (Request.IndexOf("\r\n\r\n") >= 0 || Request.Length > 4096) { break; };
                };

                // GET HTTP PARAMS
                string method = "", query = "";
                byte[] body = new byte[0];
                GetHTTP_Params(Request, ref method, ref query, ref body);
                
                string accept = null, userAgent = null, referer = null;
                GetHTTP_AUAR(Request, ref accept, ref userAgent, ref referer);

                // CALL CGI BIN
                Dictionary<string, string> pars = new Dictionary<string, string>();
                pars.Add("CONTENT_LENGTH", body.Length.ToString());
                pars.Add("SERVER_PORT", ListenPort.ToString());
                pars.Add("REQUEST_METHOD", method);
                pars.Add("DOCUMENT_ROOT", AppDomain.CurrentDomain.BaseDirectory);
                if (!String.IsNullOrEmpty(query)) pars.Add("QUERY_STRING", query);
                if (!String.IsNullOrEmpty(accept)) pars.Add("HTTP_ACCEPT", accept);
                if (!String.IsNullOrEmpty(userAgent)) pars.Add("QUERY_USER_AGENT", userAgent);
                if (!String.IsNullOrEmpty(referer)) pars.Add("QUERY_REFERER", referer);  
              
                // EXECUTE
                CGIBINCaller.Response resp = CGIBINCaller.Call(System.Reflection.Assembly.GetExecutingAssembly().Location, body, pars);

                // RESPONSE

                // HEADER
                int Code = GetHTTP_RespStatus(ref resp.Header);
                string RHeader = "HTTP/1.1 " +Code.ToString() + " " + ((HttpStatusCode)Code).ToString() + "\r\n";
                if (String.IsNullOrEmpty(GetHTTP_Header(resp.Header, "Content-Type"))) RHeader += "Content-Type: text/html;charset=utf-8\r\n";
                if (String.IsNullOrEmpty(GetHTTP_Header(resp.Header, "Content-Length"))) RHeader += "Content-Length: " + (resp.Content == null ? 0 : resp.Content.Length).ToString() + "\r\n";
                if (!String.IsNullOrEmpty(resp.Header)) RHeader += resp.Header.Trim('\n').Trim('\r') + "\r\n";
                RHeader += "\r\n";

                byte[] OutBuffer = Encoding.ASCII.GetBytes(RHeader);
                Client.GetStream().Write(OutBuffer, 0, OutBuffer.Length);
                // BODY
                if((resp.Content != null) && (resp.Content.Length > 0))
                    Client.GetStream().Write(resp.Content, 0, resp.Content.Length);

                Client.Client.Close();
                Client.Close();
            }
        }
    }

    /// <summary>
    ///     Module for call CGI-BIN executable file
    /// </summary>
    public class CGIBINCaller
    {
        public class Response
        {
            /// <summary>
            ///     Respone Header
            /// </summary>
            public string Header;

            /// <summary>
            ///     Response Body
            /// </summary>
            public string Body
            {
                get
                {
                    if (Content == null) return null;
                    if (Content.Length == 0) return "";
                    int chs = Header.IndexOf("charset=");
                    if (chs < 0)
                        return System.Text.Encoding.UTF8.GetString(Content);
                    else
                    {
                        int lind = Header.IndexOf("\n", chs + 8);
                        if (lind < 0) return System.Text.Encoding.UTF8.GetString(Content);
                        string charset = Header.Substring(chs + 8, lind - (chs + 8)).Trim('\n').Trim('\r').Trim();
                        return System.Text.Encoding.GetEncoding(charset).GetString(Content);                        
                    };
                }
            }

            /// <summary>
            ///     Response Content
            /// </summary>
            public byte[] Content;

            public Response(string header, byte[] content)
            {
                this.Header = header;
                this.Content = content;
            }
        }

        private static void SetDefaultParams(string path, System.Diagnostics.ProcessStartInfo startInfo)
        {
            startInfo.EnvironmentVariables["CONTENT_TYPE"]   = "application/x-www-form-urlencoded";
            startInfo.EnvironmentVariables["CONTENT_LENGTH"] = "0";
            startInfo.EnvironmentVariables["CONTENT_DATA"]   = "";
            startInfo.EnvironmentVariables["GATEWAY_INTERFACE"] = "CGI/1.1";
            startInfo.EnvironmentVariables["SERVER_NAME"] = "";
            startInfo.EnvironmentVariables["SERVER_SOFTWARE"] = "CGIBINCaller";
            startInfo.EnvironmentVariables["SERVER_PROTOCOL"] = "HTTP/1.0";
            startInfo.EnvironmentVariables["SERVER_PORT"] = "80";
            startInfo.EnvironmentVariables["PATH_INFO"] = "";
            startInfo.EnvironmentVariables["PATH_TRANSLATED"] = path;
            startInfo.EnvironmentVariables["SCRIPT_NAME"] = System.IO.Path.GetFileName(path);
            startInfo.EnvironmentVariables["DOCUMENT_ROOT"] = System.IO.Path.GetFileName(path);
            startInfo.EnvironmentVariables["REQUEST_METHOD"] = "GET";
            startInfo.EnvironmentVariables["REQUEST_URI"] = "";
            startInfo.EnvironmentVariables["QUERY_STRING"] = "";
            startInfo.EnvironmentVariables["REMOTE_HOST"] = "127.0.0.1";
            startInfo.EnvironmentVariables["REMOTE_ADDR"] = "127.0.0.1";
            startInfo.EnvironmentVariables["AUTH_TYPE"] = "";
            startInfo.EnvironmentVariables["REMOTE_USER"] = "";
            startInfo.EnvironmentVariables["HTTP_ACCEPT"] =  "text/html,application/xhtml,application/xml";
            startInfo.EnvironmentVariables["HTTP_USER_AGENT"] = "CGIBINCaller";
            startInfo.EnvironmentVariables["HTTP_REFERER"] = "";
            startInfo.EnvironmentVariables["HTTP_COOKIE"] = "";
            startInfo.EnvironmentVariables["HTTPS"] = "";
        }

        private static void SetParams(IDictionary<string, string> pars, System.Diagnostics.ProcessStartInfo startInfo)
        {
            foreach(KeyValuePair<string,string> kv in pars)
                startInfo.EnvironmentVariables[kv.Key] = kv.Value;
        }

        private static void SetParams(IDictionary<string, object> pars, System.Diagnostics.ProcessStartInfo startInfo)
        {
            foreach (KeyValuePair<string, object> kv in pars)
                startInfo.EnvironmentVariables[kv.Key] = kv.Value.ToString();
        }

        private static void SetParams(System.Collections.Specialized.NameValueCollection pars, System.Diagnostics.ProcessStartInfo startInfo)
        {
            foreach (string key in pars.AllKeys)
                startInfo.EnvironmentVariables[key] = pars[key];
        }

        private static Response CallBin(string path, byte[] postBody, IDictionary<string, string> p1, IDictionary<string, object> p2, System.Collections.Specialized.NameValueCollection p3)
        {
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.FileName = path;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.StandardOutputEncoding = System.Text.Encoding.UTF7;

            SetDefaultParams(path, startInfo);
            if (p1 != null) SetParams(p1, startInfo);
            if (p2 != null) SetParams(p2, startInfo);
            if (p3 != null) SetParams(p3, startInfo);

            // IF POST BODY
            if ((postBody != null) && (postBody.Length > 0))
            {
                startInfo.EnvironmentVariables["CONTENT_LENGTH"] = postBody.Length.ToString();
                startInfo.EnvironmentVariables["REQUEST_METHOD"] = "POST";
            };

            System.Diagnostics.Process proc = System.Diagnostics.Process.Start(startInfo);

            // IF POST BODY
            if ((postBody != null) && (postBody.Length > 0))
            {
                proc.StandardInput.BaseStream.Write(postBody, 0, postBody.Length);
                proc.StandardInput.BaseStream.Flush();
            };
            proc.WaitForExit(); 
            
            string header = "";
            // RECEIVE DATA
            byte[] content = new byte[0];
            {
                List<byte> resvd = new List<byte>();                
                int hi = 0;
                while (!proc.StandardOutput.EndOfStream)
                {
                    int b = proc.StandardOutput.Read();
                    if ((resvd.Count == 0) && (b == 10)) { hi = 1; }; // no header
                    if ((resvd.Count == 0) && (b == 60)) { resvd.Add(10); hi = 1; }; // no header
                    if (b >= 0)
                    {
                        if (hi == 0)
                        {
                            header += (char)b;
                            if (header.Length > 0)
                            {
                                int hend = header.IndexOf("\n\n");
                                if (hend > 0) { hi = hend + 2; };
                                hend = header.IndexOf("\r\n\r\n");
                                if (hend > 0) { hi = hend + 4; };
                            };
                        };
                        resvd.Add((byte)b);
                    }
                    else
                        break;
                };                
                
                if (resvd.Count > header.Length)
                {
                    content = new byte[resvd.Count - hi];
                    Array.Copy(resvd.ToArray(), hi, content, 0, content.Length);                    
                };
            };                       

            return new Response(header, content);
        }

        public static Response Call(string path, byte[] postBody)
        {
            return CallBin(path, postBody, null, null, null);
        }

        public static Response Call(string path, byte[] postBody, IDictionary<string, string> parameters)
        {
            return CallBin(path, postBody, parameters, null, null);
        }

        public static Response Call(string path, byte[] postBody, IDictionary<string, object> parameters)
        {
            return CallBin(path, postBody, null, parameters, null);
        }

        public static Response Call(string path, byte[] postBody, System.Collections.Specialized.NameValueCollection parameters)
        {
            return CallBin(path, postBody, null, null, parameters);
        }

        public static void Test()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            parameters.Add("QUERY_STRING", 
                "path=none"+
                "&test=empty"+
                "&var=a"+
                "&dd=яков"
                );

            byte[] post = System.Text.Encoding.UTF8.GetBytes(
                "test=post"+
                "&codepage=utf-8"+
                "&lang=ru"+
                "&name=" + System.Uri.EscapeDataString("яковинка")
                );

            Response resp = CGIBINCaller.Call(System.Reflection.Assembly.GetExecutingAssembly().Location, post, parameters);
            
            Console.WriteLine("========================================================================");
            Console.Write(resp.Header);
            Console.OutputEncoding = System.Text.Encoding.GetEncoding(866);
            Console.Write(resp.Body);
            Console.WriteLine();
            Console.WriteLine("========================================================================");

            System.IO.FileStream fs = new System.IO.FileStream(System.Reflection.Assembly.GetExecutingAssembly().Location + "_header.txt", System.IO.FileMode.Create, System.IO.FileAccess.Write);
            if (resp.Header.Length > 0)
                fs.Write(System.Text.Encoding.ASCII.GetBytes(resp.Header), 0, resp.Header.Length);
            fs.Close();

            fs = new System.IO.FileStream(System.Reflection.Assembly.GetExecutingAssembly().Location + "_body.txt", System.IO.FileMode.Create, System.IO.FileAccess.Write);
            if ((resp.Content != null) && (resp.Content.Length > 0))
                fs.Write(resp.Content, 0, resp.Content.Length);
            fs.Close();
        }

    }
}
