using Raijin.Core.CompositePattern;

namespace Raijin.Core.Classes
{
    public static class OpenEhrSerializer
    {
        /// <summary>
        /// Turns a composite model into an OpenEHR Flat File.
        /// </summary>
        /// <param name="openEhrObj">The Composite Model to serialize.</param>
        /// <returns>OpenEHR Flat File as a string.</returns>
        public static string Serialize(Composite openEhrObj)
        {
            var flatFile = openEhrObj.GetFlatFile();
            return "{\n" + flatFile.Remove(flatFile.LastIndexOf(',')) + "\n}";
        }
    }
}
