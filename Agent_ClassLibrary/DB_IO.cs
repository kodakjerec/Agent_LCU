using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Configuration;

namespace Agent_ClassLibrary
{
    public class DB_IO
    {
        /// <summary>
        /// 資料庫連結字串
        /// </summary>
        /// <returns></returns>
        public string strCon(string DB)
        {
            string strConn = string.Empty;
            strConn = ConfigurationManager.AppSettings[DB];
            return strConn;
        }

        /// <summary>
        /// 查詢
        /// </summary>
        /// <param name="strDB">資料庫名稱</param>
        /// <param name="sqlcom">SLQ_Commit</param>
        /// <param name="Prm">參數</param>
        /// <returns></returns>
        public DataSet SqlQuery(string strDB, string sqlcom, Hashtable Prm)
        {
            DataSet ds = new DataSet();
            string str_Conn = strCon(strDB);
            SqlConnection Conn = new SqlConnection(str_Conn);
            try
            {
                SqlCommand com = new SqlCommand(sqlcom, Conn);
                foreach (DictionaryEntry entry in Prm)
                {
                    string strKey = entry.Key.ToString();
                    SqlParameter P = new SqlParameter(strKey, SqlDbType.VarChar);
                    P.Value = entry.Value;
                    com.Parameters.Add(P);
                }
                Conn.Open();
                SqlDataAdapter dapter = new SqlDataAdapter(com);
                dapter.Fill(ds);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Conn.Close();
                Conn.Dispose();
            }
            return ds;
        }

        /// <summary>
        /// 更新/刪除
        /// </summary>
        /// <param name="strDB">資料庫名稱</param>
        /// <param name="sqlcom">SLQ_Commit</param>
        /// <param name="Prm">參數</param>
        /// <param name="intItems">異動筆數</param>
        /// <returns></returns>
        public bool SqlUpdate(string strDB, string sqlcom, Hashtable Prm, ref int intItems)
        {
            string str_Conn = strCon(strDB);
            SqlConnection Conn = new SqlConnection(str_Conn);
            bool booUpdate = false;
            intItems = 0;
            try
            {
                SqlCommand com = new SqlCommand(sqlcom, Conn);
                foreach (DictionaryEntry entry in Prm)
                {
                    string strKey = entry.Key.ToString();
                    string strName = entry.Value.GetType().Name.ToUpper();
                    SqlParameter P;
                    switch (strName)
                    {
                        case "BYTE[]":
                            P = new SqlParameter(strKey, SqlDbType.VarBinary); break;
                        case "DATETIME":
                            P = new SqlParameter(strKey, SqlDbType.DateTime); break;
                        default:
                            P = new SqlParameter(strKey, SqlDbType.VarChar); break;
                    }
                    P.Value = entry.Value;
                    com.Parameters.Add(P);
                }
                Conn.Open();
                intItems = com.ExecuteNonQuery();
                booUpdate = true;
            }
            catch (Exception ex)
            {
                booUpdate = false;
                throw ex;
            }
            finally
            {
                Conn.Close();
                Conn.Dispose();
            }
            return booUpdate;
        }

        /// <summary>
        /// Transaction 更新/刪除
        /// </summary>
        /// <param name="strDB"></param>
        /// <param name="sqlcom"></param>
        /// <param name="Prm"></param>
        /// <returns></returns>
        public bool SqlBeginTran(string strDB, string[] sqlcom, Hashtable[] Prm)
        {
            string str_Conn = strCon(strDB);
            bool booBeginTran = false;
            SqlConnection Conn = new SqlConnection(str_Conn);
            Conn.Open();
            SqlTransaction Trans = Conn.BeginTransaction();
            SqlCommand com = new SqlCommand();
            com.Connection = Conn;
            com.Transaction = Trans;

            try
            {
                for (int i = 0; i < sqlcom.Length; i++)
                {
                    com.CommandText = sqlcom[i];
                    foreach (DictionaryEntry entry in Prm[i])
                    {
                        string strKey = entry.Key.ToString();
                        SqlParameter P = new SqlParameter(strKey, SqlDbType.VarChar);
                        P.Value = entry.Value;
                        com.Parameters.Add(P);
                    }
                    com.ExecuteNonQuery();
                    com.Parameters.Clear();
                }
                Trans.Commit();
                booBeginTran = true;
            }
            catch (Exception ex)
            {
                Trans.Rollback();
                booBeginTran = false;
                throw ex;
            }
            finally 
            {
                Conn.Close();
                Conn.Dispose();
            }
            return booBeginTran;
        }

        /// <summary>
        /// 執行預存程序
        /// </summary>
        /// <param name="strDB">資料庫名稱</param>
        /// <param name="SpName">SP名稱</param>
        /// <param name="Prm">參數</param>
        /// <param name="OutPrm">OutPut參數</param>
        /// <returns></returns>
        public DataSet SqlSp(string strDB, string SpName, Hashtable Prm, ref Hashtable OutPrm)
        {
            DataSet Ds = new DataSet();
            string str_Conn = strCon(strDB);
            SqlConnection Conn = new SqlConnection(str_Conn);
            SqlCommand SqlCmd = new SqlCommand(SpName, Conn);
            SqlCmd.CommandTimeout = 0;
            try
            {
                ArrayList arrKey = new ArrayList();
                SqlCmd.CommandType = CommandType.StoredProcedure;
                foreach (DictionaryEntry entry in Prm)
                {
                    string strKey = entry.Key.ToString();
                    SqlCmd.Parameters.Add(strKey, SqlDbType.VarChar);
                    SqlCmd.Parameters[strKey].Value = entry.Value;
                }
                foreach (DictionaryEntry entry in OutPrm)
                {

                    string strKey = entry.Key.ToString();
                    SqlParameter rc = new SqlParameter(strKey, SqlDbType.VarChar, 500);
                    rc.Direction = ParameterDirection.Output;
                    SqlCmd.Parameters.Add(rc);
                    arrKey.Add(strKey);
                }
                SqlCmd.Connection.Open();
                SqlDataAdapter dapter = new SqlDataAdapter(SqlCmd);
                dapter.Fill(Ds);
                if (OutPrm.Count > 0)
                {
                    for (int i = 0; i < arrKey.Count; i++)
                    {
                        int intPrm = Prm.Count + i;
                        string HsKey = arrKey[i] == null ? "" : arrKey[i].ToString();
                        OutPrm[HsKey] = SqlCmd.Parameters[intPrm].Value == null ? "" : SqlCmd.Parameters[intPrm].Value;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Conn.Close();
                Conn.Dispose();
            }
            return Ds;
        }
    }
}
