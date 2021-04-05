using Hl7.Fhir.Model;
using Raijin.Core.CompositePattern;
using System.Collections.Generic;
using System.Text;
using Raijin.Core.Adapters.FhirToOpenEhr;

namespace Raijin.Core.Classes
{
    public static class FhirCompositeParser
    {
        public static Composite Parse(string rootName, Bundle fhirBundle)
        {
            var root = new Composite(rootName + "_rootnode");

            // add entry root
            var adapter = FhirToOpenEhrAdapterBase.GetRequiredAdapter(fhirBundle.Entry[0].Resource.TypeName);
            var entryRoot = new Composite(adapter.MappedTermsDictionary["Entry"] + ":0");
            root.Add(entryRoot);

            // add resources
            foreach (var entry in fhirBundle.Entry)
            {
                var resourceCount = root.FindMany(adapter.MappedTermsDictionary[entry.Resource.TypeName], new List<Component>()).Count;
                entryRoot.Add(adapter.Convert(entry, resourceCount));
            }

            return root;
        }
    }
}
