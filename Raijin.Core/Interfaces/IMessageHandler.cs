using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raijin.Core.Adapters.OpenEhrToFhir;
using Raijin.Core.CompositePattern;

namespace Raijin.Core.Interfaces
{
    public interface IMessageHandler
    {
        /// <summary>
        /// Converts the OpenEHR message into a hierarchical memory model.
        /// </summary>
        /// <param name="message">OpenEHR Plain Text Flat File.</param>
        /// <returns>The processed message.</returns>
        Composite MessageToMemoryModel(string message);

        /// <summary>
        /// Gets the appropriate adapter for the message and processes it.
        /// </summary>
        /// <param name="message">OpenEHR Plain Text Flat File.</param>
        /// <returns>The processed message.</returns>
        OpenEhrToFhirAdapterBase ProcessMessage(string message);
    }
}
