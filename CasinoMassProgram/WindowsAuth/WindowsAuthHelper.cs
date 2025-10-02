using System.DirectoryServices;
using System.Runtime.InteropServices;

namespace CasinoMassProgram.WindowsAuth
{
    public static class WindowsAuthHelper
    {
        public static string NormalizeReturnUrl(string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
                return "/";

            returnUrl = returnUrl.Replace("\\", "/");

            if (!returnUrl.StartsWith("/"))
                returnUrl = "/" + returnUrl;

            return returnUrl;
        }


        public static int WindowsAccount(string username, string password)
        {
            using (DirectoryEntry entry = new DirectoryEntry())
            {
                entry.Username = username;
                entry.Password = password;

                DirectorySearcher searcher = new DirectorySearcher(entry);

                searcher.Filter = "(objectclass=user)";
                try
                {
                    SearchResult sr = searcher.FindOne();
                    if (sr != null)
                    {

                        return 1;
                    }
                    else
                        return 0;

                }
                catch (COMException ex)
                {
                    return -2;
                }
            }
        }
    }
}