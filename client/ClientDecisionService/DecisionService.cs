using Microsoft.Research.MultiWorldTesting.Contract;
using Microsoft.Research.MultiWorldTesting.ExploreLibrary;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using VW;

[assembly: InternalsVisibleTo("ClientDecisionServiceTest")]

namespace Microsoft.Research.MultiWorldTesting.ClientLibrary
{
    /// <summary>
    /// Factory class.
    /// </summary>
    public static class DecisionService
    {
        private static ApplicationClientMetadata DownloadMetadata(DecisionServiceConfiguration config, ApplicationClientMetadata metaData)
        {
            if (!config.OfflineMode || metaData == null)
            {
                metaData = ApplicationMetadataUtil.DownloadMetadata<ApplicationClientMetadata>(config.SettingsBlobUri);
                if (config.LogAppInsights)
                {
                    Trace.Listeners.Add(new ApplicationInsights.TraceListener.ApplicationInsightsTraceListener(metaData.AppInsightsKey));
                }
            }

            return metaData;
        }

        public static DecisionServiceClient<TContext> CreateBYOM<TContext, T>(
            Func<Stream, ITypeInspector, bool, T> create,
            DecisionServiceConfiguration config,
            ITypeInspector typeInspector = null,
            ApplicationClientMetadata metaData = null) where T : IContextMapper<TContext, ActionProbability[]>
        {
            return new DecisionServiceClient<TContext>(
                config,
                DownloadMetadata(config, metaData),
                create(config.ModelStream, typeInspector, config.DevelopmentMode)); 
        }

        public static DecisionServiceClient<TContext> Create<TContext>(DecisionServiceConfiguration config, ITypeInspector typeInspector = null, ApplicationClientMetadata metaData = null)
        {
            return CreateBYOM<TContext, VWExplorer<TContext>>(
                (ms, ti, md) => new VWExplorer<TContext>(ms, ti, md),
                config,
                typeInspector,
                metaData);
        }

        public static DecisionServiceClient<string> CreateBYOMJson<T>(
            Func<Stream, bool, T> create,
            DecisionServiceConfiguration config,
            ApplicationClientMetadata metaData = null) where T : IContextMapper<string, ActionProbability[]>
        {
            return new DecisionServiceClient<string>(
                config,
                DownloadMetadata(config, metaData),
                create(config.ModelStream, config.DevelopmentMode)); 
        }

        public static DecisionServiceClient<string> CreateJson(DecisionServiceConfiguration config, ApplicationClientMetadata metaData = null)
        {
            return CreateBYOMJson(
                (ms, dm) => new VWJsonExplorer(ms, dm),
                config,
                metaData);
        }
    }
}
