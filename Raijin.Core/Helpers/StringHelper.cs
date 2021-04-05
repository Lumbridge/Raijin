using Hl7.Fhir.Model;
using Newtonsoft.Json;
using System;
using System.Xml.Linq;

namespace Raijin.Core.Helpers
{
    public static class StringHelper
    {
        public static int GetSubStringCount(this string source)
        {
            int count = 0, n = 0;
            var substring = "[]";

            if (substring != "")
            {
                while ((n = source.IndexOf(substring, n, StringComparison.InvariantCulture)) != -1)
                {
                    n += substring.Length;
                    ++count;
                }
            }

            return count;
        }

        public static int GetIndexOfNthOccurence(this string source, string match, int occurence)
        {
            var i = 1;
            var index = 0;

            while (i <= occurence && (index = source.IndexOf(match, index + 1, StringComparison.Ordinal)) != -1)
            {
                if (i == occurence)
                    return index;
                i++;
            }

            return -1;
        }

        public static CodeableConcept ParseOpenEhrCcString(string ccString)
        {
            var ccParts = ccString.Split(new[] { "::" }, StringSplitOptions.None);

            switch (ccParts.Length)
            {
                case 1:
                    return new CodeableConcept("", "", ccParts[0]);
                case 2:
                    return new CodeableConcept(ccParts[0], ccParts[1]);
                case 3:
                    return new CodeableConcept(ccParts[0], ccParts[1], ccParts[2]);
            }

            return null;
        }

        public static string FormatXml(string xml)
        {
            try
            {
                XDocument doc = XDocument.Parse(xml);
                return doc.ToString();
            }
            catch (Exception)
            {
                // Handle and throw if fatal exception here; don't just ignore them
                return xml;
            }
        }

        public static string FormatJson(string text)
        {
            return JsonConvert.SerializeObject(JsonConvert.DeserializeObject(text), Formatting.Indented);
        }
    }
}
