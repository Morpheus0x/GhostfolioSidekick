﻿using GhostfolioSidekick.GhostfolioAPI.Contract;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using RestSharp;
using System.Diagnostics;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
	public class RestCall
	{
		private readonly Mutex mutex = new();

		private static int _maxRetryAttempts = 5;
		private static TimeSpan _pauseBetweenFailures = TimeSpan.FromSeconds(1);
		private readonly IMemoryCache memoryCache;
		private readonly IRestClient restClient;
		private readonly ILogger<RestCall> logger;
		private readonly string url;
		private readonly string accessToken;
		private readonly RetryPolicy<RestResponse> retryPolicy;
		private readonly CircuitBreakerPolicy<RestResponse> basicCircuitBreakerPolicy;

		public RestCall(
			IRestClient restClient,
			IMemoryCache memoryCache,
			ILogger<RestCall> logger,
			string url,
			string accessToken)
		{
			if (string.IsNullOrEmpty(url))
			{
				throw new ArgumentException($"'{nameof(url)}' cannot be null or empty.", nameof(url));
			}

			this.memoryCache = memoryCache;
			this.restClient = restClient ?? throw new ArgumentNullException(nameof(restClient));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.url = url;
			this.accessToken = accessToken;

			retryPolicy = Policy
				.HandleResult<RestResponse>(x =>
				!x.IsSuccessful
				&& x.StatusCode != System.Net.HttpStatusCode.Forbidden
				&& x.StatusCode != System.Net.HttpStatusCode.BadRequest)
				.WaitAndRetry(_maxRetryAttempts, x => _pauseBetweenFailures, (iRestResponse, timeSpan, retryCount, context) =>
				{
					logger.LogDebug($"The request failed. HttpStatusCode={iRestResponse.Result.StatusCode}. Waiting {timeSpan} seconds before retry. Number attempt {retryCount}. Uri={iRestResponse.Result.ResponseUri};");
				});

			basicCircuitBreakerPolicy = Policy
				.HandleResult<RestResponse>(r =>
				!r.IsSuccessStatusCode
				&& r.StatusCode != System.Net.HttpStatusCode.Forbidden
				&& r.StatusCode != System.Net.HttpStatusCode.BadRequest)
				.CircuitBreaker(2, TimeSpan.FromSeconds(30), (iRestResponse, timeSpan) =>
				{
					logger.LogWarning("Circuit Breaker on a break");
				}, () =>
				{
					logger.LogDebug("Circuit Breaker active");
				});
		}

		public async Task<string?> DoRestGet(string suffixUrl, bool useCircuitBreaker = false)
		{
			Policy<RestResponse> policy = retryPolicy;
			if (useCircuitBreaker)
			{
				policy = basicCircuitBreakerPolicy.Wrap(retryPolicy);
			}

			try
			{
				mutex.WaitOne();

				var request = new RestRequest($"{url}/{suffixUrl}")
				{
					RequestFormat = DataFormat.Json
				};

				request.AddHeader("Authorization", $"Bearer {await GetAuthenticationToken()}");
				request.AddHeader("Content-Type", "application/json");

				var stopwatch = new Stopwatch();

				stopwatch.Start();

				var r = policy.Execute(() => restClient.ExecuteGetAsync(request).Result);
				stopwatch.Stop();

				logger.LogTrace($"Url {url}/{suffixUrl} took {stopwatch.ElapsedMilliseconds}ms");

				if (!r.IsSuccessStatusCode)
				{
					if (r.StatusCode == System.Net.HttpStatusCode.Forbidden)
					{
						throw new NotAuthorizedException($"Not authorized executing url [{r.StatusCode}]: {url}/{suffixUrl}");
					}

					throw new NotSupportedException($"Error executing url [{r.StatusCode}]: {url}/{suffixUrl}");
				}

				return r.Content;
			}
			catch (BrokenCircuitException)
			{
				return null;
			}
			finally
			{
				//Call the ReleaseMutex method to unblock so that other threads
				//that are trying to gain ownership of the mutex can enter  
				mutex.ReleaseMutex();
			}
		}

		public async Task<RestResponse> DoRestPost(string suffixUrl, string body)
		{
			try
			{
				mutex.WaitOne();

				var request = new RestRequest($"{url}/{suffixUrl}")
				{
					RequestFormat = DataFormat.Json
				};

				request.AddHeader("Authorization", $"Bearer {await GetAuthenticationToken()}");
				request.AddHeader("Content-Type", "application/json");

				request.AddJsonBody(body);
				var r = retryPolicy.Execute(() => restClient.ExecutePostAsync(request).Result);

				if (!r.IsSuccessStatusCode)
				{
					if (r.StatusCode == System.Net.HttpStatusCode.Forbidden)
					{
						throw new NotAuthorizedException($"Not authorized executing url [{r.StatusCode}]: {url}/{suffixUrl}");
					}

					throw new NotSupportedException($"Error executing url [{r.StatusCode}]: {url}/{suffixUrl}");
				}

				return r;
			}
			finally
			{
				//Call the ReleaseMutex method to unblock so that other threads
				//that are trying to gain ownership of the mutex can enter  
				mutex.ReleaseMutex();
			}
		}

		public async Task<RestResponse> DoRestPut(string suffixUrl, string body)
		{
			try
			{
				mutex.WaitOne();

				var request = new RestRequest($"{url}/{suffixUrl}")
				{
					RequestFormat = DataFormat.Json
				};

				request.AddHeader("Authorization", $"Bearer {await GetAuthenticationToken()}");
				request.AddHeader("Content-Type", "application/json");

				request.AddJsonBody(body);
				var r = retryPolicy.Execute(() => restClient.ExecutePutAsync(request).Result);

				if (!r.IsSuccessStatusCode)
				{
					if (r.StatusCode == System.Net.HttpStatusCode.Forbidden)
					{
						throw new NotAuthorizedException($"Not authorized executing url [{r.StatusCode}]: {url}/{suffixUrl}");
					}

					throw new NotSupportedException($"Error executing url [{r.StatusCode}]: {url}/{suffixUrl}");
				}

				return r;
			}
			finally
			{
				//Call the ReleaseMutex method to unblock so that other threads
				//that are trying to gain ownership of the mutex can enter  
				mutex.ReleaseMutex();
			}
		}

		public async Task<RestResponse> DoRestPatch(string suffixUrl, string body)
		{
			try
			{
				mutex.WaitOne();

				var request = new RestRequest($"{url}/{suffixUrl}")
				{
					RequestFormat = DataFormat.Json
				};

				request.AddHeader("Authorization", $"Bearer {await GetAuthenticationToken()}");
				request.AddHeader("Content-Type", "application/json");

				request.AddJsonBody(body);
				var r = retryPolicy.Execute(() => restClient.PatchAsync(request).Result);

				if (!r.IsSuccessStatusCode)
				{
					if (r.StatusCode == System.Net.HttpStatusCode.Forbidden)
					{
						throw new NotAuthorizedException($"Not authorized executing url [{r.StatusCode}]: {url}/{suffixUrl}");
					}

					throw new NotSupportedException($"Error executing url [{r.StatusCode}]: {url}/{suffixUrl}");
				}

				return r;
			}
			finally
			{
				//Call the ReleaseMutex method to unblock so that other threads
				//that are trying to gain ownership of the mutex can enter  
				mutex.ReleaseMutex();
			}
		}

		public async Task<RestResponse> DoRestDelete(string suffixUrl)
		{
			try
			{
				mutex.WaitOne();

				var request = new RestRequest($"{url}/{suffixUrl}")
				{
					RequestFormat = DataFormat.Json
				};

				request.AddHeader("Authorization", $"Bearer {await GetAuthenticationToken()}");
				request.AddHeader("Content-Type", "application/json");

				var r = retryPolicy.Execute(() => restClient.DeleteAsync(request).Result);

				if (!r.IsSuccessStatusCode)
				{
					throw new NotSupportedException($"Error executing url [{r.StatusCode}]: {url}/{suffixUrl}");
				}

				return r;
			}
			finally
			{
				//Call the ReleaseMutex method to unblock so that other threads
				//that are trying to gain ownership of the mutex can enter  
				mutex.ReleaseMutex();
			}
		}

		private Task<string> GetAuthenticationToken()
		{
			var suffixUrl = "api/v1/auth/anonymous";
			if (memoryCache.TryGetValue<string>(suffixUrl, out var result))
			{
				return Task.FromResult(result!);
			}

			var request = new RestRequest($"{url}/{suffixUrl}")
			{
				RequestFormat = DataFormat.Json
			};

			request.AddHeader("Content-Type", "application/json");

			var body = new JObject
			{
				["accessToken"] = accessToken
			};
			request.AddJsonBody(body.ToString());
			var r = retryPolicy.Execute(() => restClient.ExecutePostAsync(request).Result);

			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Error executing url [{r.StatusCode}]: {url}/{suffixUrl}");
			}

			var content = r.Content;
			if (content == null)
			{
				throw new NotSupportedException($"No token found [{r.StatusCode}]: {url}/{suffixUrl}");
			}

			var token = JsonConvert.DeserializeObject<Token>(content)?.AuthToken ?? throw new NotSupportedException($"No token found [{r.StatusCode}]: {url}/{suffixUrl}");

			memoryCache.Set(suffixUrl, token, CacheDuration.Short());
			return Task.FromResult(token);
		}
	}
}
