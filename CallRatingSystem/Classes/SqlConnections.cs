using HW.CentralConfig.Package.Framework;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using ThreadingTasks = System.Threading.Tasks;
using System.Web.UI.WebControls;


namespace CallRatingSystem.Classes
{
    public class SqlConnections
    {
        private static string _connectionString = ConfigurationManager.ConnectionStrings["MsSqlConnection"].ConnectionString;
        private static string _connectionStringADUsers = ConfigurationManager.ConnectionStrings["MsSqlConnectionADUsers"].ConnectionString;

        private static readonly HttpClient HttpClient = new HttpClient()
        {
            BaseAddress = new Uri( ConfigurationManager.AppSettings["StatsApiBaseUrl"])
        };

        private static async ThreadingTasks.Task SetHttpClientTokenAsync()
        {
            string token = await SecurityTokenProvider.GetTokenAsync()
                .ConfigureAwait(false);

            HttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }


        public static async ThreadingTasks.Task<List<CallInfo>> GetDataCallStatsAsync(string extension, DateTime date)
        {
            await SetHttpClientTokenAsync();
            bool isDebug = ConfigurationManager.AppSettings["Debug"] == "1";

            if (isDebug)
            {
                extension = "330";
                date = new DateTime(2026, 10, 5); 
            }

            var requestBody = new
            {
                extension = extension,
                callType = "Both",
                date = date.Date
            };

            using (var content = new StringContent(
                JsonConvert.SerializeObject(requestBody),
                Encoding.UTF8,
                "application/json"))
            {
                HttpResponseMessage response = await HttpClient
                    .PostAsync("api/3cx/call-logs/by-extension", content);

                response.EnsureSuccessStatusCode();

                string responseJson = await response.Content
                    .ReadAsStringAsync();


                return JsonConvert.DeserializeObject<List<CallInfo>>(responseJson)
                       ?? new List<CallInfo>();
            }
        }

        public static async ThreadingTasks.Task<string> GetCallURLAsync(string filename)
        {
            await SetHttpClientTokenAsync().ConfigureAwait(false);

            string endpoint = "api/3cx/call-logs/call-url/" + Uri.EscapeDataString(filename);

            HttpResponseMessage response = await HttpClient
                .GetAsync(endpoint)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            string json = await response.Content
                .ReadAsStringAsync()
                .ConfigureAwait(false);

            CallUrlResponse result =
                JsonConvert.DeserializeObject<CallUrlResponse>(json);

            return result?.Url;
        }

        public static DataTable GetData(string sql, List<SqlParameter> parms = null, CommandType command = CommandType.Text)
        {
            DataTable dt = new DataTable();

            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                con.Open();

                cmd.CommandType = command;

                if (parms != null)
                    cmd.Parameters.AddRange(parms.ToArray());

                var reader = cmd.ExecuteReader();

                dt.Load(reader);

                con.Close();
            }

            return dt;
        }

        public static DataTable GetDataSQL(string sql)
        {
            DataTable dt = new DataTable();

            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                con.Open();

                cmd.CommandType = CommandType.Text;

                cmd.CommandText = sql;

                var reader = cmd.ExecuteReader();

                dt.Load(reader);

                con.Close();
            }

            return dt;
        }





        public static DataTable GetDataCallStatsSQL(string sql)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["UN_Pabx_Connection"].ConnectionString;
            DataTable dt = new DataTable();

            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                con.Open();

                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(dt);
                }
            }

            return dt;
        }


        public static void ExecuteQuery(string sql, List<SqlParameter> parms = null, CommandType command = CommandType.Text)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                con.Open();

                cmd.CommandType = command;

                if (parms != null)
                    cmd.Parameters.AddRange(parms.ToArray());

                cmd.ExecuteNonQuery();

                con.Close();
            }
        }

        public static long ExecuteQueryAndGetID(string sql, List<SqlParameter> parms = null, CommandType command = CommandType.Text)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                con.Open();

                cmd.CommandType = command;

                if (parms != null)
                    cmd.Parameters.AddRange(parms.ToArray());

                cmd.ExecuteNonQuery();

                cmd.CommandText = "SELECT @@Identity";

                cmd.CommandType = CommandType.Text;

                var id = Convert.ToInt64(cmd.ExecuteScalar());

                con.Close();

                return id;
            }
        }

        public static DataTable GetDataADUsers(string sql, List<SqlParameter> parms = null, CommandType command = CommandType.Text)
        {
            DataTable dt = new DataTable();

            using (SqlConnection con = new SqlConnection(_connectionStringADUsers))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                con.Open();

                cmd.CommandType = command;

                if (parms != null)
                    cmd.Parameters.AddRange(parms.ToArray());

                var reader = cmd.ExecuteReader();

                dt.Load(reader);

                con.Close();
            }

            return dt;
        }

        public static DataTable GetDataADUsersSQL(string sql)
        {
            DataTable dt = new DataTable();

            using (SqlConnection con = new SqlConnection(_connectionStringADUsers))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                con.Open();

                cmd.CommandType = CommandType.Text;


                cmd.CommandText = sql;

                var reader = cmd.ExecuteReader();

                dt.Load(reader);

                con.Close();
            }

            return dt;
        }

        public static void ExecuteQueryADUsers(string sql, List<SqlParameter> parms = null, CommandType command = CommandType.Text)
        {
            using (SqlConnection con = new SqlConnection(_connectionStringADUsers))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                con.Open();

                cmd.CommandType = command;

                if (parms != null)
                    cmd.Parameters.AddRange(parms.ToArray());

                cmd.ExecuteNonQuery();

                con.Close();
            }
        }

        public static long ExecuteQueryAndGetIDADUsers(string sql, List<SqlParameter> parms = null, CommandType command = CommandType.Text)
        {
            using (SqlConnection con = new SqlConnection(_connectionStringADUsers))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                con.Open();

                cmd.CommandType = command;

                if (parms != null)
                    cmd.Parameters.AddRange(parms.ToArray());

                cmd.ExecuteNonQuery();

                cmd.CommandText = "SELECT @@Identity";

                cmd.CommandType = CommandType.Text;

                var id = Convert.ToInt64(cmd.ExecuteScalar());

                con.Close();

                return id;
            }
        }

    }
}