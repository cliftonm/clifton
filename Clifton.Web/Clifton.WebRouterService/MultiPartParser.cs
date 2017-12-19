using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Clifton.Core.ExtensionMethods;

/* Example:

------WebKitFormBoundaryF1u8pcziAV47S3Ux
Content-Disposition: form-data; name="project.txt"; filename="foo.txt"
Content-Type: text/plain

bar
------WebKitFormBoundaryF1u8pcziAV47S3Ux--

*/

namespace Clifton.WebRouterService
{
    // For lack of a better place to put this right now.
    // TODO: Lots of missing implementation!
    public static class MultiPartParser
    {
        public enum ContentType
        { 
            Unknown,
            TextPlain,
        }

        public const string MULTIPART_START = "------WebKitFormBoundary";
        public const string MULTIPART_END = "------WebKitFormBoundary";
        public const string CONTENT_TYPE = "Content-Type: ";
        public const string TEXT_PLAIN = "text/plain";
        public const string CRLF = "\r\n";

        public static bool IsMultiPart(string data)
        {
            return data.StartsWith(MULTIPART_START);
        }

        public static ContentType GetContentType(string data)
        {
            ContentType ret = ContentType.Unknown;
            string ct = data.RightOf(CONTENT_TYPE).LeftOf(CRLF);

            switch (ct)
            {
                case TEXT_PLAIN:
                    ret = ContentType.TextPlain;
                    break;
            }

            return ret;
        }

        public static string GetContent(string data)
        {
            return data.RightOf(CONTENT_TYPE).RightOf(CRLF).RightOf(CRLF).LeftOf(MULTIPART_END).LeftOfRightmostOf("\r\n");
        }
    }
}
