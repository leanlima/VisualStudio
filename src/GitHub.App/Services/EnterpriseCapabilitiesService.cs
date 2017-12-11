﻿using System;
using System.ComponentModel.Composition;
using System.Net.Http;
using System.Threading.Tasks;
using GitHub.Api;
using GitHub.Primitives;
using Octokit;

namespace GitHub.Services
{
    [Export(typeof(IEnterpriseCapabilitiesService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class EnterpriseCapabilitiesService : IEnterpriseCapabilitiesService
    {
        readonly ISimpleApiClientFactory apiClientFactory;
        readonly IEnterpriseProbe probe;

        [ImportingConstructor]
        public EnterpriseCapabilitiesService(
            ISimpleApiClientFactory apiClientFactory,
            IEnterpriseProbe probe)
        {
            this.apiClientFactory = apiClientFactory;
            this.probe = probe;
        }

        public Task<EnterpriseProbeResult> Probe(Uri enterpriseBaseUrl) => probe.Probe(enterpriseBaseUrl);

        public async Task<EnterpriseLoginMethods> ProbeLoginMethods(Uri enterpriseBaseUrl)
        {
            var result = EnterpriseLoginMethods.Token;
            var client = await apiClientFactory.Create(UriString.ToUriString(enterpriseBaseUrl));
            var meta = await client.GetMetadata();

            if (meta.VerifiablePasswordAuthentication) result |= EnterpriseLoginMethods.UsernameAndPassword;

            return result;
        }

        private Uri GetLoginUrl(IConnection connection)
        {
            var oauthClient = new OauthClient(connection);
            var oauthLoginRequest = new OauthLoginRequest(ApiClientConfiguration.ClientId)
            {
                RedirectUri = new Uri(OAuthCallbackListener.CallbackUrl),
            };
            var uri = oauthClient.GetGitHubLoginUrl(oauthLoginRequest);

            // OauthClient.GetGitHubLoginUrl seems to give the wrong URL. Fix this.
            return new Uri(uri.ToString().Replace("/api/v3", ""));
        }
    }
}
