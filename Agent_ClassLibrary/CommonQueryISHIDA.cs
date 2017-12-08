using System.Data;
using System.Collections;
using System.Threading;
using System;
using System.IO;
using System.Text;

namespace Agent_ClassLibrary
{
    public class CommonQueryISHIDA
    {
        public CommonQueryLCU comQryLCUforISHIDA;

        public DB_IO.Connect db_io = new DB_IO.Connect();
        public string Step;

        #region ISHIDA
        #region ISHIDA:取得TXT
        /// <summary>
        /// 取得TXT
        /// </summary>
        /// <param name="DEVICE_AREA"></param>
        /// <param name="DEVICE_ID"></param>
        /// <returns></returns>
        public DataTable GetTxtFromISHIDA(ref string TXTFileName, ref string BATCHID)
        {
            string cmd_Query = "";
            switch (comQryLCUforISHIDA.Parameter_Version) {
                case 1:
                    cmd_Query= "spUD_LCU_ISHIDA_PCRS_V1_CreateTXT";
                    break;
                case 2:
                    cmd_Query = "spUD_LCU_ISHIDA_PCRS_V2_CreateTXT";
                    break;
            }
            comQryLCUforISHIDA.Agent_WriteLog("GetTxtFromISHIDA " + cmd_Query);

            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("@TXTName", TXTFileName);
            ht_Query.Add("@DEVICE_AREA", comQryLCUforISHIDA.Parameter_DEVICE_AREA);
            ht_Query.Add("@DEVICE_ID", comQryLCUforISHIDA.Parameter_DEVICE_ID);

            Hashtable ht_return = new Hashtable();
            DataTable dt = db_io.SqlSp("DDI_UNDER", cmd_Query, ht_Query, ref ht_return).Tables[0];
            return dt;
        }
        /// <summary>
        /// 取得TXT(含參數)
        /// </summary>
        /// <param name="TXTFileName"></param>
        /// <param name="BATCHID"></param>
        /// <param name="Mode"></param>
        /// <returns></returns>
        public DataTable GetTxtFromISHIDA(ref string TXTFileName, ref string BATCHID, string Mode)
        {
            string cmd_Query = "";
            switch (comQryLCUforISHIDA.Parameter_Version)
            {
                case 1:
                    cmd_Query = "spUD_LCU_ISHIDA_PCRS_V1_CreateTXT";
                    break;
                case 2:
                    cmd_Query = "spUD_LCU_ISHIDA_PCRS_V2_CreateTXT";
                    break;
            }
            comQryLCUforISHIDA.Agent_WriteLog("GetTxtFromISHIDA:" + cmd_Query + " Mode:" + Mode);

            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("@TXTName", TXTFileName);
            ht_Query.Add("@DEVICE_AREA", comQryLCUforISHIDA.Parameter_DEVICE_AREA);
            ht_Query.Add("@DEVICE_ID", comQryLCUforISHIDA.Parameter_DEVICE_ID);
            ht_Query.Add("@Mode", Mode);
            Hashtable ht_return = new Hashtable();
            DataTable dt = db_io.SqlSp("DDI_UNDER", cmd_Query, ht_Query, ref ht_return).Tables[0];
            return dt;
        }
        #endregion

        /// <summary>
        /// 暫存ISHIDA_RFD100.csv(回收專用)
        /// </summary>
        /// <returns></returns>
        public DataTable GetWhiteDT_ISHIDA_RFD100()
        {
            string cmd_Query = "Select * from [ib.DDI_UD_LCU_ISHIDA_PCRS_V1_RFD100] with(nolock) where 1=0";
            Hashtable ht_Query = new Hashtable();
            DataTable dt = db_io.SqlQuery("DDI_UNDER", cmd_Query, ht_Query).Tables[0];
            return dt;
        }

        /// <summary>
        /// TxtTODB 大量塞入資料
        /// </summary>
        /// <param name="RFD100"></param>
        public void TXTTODB_RFD100(DataTable RFD100)
        {
            Hashtable ht1 = new Hashtable();
            ht1.Add("@myTable", RFD100);
            Hashtable ht2 = new Hashtable();
            db_io.SqlSp("DDI_UNDER", "spUD_LCU_ISHIDA_PCRS_V1_BulkInsert", ht1, ref ht2);
        }

        /// <summary>
        /// 上傳ISHIDA_RFD100
        /// </summary>
        /// <param name="DEVICE_AREA"></param>
        /// <param name="DEVICE_ID"></param>
        /// <param name="BATCHID"></param>
        public string UploadISHIDA_RFD100()
        {
            string cmd_Query = "spUD_LCU_ISHIDA_PCRS_V1_GET_RFD100";
            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("@DEVICE_AREA", comQryLCUforISHIDA.Parameter_DEVICE_AREA);
            ht_Query.Add("@DEVICE_ID", comQryLCUforISHIDA.Parameter_DEVICE_ID);
            Hashtable ht_return = new Hashtable();
            ht_return.Add("@I_result", 0);
            ht_return.Add("@S_result", "");
            db_io.SqlSp("DDI_UNDER", cmd_Query, ht_Query, ref ht_return);
            if (ht_return["@I_result"].ToString() == "0")
                return "";
            else
                return ht_return["@S_result"].ToString();
        }
        #endregion
    }
}
