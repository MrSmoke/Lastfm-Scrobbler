namespace LastfmScrobbler.Api
{
    using Models.Requests;
    using Models.Responses;
    using Resources;
    using MediaBrowser.Common.Net;
    using MediaBrowser.Model.Serialization;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LastfmScrobbler.Utils;
    using System.Collections.Generic;

    public class BaseLastfmApiClient
    {
        private const string API_VERSION = "2.0";

        private readonly IHttpClient     _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        public BaseLastfmApiClient(IHttpClient httpClient, IJsonSerializer jsonSerializer)
        {
            _httpClient     = httpClient;
            _jsonSerializer = jsonSerializer;
        }

        /// <summary>
        /// Send a POST request to the LastFM Api
        /// </summary>
        /// <typeparam name="TRequest">The type of the request</typeparam>
        /// <typeparam name="TResponse">The type of the response</typeparam>
        /// <param name="request">The request</param>
        /// <returns>A response with type TResponse</returns>
        public async Task<TResponse> Post<TRequest, TResponse>(TRequest request) where TRequest: BaseRequest where TResponse: BaseResponse
        {
            var data = request.ToDictionary();

            //Append the signature
            Helpers.AppendSignature(ref data);

            using (var stream = await _httpClient.Post(new HttpRequestOptions()
            {
                Url                   = BuildPostUrl(request.Secure),
                ResourcePool          = Plugin.LastfmResourcePool,
                CancellationToken     = CancellationToken.None,
                EnableHttpCompression = false,
            }, data))
            {
                try
                {
                    var result = _jsonSerializer.DeserializeFromStream<TResponse>(stream);

                    //Lets Log the error here to ensure all errors are logged
                    if (result.isError())
                        Plugin.Logger.Error(result.Message);

                    return result;
                }
                catch (Exception e)
                {
                    Plugin.Logger.Debug(e.Message);
                }

                return null;
            }
        }

        public async Task<TResponse> Get<TRequest, TResponse>(TRequest request) where TRequest: BaseRequest where TResponse: BaseResponse
        {
            using (var stream = await _httpClient.Get(new HttpRequestOptions()
            {
                Url                   = BuildGetUrl(request.Method, request.ToDictionary()),
                ResourcePool          = Plugin.LastfmResourcePool,
                CancellationToken     = CancellationToken.None,
                EnableHttpCompression = false,
            }))
            {
                try
                {
                    var result = _jsonSerializer.DeserializeFromStream<TResponse>(stream);

                    //Lets Log the error here to ensure all errors are logged
                    if (result.isError())
                        Plugin.Logger.Error(result.Message);

                    return result;
                }
                catch (Exception e)
                {
                    Plugin.Logger.Debug(e.Message);
                }

                return null;
            }
        }

        #region Private methods
        private string BuildGetUrl(string method, Dictionary<string, string> requestData)
        {
            var qs = Utils.Helpers.DictionaryToQueryString(requestData);

            return String.Format("{0}/{1}/?{2}&format=json", Strings.Endpoints.LastfmApi, API_VERSION, qs);
        }

        private string BuildPostUrl(bool secure = false)
        {
            if (secure)
                return String.Format("{0}/{1}/?format=json", Strings.Endpoints.LastfmApiS, API_VERSION);

            return String.Format("{0}/{1}/?format=json", Strings.Endpoints.LastfmApi, API_VERSION);
        }
        #endregion
    }
}
