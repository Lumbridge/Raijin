using System;
using Raijin.Core.Classes;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;

namespace Raijin.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            OpenEhrToFhirExampleMethod();
        }

        public static void OpenEhrToFhirExampleMethod()
        {
            //Log.Info("Started OpenEHR/FHIR testing console application.\n");

            // message handler instantiated to handle the incoming message
            var messageHandler = new MessageHandler();

            // test message coming into the system
            var message = Constants.NWISAdverseReactionsList;

            //OpenEhrParser.Parse("test", message).DisplayFlatFile();

            // message handler processes the message
            var result = messageHandler.ProcessMessage(message);

            //result.OpenEhrRecord.Display();

            //Console.WriteLine(OpenEhrSerializer.Serialize(result.OpenEhrRecord));

            result.OpenEhrRecord.DisplayFlatFile();

            System.Console.WriteLine("");

            result.DisplayFhirResult(ResourceFormat.Json);

            //new MessageHandler().ProcessMessage(Constants.OpenEhrRecordA); // process record A example
            //new MessageHandler().ProcessMessage(Constants.OpenEhrRecordB).DisplayFhirResult(); // process record B example
        }

        //public static void FhirToOpenEhrExampleMethod()
        //{
        //    // convert fhir string into fhir memory model
        //    //
        //    var parsedRecord = new FhirXmlParser().Parse(Constants.FhirRecordB);
        //    var result = FhirCompositeParser.Parse("OehrUc007RecordReactionRecordB", (Bundle)parsedRecord);

        //    // Display Results
        //    //
        //    result.DisplayFlatFile(); // displays as openEHR flat file
        //    System.Console.WriteLine(); // make gap between results
        //    result.Display(); // displays as composite hierarchical model
        //}
    }
}
