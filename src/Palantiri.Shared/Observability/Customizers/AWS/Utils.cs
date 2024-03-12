using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palantiri.Shared.Observability.Customizers.AWS
{

    internal class Utils
    {
        internal static object GetTagValue(Activity activity, string tagName)
        {
            foreach (KeyValuePair<string, object> tagObject in activity.TagObjects)
            {
                if (tagObject.Key.Equals(tagName))
                {
                    return tagObject.Value;
                }
            }

            return null;
        }

        internal static string RemoveSuffix(string originalString, string suffix)
        {
            if (string.IsNullOrEmpty(originalString))
            {
                return string.Empty;
            }

            if (originalString.EndsWith(suffix))
            {
                return originalString.Substring(0, originalString.Length - suffix.Length);
            }

            return originalString;
        }

        internal static string RemoveAmazonPrefixFromServiceName(string serviceName)
        {
            return RemovePrefix(RemovePrefix(serviceName, "Amazon"), ".");
        }

        private static string RemovePrefix(string originalString, string prefix)
        {
            if (string.IsNullOrEmpty(originalString))
            {
                return string.Empty;
            }

            if (originalString.StartsWith(prefix))
            {
                return originalString.Substring(prefix.Length);
            }

            return originalString;
        }
    }
}
