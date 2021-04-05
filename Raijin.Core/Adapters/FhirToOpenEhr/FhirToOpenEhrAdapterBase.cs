using Hl7.Fhir.Model;
using Raijin.Core.CompositePattern;
using System.Collections.Generic;

namespace Raijin.Core.Adapters.FhirToOpenEhr
{
    public abstract class FhirToOpenEhrAdapterBase
    {
        /// <summary>
        /// Simple Log4Net Logger Object
        /// </summary>
        //public static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Gets the name of the current adapter.
        /// </summary>
        public string AdapterName => GetType().Name;

        /// <summary>
        /// An in-memory version of the fhir flat file record based on the composite pattern.
        /// </summary>
        public Composite FhirComposite { get; set; }

        /// <summary>
        /// List of terms to map from FHIR to OpenEHR.
        /// </summary>
        public abstract Dictionary<string, string> MappedTermsDictionary { get; }

        /// <summary>
        /// List of nodes which should be removed, any leaf nodes will be re-parented to the parent before this path.
        /// E.g. NodePrune = TextElement/Value
        /// Node: "Substance/TextElement/Value": "Latex" -> "Substance": "Latex"
        /// </summary>
        public abstract List<string> NodePruneList { get; }

        /// <summary>
        /// List of node names to skip processing.
        /// </summary>
        public abstract List<string> SkipNodeNames { get; }

        public abstract Composite Convert(Bundle.EntryComponent entry, int resourceIndex);

        public static FhirToOpenEhrAdapterBase GetRequiredAdapter(string resourceTypeName)
        {
            // TODO: add logic to dynamically select adapter based on resourceTypeName.
            return new AllergyIntoleranceAdapter();
        }
    }
}
