using Raijin.Core.Adapters.OpenEhrToFhir;
using Raijin.Core.CompositePattern;
using Raijin.Core.Helpers;
using Raijin.Core.Interfaces;

namespace Raijin.Core.Classes
{
    public class MessageHandler : IMessageHandler
    {
        //private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public Composite MessageToMemoryModel(string message)
        {
            return OpenEhrParser.Parse("root", message);
        }

        public OpenEhrToFhirAdapterBase ProcessMessage(string message)
        {
            // get all classes which inherit OpenEhrToFhirAdapterBase
            // (this gets all adapters and passes the message into the adapter constructor which automatically processes the message using that adapter)
            var adapters = ReflectionHelper.GetEnumerableOfType<OpenEhrToFhirAdapterBase>(message);

            // set default highest confidence
            var highestConfidence = 0.0;

            // set default chosenOpenEhrToFhirAdapter
            OpenEhrToFhirAdapterBase chosenOpenEhrToFhirAdapter = null;

            // loop through all adapters
            foreach (var adapter in adapters)
            {
                // check if the confidence score of this adapter is higher than the current
                if (adapter.ConfidenceScore > highestConfidence || chosenOpenEhrToFhirAdapter == null)
                {
                    // set the current highest confidence score
                    highestConfidence = adapter.ConfidenceScore;
                    // set the current adapter 
                    chosenOpenEhrToFhirAdapter = adapter;
                }
            }

            // log some info
            //Log.Info($"Received message and selected adapter: {chosenOpenEhrToFhirAdapter?.AdapterName} with confidence of {chosenOpenEhrToFhirAdapter?.ConfidenceScore}%.\n");

            // return the chosen adapter
            return chosenOpenEhrToFhirAdapter;
        }
    }

}
