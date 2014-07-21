using System.Runtime.Serialization;

namespace OwinHost
{
	/// <summary>
	/// Данные по рекомендациям к товару
	/// </summary>
	[DataContract]
	public class RecommendationsDataValue
	{
		/// <summary>
		/// Id товара
		/// </summary>
		[DataMember(IsRequired = true, Order = 1)]
		public string ItemId { get; set; }

		/// <summary>
		/// Вес отношения
		/// </summary>
		[DataMember(IsRequired = true, Order = 2)]
		public decimal Weight { get; set; }
	}
}