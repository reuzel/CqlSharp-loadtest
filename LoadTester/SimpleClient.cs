using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace LoadTester
{
	public class SimpleClient
	{

		public string BaseUrl { get; set; }

		/// <summary>
		/// API path template
		/// </summary>
		/// <exclude />
		public const string PATH_TEMPLATE = "v2/negr/{0}/{1}";

		/// <summary>
		/// <see cref="System.Net.Http.Formatting.MediaTypeFormatter"/> для упаковки данных запроса к сервису и распаковки ответа
		/// </summary>
		public MediaTypeFormatter Formatter { get; set; }

		/// <inheritdoc cref="Ozon.Api.Client.ApiClientBase" />
		public SimpleClient(string url)
		{
			BaseUrl = url;
			Formatter = new JsonMediaTypeFormatter();
		}


		/// <inheritdoc cref="IRecommendationsService.Get(string,string,string)" />
		public async Task<List<string>> GetAsync(string itemId, string relation)
		{
			return await GetAsync<List<string>>(string.Format(PATH_TEMPLATE, itemId, relation)).ConfigureAwait(false);
		}

		/// <summary>
		/// Прочитать ресурс по адресу и распарсить его ответ в тип <paramref name="T"/>
		/// </summary>
		/// <typeparam name="T">Тип объекта, в который десериализовать ответ сервиса</typeparam>
		/// <param name="url">Адрес ресурса</param>
		/// <returns>Ответ сервиса, десериализованный в тип <paramref name="T"/>. Либо null, если десериализовать не удалось или сервис не вернул ничего.</returns>
		protected async Task<T> GetAsync<T>(string url)
		{
			return await SendAsync<T>(url, HttpMethod.Get, Formatter, null).ConfigureAwait(false);
		}


/// <summary>
		/// Отправить объект по адресу ресурса сервиса
		/// </summary>
		/// <typeparam name="T">Тип DTO-объекта</typeparam>
		/// <param name="url">Адрес ресурса</param>
		/// <param name="method"><see cref="System.Net.Http.HttpMethod"/> запроса</param>
		/// <param name="format"><see cref="System.Net.Http.Formatting.MediaTypeFormatter"/> для упаковки <paramref name="data"/></param>
		/// <param name="data">Объект с данными</param>
		/// <returns>Асинхронный таск</returns>
		protected async Task<T> SendAsync<T>(string url, HttpMethod method, MediaTypeFormatter format, object data)
		{
			using (var client = new HttpClient())
			{
				client.BaseAddress = new Uri(BaseUrl);
				client.DefaultRequestHeaders.Accept.Clear();
				client.Timeout = TimeSpan.FromMilliseconds(3000);

				foreach (var mediaType in format.SupportedMediaTypes)
				{
					client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType.MediaType));
				}

				var request = new HttpRequestMessage(method, url);
				if (method != HttpMethod.Get && data != null)
				{
					request.Content = new ObjectContent(data.GetType(), data, format, format.SupportedMediaTypes.FirstOrDefault());
				}

				HttpResponseMessage response = null;

				response = await client.SendAsync(request).ConfigureAwait(false);

				if (response!= null)
				{
					if (response.IsSuccessStatusCode)
					{
						if (response.StatusCode == System.Net.HttpStatusCode.NoContent || response.StatusCode == System.Net.HttpStatusCode.NotFound)
						{
							return default(T);
						}
						else
						{
							return await response.Content.ReadAsAsync<T>(Enumerable.Repeat(format, 1)).ConfigureAwait(false);
						}
					}
				}
					
				return default(T);
			}
		}

		/// <summary>
		/// Simply dispose it.
		/// </summary>
		public void Dispose()
		{
		}
	}
}
