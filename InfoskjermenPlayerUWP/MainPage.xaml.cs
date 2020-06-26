using System;
using System.Diagnostics;
using System.Threading;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;
using Windows.ApplicationModel;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.System.Profile;
using Windows.Storage;

namespace InfoskjermenPlayerUWP
{

    public sealed partial class MainPage : Page
    {
        private const string DefaultHost = "http://app.infoskjermen.no";
        private const string LocalHost = "http://10.0.1.10:3000";
        private const string DevHost = "http://dev.infoskjermen.no";
        private String host;
        private ApplicationDataContainer localSettings;

        public MainPage()
        {
            this.InitializeComponent();

            Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated += AcceleratorKeyActivated;
            webView.DOMContentLoaded += webView_DOMContentLoaded;
            webView.ScriptNotify += WebView_ScriptNotify;

            localSettings = ApplicationData.Current.LocalSettings;
           
            host = (string) localSettings.Values["Host"];
            if (host == null)
            {
                localSettings.Values["Host"] = DefaultHost;
                host = (string) localSettings.Values["Host"];
            }
            Debug.WriteLine(host);

            WebView_load_go();
        }

        private void WebView_ScriptNotify(object sender, NotifyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("from webview: " + e.Value);
            Debug.WriteLine("Denne funskjone er kalt");

        }
            

        private void AcceleratorKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
        {
            if (args.EventType.ToString().Contains("Down"))
            {
                var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
                if (ctrl.HasFlag(CoreVirtualKeyStates.Down))
                {
                    switch (args.VirtualKey)
                    {
                        case VirtualKey.J: //Open change host modal
                            System.Diagnostics.Debug.WriteLine(args.VirtualKey);
                            _ = Popup.IsOpen == false ? Popup.IsOpen = true : Popup.IsOpen = false;
                            break;
                        case VirtualKey.U: //Unpair
                            System.Diagnostics.Debug.WriteLine(args.VirtualKey);
                            webView.Navigate(new Uri(host + "/unpair"));
                            break;
                        case VirtualKey.R: //Refresh
                            System.Diagnostics.Debug.WriteLine(args.VirtualKey);
                            webView.Navigate(new Uri(host + "/go"));
                            break;
                    }
                }
            }
        }

        
        private async void webView_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args)
        {
            // Show status.
            if (args.Uri != null)
            {
                Canvas.SetZIndex(webView, 1000);

                String script = String.Format("window.localStorage.physical_id");
                var result = await webView.InvokeScriptAsync("eval", new string[] { script });
                Debug.WriteLine(result);
                Send_device_info(result);
            }
        }
        

        private void Button_Go(object sender, RoutedEventArgs e)
        {
            string userhost = Modal_TextBox.Text;
            localSettings.Values["Host"] = "http://" + userhost;
            webView.Navigate(new Uri("http://" + userhost + "/go"));
            Popup.IsOpen = false;
        }

        private void Button_Close(object sender, RoutedEventArgs e)
        {
            _ = Popup.IsOpen == false ? Popup.IsOpen = true : Popup.IsOpen = false;
        }

        public async void WebView_load_go()
        {
            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();
            var headers = httpClient.DefaultRequestHeaders;
            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            String httpResponsBody;
            try
            {
                httpResponse = await httpClient.GetAsync(new Uri(DefaultHost + "/up"));
                httpResponse.EnsureSuccessStatusCode();
                httpResponsBody = await httpResponse.Content.ReadAsStringAsync();
                Debug.WriteLine(httpResponse.ToString());
                Debug.WriteLine(httpResponse.StatusCode);
                if (httpResponse.StatusCode.ToString() == "Ok")
                {
                    webView.Navigate(new Uri(host + "/go"));
                    string[] arg = { "Lets Go!" };
                    string returnvalue = await splash.InvokeScriptAsync("setMessage", arg);
                    Debug.WriteLine(returnvalue);
                }
                else
                {     
                    string[] args = { "Reconnecting.." };
                    string returnValue = await splash.InvokeScriptAsync("setMessage", args);
                    Debug.WriteLine("No internet..");
                    Thread.Sleep(5000);
                    WebView_load_go();
                }
            }
            catch (Exception e)
            {
                httpResponsBody = "Error: " + e.HResult.ToString("X") + "Message: " + e.Message;
                Debug.WriteLine(httpResponsBody);
            }
        }

        
        private async void Send_device_info(String physicalID)
        {
            // get the system family name
            AnalyticsVersionInfo ai = AnalyticsInfo.VersionInfo;
            String SystemFamily = ai.DeviceFamily;

            // get the system version number
            string sv = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
            ulong v = ulong.Parse(sv);
            ulong v1 = (v & 0xFFFF000000000000L) >> 48;
            ulong v2 = (v & 0x0000FFFF00000000L) >> 32;
            ulong v3 = (v & 0x00000000FFFF0000L) >> 16;
            ulong v4 = (v & 0x000000000000FFFFL);
            String SystemVersion = $"{v1}.{v2}.{v3}.{v4}";

            // get the package architecure
            Package package = Package.Current;
            String SystemArchitecture = package.Id.Architecture.ToString();

            // get the user friendly app name
            String ApplicationName = package.DisplayName;

            // get the app version
            PackageVersion pv = package.Id.Version;
            String ApplicationVersion = $"{pv.Major}.{pv.Minor}.{pv.Build}.{pv.Revision}";

            // get the device manufacturer and model name
            EasClientDeviceInformation eas = new EasClientDeviceInformation();
            String DeviceManufacturer = eas.SystemManufacturer;
            String DeviceModel = eas.SystemProductName;

            try
            {
                // Construct the HttpClient and Uri. This endpoint is for test purposes only.
                HttpClient httpClient = new HttpClient();
                Uri uri = new Uri("http://dev.infoskjermen.no/listeners/set_options/"+ physicalID + "/");

                // Construct the JSON to post.
                HttpStringContent content = new HttpStringContent(
                    "{ \"options\": {\"Host\": \""+ DefaultHost + "\", " +
                    "\"SystemFamily\": \"" + SystemFamily + "\", " +
                    "\"SystemVersion\": \"" + SystemVersion + "\", " +
                    "\"SystemArchitecture\": \"" + SystemArchitecture + "\", " +
                    "\"ApplicationName\": \"" + ApplicationName + "\", " +
                    "\"ApplicationVersion\": \"" + ApplicationVersion + "\", " +
                    "\"DeviceManufacturer\": \"" + DeviceManufacturer + "\", " +
                    "\"DeviceModel\": \"" + DeviceModel + "\"} }",
                    Windows.Storage.Streams.UnicodeEncoding.Utf8,
                    "application/json");

                // Post the JSON and wait for a response.
                HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(
                    uri,
                    content);

                // Make sure the post succeeded, and write out the response.
                httpResponseMessage.EnsureSuccessStatusCode();
                Debug.WriteLine(httpResponseMessage);
                var httpResponseBody = await httpResponseMessage.Content.ReadAsStringAsync();
                Debug.WriteLine(httpResponseBody);
            }
            catch (Exception ex)
            {
                // Write out any exceptions.
                Debug.WriteLine(ex);
            }

        }
        
    }
}
