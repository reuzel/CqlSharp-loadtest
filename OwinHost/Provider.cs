using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CqlSharp;
using Newtonsoft.Json;

namespace OwinHost
{
	public class Provider
	{
		private string _connString;

		/// <summary>
		/// Ctor, (KO)
		/// </summary>
		public Provider()
		{
			var cString = System.Configuration.ConfigurationManager.ConnectionStrings["api:provider"];
			if (cString != null)
			{
				_connString = cString.ConnectionString;
			}
		}

		public async Task<List<string>> Get(string appId, string itemId, string relation)
		{
			return await Get(appId, itemId, relation, null).ConfigureAwait(false);
		}

		public async Task<List<string>> Get(string appId, string itemId, string relation, string userId)
		{
			using (var conn = new CqlConnection(_connString))
			{
				await conn.OpenAsync().ConfigureAwait(false);
				var query = string.Format(
					System.Globalization.CultureInfo.InvariantCulture,
					"select values from relations where app_id = '{0}' and item_id='{1}' and relation='{2}';",
					appId,
					GetKey(itemId, userId),
					relation);

				var cmd = new CqlCommand(
					conn,
					query,
					CqlConsistency.One);

				var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false) as string;

				if (string.IsNullOrEmpty(result))
				{
					return null;
				}

				var list = JsonConvert.DeserializeObject<List<RecommendationsDataValue>>(result);
				return list
					.Select(i => i.ItemId)
					.ToList();
			}
		}

		private string GetKey(string baseKey, string optionalKey)
		{
			return string.IsNullOrEmpty(optionalKey) ? baseKey : optionalKey + "||" + baseKey;
		}
	}
}