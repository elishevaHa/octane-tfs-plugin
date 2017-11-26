﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Flurl;
using MicroFocus.Ci.Tfs.Octane.Tfs.Beans;
using MicroFocus.Ci.Tfs.Octane.Tools;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using System.Net.Http;
using System.Net.Http.Headers;
using Nancy.Json;
using Newtonsoft.Json;
using MicroFocus.Ci.Tfs.Octane.Tfs.ApiItems;

namespace MicroFocus.Ci.Tfs.Octane.Tfs
{
    public abstract class TfsManagerBase
    {
        private readonly TfsConfigurationServer _configurationServer;
        private const string TfsUrl = "http://localhost:8080/tfs/";
        private readonly string _pat;
        private readonly Uri _tfsUri;

        protected TfsManagerBase(string pat)
        {
            _tfsUri = new Uri(TfsUrl);
            _pat = pat;

            _configurationServer =
                TfsConfigurationServerFactory.GetConfigurationServer(_tfsUri);
        }

        protected List<TfsCollectionItem> GetCollections()
        {

            var visualStudioServicesConnection = new VssConnection(_tfsUri, new PatCredentials(string.Empty, _pat));

            // get ahold of the Project Collection client
            var projectCollectionHttpClient = visualStudioServicesConnection.GetClient<ProjectCollectionHttpClient>();
            var result = new List<TfsCollectionItem>();
            // iterate over the first 10 Project Collections (I am allowed to see)
            // however, if no parameter(s) were provided to the .GetProjectCollections() method, it would only retrieve one Collection,
            // so basically this allows / provides fine-grained pagination control
            foreach (var projectCollectionReference in projectCollectionHttpClient.GetProjectCollections().Result)
            {
                // retrieve a reference to the actual project collection based on its (reference) .Id
                var projectCollection = projectCollectionHttpClient.GetProjectCollection(projectCollectionReference.Id.ToString()).Result;

                // the 'web' Url is the one for the PC itself, the API endpoint one is different, see below
                var webUrlForProjectCollection = projectCollection.Links.Links["web"] as ReferenceLink;

                if (webUrlForProjectCollection != null)
                    Trace.WriteLine(
                        $"Project Collection '{projectCollection.Name}' (Id: {projectCollection.Id}) at Web Url: '{webUrlForProjectCollection.Href}' & API Url: '{projectCollection.Url}'");

                result.Add(new TfsCollectionItem(projectCollection.Id, projectCollection.Name));

            }

            return result;

            //            ReadOnlyCollection<CatalogNode> collectionNodes = _configurationServer.CatalogNode.QueryChildren(
            //                new[] {CatalogResourceTypes.ProjectCollection},
            //                false, CatalogQueryOptions.None);
            //
            //            var result = new List<TfsCollectionItem>();
            //            foreach (CatalogNode collectionNode in collectionNodes)
            //            {
            //
            //                // Use the InstanceId property to get the team project collection
            //                Guid collectionId = new Guid(collectionNode.Resource.Properties["InstanceId"]);
            //                TfsTeamProjectCollection teamProjectCollection =
            //                    _configurationServer.GetTeamProjectCollection(collectionId);
            //
            //                // Print the name of the team project collection
            //                Trace.WriteLine("Found Collection: " + teamProjectCollection.Name);
            //
            //                result.Add(new TfsCollectionItem(teamProjectCollection.InstanceId, teamProjectCollection.Name));
            //            }
            //
            //            return result;
        }

        protected List<TfsProjectItem> GetProjects(TfsCollectionItem collection)
        {
            return GetProjects(collection.Name);
        }

        protected List<TfsProjectItem> GetProjects(string collectionName)
        {
            var collectionUri = new Uri(Url.Combine(_tfsUri.ToString(), collectionName));
            VssConnection collectionVssConnection = new VssConnection(collectionUri, new PatCredentials(string.Empty, _pat));
            var projectHttpClient = collectionVssConnection.GetClient<ProjectHttpClient>();

            var result = new List<TfsProjectItem>();
            foreach (var projectReference in projectHttpClient.GetProjects().Result)
            {
                // and then get ahold of the actual project
                var teamProject = projectHttpClient.GetProject(projectReference.Id.ToString()).Result;
                var urlForTeamProject = ((ReferenceLink)teamProject.Links.Links["web"]).Href;

                Trace.WriteLine(
                    $"Team Project '{teamProject.Name}' (Id: {teamProject.Id}) at Web Url: '{urlForTeamProject}' & API Url: '{teamProject.Url}'");

                result.Add(new TfsProjectItem(teamProject.Id, teamProject.Name));
            }

            return result;
        }

        protected List<TfsBuildDefenitionItem> GetBuildDefenitions(TfsCollectionItem collection, TfsProjectItem project)
        {
            return GetBuildDefenitions(collection.Name, project.Name);
        }

        protected List<TfsBuildDefenitionItem> GetBuildDefenitions(string collectionName, string projectName)
        {
            var uri = _tfsUri.Append(collectionName);
            var buildClient = new BuildHttpClient(uri, new PatCredentials(string.Empty, _pat));
            var definitions = buildClient.GetDefinitionsAsync(project: projectName);
            var result = new List<TfsBuildDefenitionItem>();
            foreach (var buildDefenition in definitions.Result)
            {
                Trace.WriteLine(buildDefenition.Name);

                result.Add(new TfsBuildDefenitionItem(buildDefenition.Id.ToString(), buildDefenition.Name));

            }

            return result;

        }


        private T GetResult<T>(String urlSuffix)
        {
            //encode your personal access token                   
            string credentials = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", _pat)));


            //use the httpclient
            using (var client = new HttpClient())
            {
                client.BaseAddress = _tfsUri;
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                HttpResponseMessage response = client.GetAsync(urlSuffix, HttpCompletionOption.ResponseContentRead).Result;

                //check to see if we have a succesfull respond
                string content = response.Content.ReadAsStringAsync().Result;
                if (response.IsSuccessStatusCode)
                {
                    T result = JsonConvert.DeserializeObject<T>(content);
                    return result;
                }
                else
                {
                    String msg = $"Failed to get result {urlSuffix} : {content})";
                    Trace.WriteLine(msg);
                    throw new Exception(msg);

                }
            }
        }

        private TfsBuild GetBuild(string collectionName, string projectName, long buildId)
        {
            string uriSuffix = ($"{collectionName}/{projectName}/_apis/build/builds/{buildId}?api-version=1.0");
            TfsBuild build = GetResult<TfsBuild>(uriSuffix);
            return build;
        }

        private TfsRun GetRunByBuildUri(string collectionName, string projectName, string buildUri)
        {
            string uriSuffix = ($"{collectionName}/{projectName}/_apis/test/runs?api-version=1.0&buildUri={buildUri}");
            TfsRuns runs = GetResult<TfsRuns>(uriSuffix);
            return runs.Results.Count > 0 ? runs.Results[0] : null;
        }

        public TfsTestResults getTestResults(string collectionName, string projectName, int buildId)
        {
            TfsBuild build = GetBuild(collectionName, projectName, buildId);
            TfsRun run = GetRunByBuildUri(collectionName, projectName, build.Uri);
            string uriSuffix = ($"{collectionName}/{projectName}/_apis/test/runs/{run.Id}?api-version=1.0");
            TfsTestResults results = GetResult<TfsTestResults>(uriSuffix);
            return results;
        }
    }
}
