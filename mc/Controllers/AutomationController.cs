﻿using DecisionServicePrivateWeb.Classes;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Research.MultiWorldTesting.Contract;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.ApplicationInsights;
using System.Xml.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Experimentation;
using System.IO;
using System.Text;
using System.Net;

namespace DecisionServicePrivateWeb.Controllers
{
    [RequireHttps]
    public class AutomationController : Controller
    {
        [HttpGet]
        public async Task UpdateSettings(string trainArguments = null, string byomtrainArguments = null, float? initialExplorationEpsilon = null, bool? isExplorationEnabled = null)
        {
            var token = Request.Headers["auth"];
            if (token != ConfigurationManager.AppSettings[ApplicationMetadataStore.AKPassword])
                throw new UnauthorizedAccessException();

            var telemetry = new TelemetryClient();
            try
            {
                telemetry.TrackTrace($"UpdateSettings(trainArguments={trainArguments}, byomtrainArguments=${byomtrainArguments}, initialExplorationEpsilon={initialExplorationEpsilon}, isExplorationEnabled={isExplorationEnabled})");

                string azureStorageConnectionString = ConfigurationManager.AppSettings[ApplicationMetadataStore.AKConnectionString];
                var storageAccount = CloudStorageAccount.Parse(azureStorageConnectionString);
                var blobClient = storageAccount.CreateCloudBlobClient();
                var settingsBlobContainer = blobClient.GetContainerReference(ApplicationBlobConstants.SettingsContainerName);

                var blob = settingsBlobContainer.GetBlockBlobReference(ApplicationBlobConstants.LatestClientSettingsBlobName);
                ApplicationClientMetadata clientMeta;
                if (await blob.ExistsAsync())
                    clientMeta = JsonConvert.DeserializeObject<ApplicationClientMetadata>(await blob.DownloadTextAsync());
                else
                    clientMeta = new ApplicationClientMetadata();

                if (trainArguments != null)
                    clientMeta.TrainArguments = trainArguments;

                if (byomtrainArguments != null)
                    clientMeta.BYOMTrainArguments = byomtrainArguments;

                if (initialExplorationEpsilon != null)
                    clientMeta.InitialExplorationEpsilon = (float)initialExplorationEpsilon;

                if (isExplorationEnabled != null)
                    clientMeta.IsExplorationEnabled = (bool)isExplorationEnabled;

                await blob.UploadTextAsync(JsonConvert.SerializeObject(clientMeta));
            }
            catch (Exception e)
            {
                telemetry.TrackException(e);
            }
        }

        [HttpGet]
        public string AppSettings()
        {
            var token = Request.Headers["auth"];
            if (token != ConfigurationManager.AppSettings[ApplicationMetadataStore.AKPassword])
                throw new UnauthorizedAccessException();

            return new XElement("appSettings",
                ConfigurationManager.AppSettings.AllKeys
                    .Where(key => !key.Contains(":")) // filter special ASP parameters
                    .Select(key => 
                        new XElement("add", 
                            new XAttribute("key", key), 
                            new XAttribute("value", ConfigurationManager.AppSettings[key])))
                ).ToString();
        }

        [HttpGet]
        public async Task<ActionResult> Offline(string startTimeInclusive, string endTimeExclusive, string dataFormat = "json")
        {
            var token = Request.Headers["auth"];
            if (token != ConfigurationManager.AppSettings[ApplicationMetadataStore.AKPassword])
                throw new UnauthorizedAccessException();

            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings[ApplicationMetadataStore.AKConnectionString]);

            var blobClient = storageAccount.CreateCloudBlobClient();

            StreamWriter responseWriter = null;
            switch (dataFormat)
            {
                case "json":
                    responseWriter = new StreamWriter(Response.OutputStream, Encoding.UTF8);
                    break;
                case "vw":
                    var settingsBlobContainer = blobClient.GetContainerReference(ApplicationBlobConstants.SettingsContainerName);
                    var blob = settingsBlobContainer.GetBlockBlobReference(ApplicationBlobConstants.LatestClientSettingsBlobName);
                    if (!await blob.ExistsAsync())
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Application settings blob not found.");
                    }
                    ApplicationClientMetadata clientMeta = JsonConvert.DeserializeObject<ApplicationClientMetadata>(await blob.DownloadTextAsync());
                    responseWriter = new VowpalWabbitStreamWriter(Response.OutputStream, Encoding.UTF8, clientMeta.TrainArguments);
                    break;
                default:
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Unrecognized data format.");
            }

            using (responseWriter)
            {
                await AzureBlobDownloader.Download(
                    storageAccount,
                    DateTime.ParseExact(startTimeInclusive, "yyyyMMddHHmm", System.Globalization.CultureInfo.InvariantCulture),
                    DateTime.ParseExact(endTimeExclusive, "yyyyMMddHHmm", System.Globalization.CultureInfo.InvariantCulture),
                    responseWriter).ConfigureAwait(false);
            }

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
    }
}