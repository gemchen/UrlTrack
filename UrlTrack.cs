using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using SHDocVw;
using mshtml;

namespace UrlTrack
{
    [
    ComVisible(true),
    Guid("e90da13b-117a-4178-8111-0f712da09ff9"),
    ClassInterface(ClassInterfaceType.None)
    ]

    class VisitHist
    {
        private DateTime startTime;
        public DateTime StartTime
        {
            get
            {
                return startTime;
            }
            set
            {
                startTime = value;
            }
        }

        private DateTime endTime;
        public DateTime EndTime
        {
            get
            {
                return endTime;
            }
            set
            {
                endTime = value;
            }
        }

        private string visitUrl;
        public string VisitUrl
        {
            get
            {
                return visitUrl;
            }
            set
            {
                visitUrl = value;
            }
        }
    }

    public class UrlTrack : IObjectWithSite
    {
        private WebBrowser webBrowser;
        private List<VisitHist> visitHists = new List<VisitHist>();

        private void webBrowser_NavigateComplete2(object pDisp, ref object URL)
        {
            string url = URL as string;
            if (url.IndexOf("about:blank") >= 0)
            {
                return;
            }
            if (visitHists.Count > 0)
            {
                VisitHist currentHist = visitHists[visitHists.Count - 1];
                if (currentHist.VisitUrl != url)
                {
                    currentHist.EndTime = System.DateTime.Now;
                }
                else
                {
                    return;
                }
            }
            VisitHist newHist = new VisitHist();
            newHist.StartTime = System.DateTime.Now;
            newHist.VisitUrl = url;
            visitHists.Add(newHist);
        }

        private void webBrowser_OnQuit()
        {
            //System.Diagnostics.Debugger.Break();
            if (visitHists.Count > 0)
            {
                VisitHist currentHist = visitHists[visitHists.Count - 1];
                currentHist.EndTime = System.DateTime.Now;
            }

            // Write visit histories to log file
            System.Diagnostics.Debug.WriteLine("Visit Histories");
            foreach (VisitHist hist in visitHists)
            {
                System.Diagnostics.Debug.WriteLine(hist.EndTime - hist.StartTime + " | " + hist.StartTime + " | " + hist.EndTime + " | " + hist.VisitUrl);
            }
        }

        #region IObjectWithSite Members

        public int SetSite(object site)
        {
            if (site != null)
            {
                webBrowser = site as WebBrowser;
                if (webBrowser != null)
                {
                    webBrowser.NavigateComplete2 += new DWebBrowserEvents2_NavigateComplete2EventHandler(webBrowser_NavigateComplete2);
                    webBrowser.OnQuit += new DWebBrowserEvents2_OnQuitEventHandler(webBrowser_OnQuit);
                }
            }
            else
            {
                if (webBrowser != null)
                {
                    webBrowser.NavigateComplete2 -= new DWebBrowserEvents2_NavigateComplete2EventHandler(webBrowser_NavigateComplete2);
                    webBrowser.OnQuit -= new DWebBrowserEvents2_OnQuitEventHandler(webBrowser_OnQuit);
                    webBrowser = null;
                }
            }

            return 0;
        }

        public int GetSite(ref Guid guid, out IntPtr ppvSite)
        {
            IntPtr punk = Marshal.GetIUnknownForObject(webBrowser);
            int hr = Marshal.QueryInterface(punk, ref guid, out ppvSite);
            Marshal.Release(punk);

            return hr;
        }

        #endregion    // IObjectWithSite Members

        #region Com Register/UnRegister Methods

        public static string BHO_KEY_NAME = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects";

        [ComRegisterFunction]
        public static void RegisterBHO(Type type)
        {
            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(BHO_KEY_NAME, true);
            if (registryKey == null)
            {
                registryKey = Registry.LocalMachine.CreateSubKey(BHO_KEY_NAME);
            }

            string guid = type.GUID.ToString("B");
            RegistryKey bhoKey = registryKey.OpenSubKey(guid, true);
            if (bhoKey == null)
            {
                bhoKey = registryKey.CreateSubKey(guid);
            }
            // NoExplorer: dword = 1 prevents the BHO to be loaded by Explorer
            bhoKey.SetValue("NoExplorer", 1);
            bhoKey.Close();

            registryKey.Close();
        }

        [ComUnregisterFunction]
        public static void UnregisterBHO(Type type)
        {
            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(BHO_KEY_NAME, true);
            string guid = type.GUID.ToString("B");

            if (registryKey != null)
                registryKey.DeleteSubKey(guid, false);
        }

        #endregion    // Com Register/UnRegister Methods
    }
}
