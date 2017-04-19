//------------------------------------------------------------------------------
// <copyright>
//     Copyright (c) Microsoft Corporation. All Rights Reserved.
// </copyright>
//------------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.Research.MultiWorldTesting.Contract
{
    /// <summary>
    /// The type of decision.
    /// </summary>
    public enum DecisionType
    {
        /// <summary>
        /// Choose a single action from a fixed set of available actions.
        /// </summary>
        SingleAction = 0,

        /// <summary>
        /// Choose multiple actions from a variable set of available actions.
        /// </summary>
        MultiActions
    }

    /// <summary>
    /// The frequency with which a new model needs to be retrained.
    /// </summary>
    public enum TrainFrequency
    {
        Low = 0,
        High
    }

    public class ApplicationClientMetadata
    {
        /// <summary>
        /// The name of the application as created on the Command Center.
        /// </summary>
        public string ApplicationID { get; set; }

        /// <summary>
        /// VW-specific training arguments to be used in training service.  
        /// </summary>
        public string TrainArguments { get; set; }

        /// <summary>
        /// BYOM training arguments to be used in the training service.
        /// </summary>
        public string BYOMTrainArguments { get; set; }

        /// <summary>
        /// Method for determining if a fixed number of actions has been specified for the training service.
        /// Abstracts over VW arguments and generic arguments.
        /// </summary>
        public int? TrainingNumberOfActions
        {
            get
            {
                if (string.IsNullOrEmpty(this.BYOMTrainArguments))
                {
                    var match = Regex.Match(this.TrainArguments, @"--cb_explore\s+(?<numActions>\d+)");
                    return match.Success ? int.Parse(match.Groups["numActions"].Value) : (int?)null;
                }
                else
                {
                    Dictionary<string, string> dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(this.BYOMTrainArguments);
                    string value;

                    return dict.TryGetValue("numActions", out value) ? int.Parse(value) : (int?)null;
                }
            }
        }

        /// <summary>
        /// The EventHub connection string to which the client needs to send interaction data.
        /// </summary>
        public string EventHubInteractionConnectionString { get; set; }

        /// <summary>
        /// The EventHub connection string to which the client needs to send interaction data.
        /// </summary>
        public string EventHubObservationConnectionString { get; set; }

        /// <summary>
        /// Turn on/off exploration at client side.
        /// </summary>
        public bool IsExplorationEnabled { get; set; }

        /// <summary>
        /// The publicly accessible Uri of the model blob that clients can check for update.
        /// </summary>
        public string ModelBlobUri { get; set; }

        /// <summary>
        /// The instrumentation key of Application Insights used for diagnostics &amp; event logging.
        /// </summary>
        public string AppInsightsKey { get; set; }

        public float InitialExplorationEpsilon { get; set; }
    }

    public class ApplicationExtraMetadata
    {
        /// <summary>
        /// The Id of the Azure subscription associated with this application.
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// The name of the Azure resource group provisioned for this application.
        /// </summary>
        public string AzureResourceGroupName { get; set; }

        /// <summary>
        /// The type of decision.
        /// </summary>
        public DecisionType DecisionType { get; set; }
        
        /// <summary>
        /// The training frequency type.
        /// </summary>
        public TrainFrequency TrainFrequency { get; set; }

        /// <summary>
        /// The Id of the model to use in client library.
        /// </summary>
        public string ModelId { get; set; }

        /// <summary>
        /// The experimental unit duration for joining decisions with observations.
        /// </summary>
        public int ExperimentalUnitDuration { get; set; }

        /// <summary>
        /// First URI of the settings blob including the SAS token.
        /// </summary>
        public string SettingsTokenUri1 { get; set; }

        /// <summary>
        /// Second URI of the settings blob including the SAS token.
        /// </summary>
        public string SettingsTokenUri2 { get; set; }
    }

}
