using System.Data;
using System.Collections;
using System.Threading;
using System;
using System.IO;
using System.Text;

namespace Agent_ClassLibrary
{
    public partial class CommonQuery_forSMD
    {
        public DB_IO.Connect db_io = new DB_IO.Connect();
        public string Parameter_DEVICE_AREA;
        public string Parameter_DEVICE_ID;
        public string Step;

        /// <summary>
        /// ftp測試連線
        /// </summary>
        public void FTP_TryConnection(ref ftpClient ftpclient)
        {
            #region 取得要連結到的FTP IP
            Step = "取得ftpIP";
            DataTable dt_LCUIPList = GetIPList();
            if (dt_LCUIPList.Rows.Count <= 0)
            {
                throw new Exception("無法取得ftp IP");
            }
            #endregion

            DataRow dr_LCUIPList = dt_LCUIPList.Rows[0];
            string DEVICE_IP = dr_LCUIPList[0].ToString();

            #region 設定ftp連線
            Step = "設定ftp";

            //連結ftp
            ftpclient = new ftpClient(@"ftp://" + DEVICE_IP + @":21/", "anonymous", "anonymous");

            //測試連線是否正常
            ftpclient.directoryListSimple(@"/");

            //建立暫存檔資料夾
            string FileDirectory = System.AppDomain.CurrentDomain.BaseDirectory + Parameter_DEVICE_AREA + @"\" + Parameter_DEVICE_ID;
            Directory.CreateDirectory(FileDirectory);
            #endregion
        }

        /// <summary>
        /// 取得區域內的設備清單
        /// </summary>
        /// <param name="DEVICE_AREA"></param>
        /// <returns></returns>
        public DataTable GetIPList()
        {
            string cmd_Query =
            @"select IP_ADDRESS from SMD_DEVICE with(nolock) where ID=@ID";
            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("@ID", Parameter_DEVICE_AREA);
            DataTable dt = db_io.SqlQuery("SMD", cmd_Query, ht_Query).Tables[0];
            return dt;
        }
    }
}
