using System.Data;
using System.Data.SqlClient;

namespace BotFair.Tools
{
    internal class DbAccess
    {
        private string connectionString;

        public DbAccess(string connnectionString)
        {
            this.connectionString = connnectionString;
        }

        public DataRowCollection DoQuery(string query)
        {
            SqlConnection connection = new SqlConnection(connectionString);
            SqlCommand command = new SqlCommand(query, connection);

            SqlDataAdapter adapter = new SqlDataAdapter(command);

            DataSet ds = new DataSet();
            adapter.Fill(ds);

            if (ds.Tables.Count > 0) return ds.Tables[0].Rows;

            return null;
        }

        public int DoUpdate(string update)
        {
            int rows = 0;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand(update, connection);
                rows = command.ExecuteNonQuery();
            }

            return rows;
        }
    }
}