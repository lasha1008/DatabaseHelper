using System.Data;

namespace DatabaseHelper.Common.Extensions
{
	internal static class DbCommandHelper
	{
		public static void AddRange(this IDataParameterCollection dbParametersCollection, params IDataParameter[] parameters)
		{
			foreach (var parameter in parameters)
			{
				dbParametersCollection.Add(parameter);
			}
		}
	}
}
