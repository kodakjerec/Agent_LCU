using System.Data;
using System.Collections;

namespace Agent_ISHIDA
{
    class CommonQueryISHIDA
    {
        #region 待執行的清單
        /// <summary>
        /// 取得待執行的清單
        /// </summary>
        /// <param name="DEVICE_AREA">區域</param>
        /// <param name="Step">狀態, 0=DB->DB, 1=DB->TXT->DEVICE</param>
        /// <returns></returns>
        public DataTable GetMiddleList(string DEVICE_AREA, int Step)
        {
            string Query =
            @"SELECT 
                GUID
                ,ORDER_TYPE
            FROM 
                [DDI_Middle_Order] 
            WHERE 
                [STATUS]=@STEP
                AND WORKSPACE=@DEVICE_AREA
            GROUP BY
                GUID,
                ORDER_TYPE";
            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("@STEP", Step);
            ht_Query.Add("@DEVICE_AREA", DEVICE_AREA);
            DataTable dt = Program.ioDB.SqlQuery("DDI", Query, ht_Query).Tables[0];
            return dt;
        }

        /// <summary>
        /// 更改待執行清單
        /// </summary>
        /// <param name="DEVICE_AREA"></param>
        /// <param name="DEVICE_ID"></param>
        /// <param name="GUID"></param>
        /// <param name="SpName"></param>
        /// <param name="Step"></param>
        /// <returns></returns>
        public void UpdMiddleList(string DEVICE_AREA, string DEVICE_ID, string ORDER_TYPE, string GUID)
        {
            int SuccessCount = 0;

            Hashtable ht_Query = new Hashtable();
            string Query =
            @"UPDATE 
				    [DDI_Middle_Order] 
			    SET 
				    [Status]=2 
			    Where 
				    [Status]=1 
				    and GUID=@GUID
				    and WORKSPACE=@DEVICE_AREA
                    and DEVICE_ID=@DEVICE_ID
				    and ORDER_TYPE=@ORDER_TYPE";
            ht_Query.Add("@GUID", GUID);
            ht_Query.Add("@DEVICE_AREA", DEVICE_AREA);
            ht_Query.Add("@DEVICE_ID", DEVICE_ID);
            ht_Query.Add("@ORDER_TYPE", ORDER_TYPE);
            Program.ioDB.SqlUpdate("DDI", Query, ht_Query, ref SuccessCount);
            Program.Agent_WriteLog(ORDER_TYPE + "," + GUID + ",執行完畢");
        }
        #endregion

        #region 設備清單
        /// <summary>
        /// 取得區域內的設備清單
        /// </summary>
        /// <param name="DEVICE_AREA"></param>
        /// <returns></returns>
        public DataTable GetIPList(string DEVICE_AREA, string DEVICE_ID)
        {
            string cmd_Query =
            @"Select 
                DEVICE_IP 
            From 
                [vDDI_DEVICE] 
            WHERE 
                WORKSPACE=@DEVICE_AREA 
                AND DEVICE_ID=@DEVICE_ID";
            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("@DEVICE_AREA", DEVICE_AREA);
            ht_Query.Add("@DEVICE_ID", DEVICE_ID);
            DataTable dt = Program.ioDB.SqlQuery("DDI", cmd_Query, ht_Query).Tables[0];
            return dt;
        }
        #endregion

        #region 批次管理
        /// <summary>
        /// 結束批次
        /// </summary>
        /// <param name="DEVICE_AREA"></param>
        /// <param name="DEVICE_ID"></param>
        /// <param name="BATCHID"></param>
        public string CloseBatch(string DEVICE_AREA, string DEVICE_ID, string BATCHID)
        {
            Hashtable ht_Query = new Hashtable();
            string Query =
            @"spUD_LCU_TERAOKA_LCU700_V1_BatchList";
            ht_Query.Add("@DEVICE_AREA", DEVICE_AREA);
            ht_Query.Add("@DEVICE_ID", DEVICE_ID);
            ht_Query.Add("@BatchID", BATCHID);
            ht_Query.Add("@Status", 0);
            Hashtable ht_return = new Hashtable();
            ht_return.Add("@I_result", 0);
            ht_return.Add("@S_result", "");
            Program.ioDB.SqlSp("DDI", Query, ht_Query, ref ht_return);
            if (ht_return["@I_result"].ToString() == "0")
                return "";
            else
                return ht_return["@S_result"].ToString();
        }

        /// <summary>
        /// 開始批次
        /// </summary>
        /// <param name="DEVICE_AREA"></param>
        /// <param name="DEVICE_ID"></param>
        /// <param name="BATCHID"></param>
        public string OpenBatch(string DEVICE_AREA, string DEVICE_ID, string BATCHID)
        {
            Hashtable ht_Query = new Hashtable();
            string Query =
            @"spUD_LCU_TERAOKA_LCU700_V1_BatchList";
            ht_Query.Add("@DEVICE_AREA", DEVICE_AREA);
            ht_Query.Add("@DEVICE_ID", DEVICE_ID);
            ht_Query.Add("@BatchID", BATCHID);
            ht_Query.Add("@Status", 1);
            Hashtable ht_return = new Hashtable();
            ht_return.Add("@I_result", 0);
            ht_return.Add("@S_result", "");
            Program.ioDB.SqlSp("DDI", Query, ht_Query, ref ht_return);
            if (ht_return["@I_result"].ToString() == "0")
                return "";
            else
                return ht_return["@S_result"].ToString();
        }

        /// <summary>
        /// 取得批次(回收專用)
        /// </summary>
        /// <param name="DEVICE_AREA"></param>
        /// <param name="DEVICE_ID"></param>
        public DataTable GetBatch(string DEVICE_AREA, string DEVICE_ID)
        {
            Hashtable ht_Query = new Hashtable();
            string Query =
            @"spUD_LCU_TERAOKA_LCU700_V1_BatchList";
            ht_Query.Add("@DEVICE_AREA", DEVICE_AREA);
            ht_Query.Add("@DEVICE_ID", DEVICE_ID);
            ht_Query.Add("@BatchID", "");
            ht_Query.Add("@Status", 2);
            Hashtable ht_return = new Hashtable();
            ht_return.Add("@I_result", 0);
            ht_return.Add("@S_result", "");
            DataTable dt = Program.ioDB.SqlSp("DDI", Query, ht_Query, ref ht_return).Tables[0];

            return dt;
        }
        #endregion

        #region 取得TXT
        /// <summary>
        /// 取得TXT
        /// </summary>
        /// <param name="DEVICE_AREA"></param>
        /// <param name="DEVICE_ID"></param>
        /// <returns></returns>
        public DataTable GetTxt(string DEVICE_AREA, string DEVICE_ID, ref string TXTFileName)
        {
            string cmd_Query = "spUD_LCU_TERAOKA_LCU700_V1_CreateTXT";
            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("@TXTName", TXTFileName);
            ht_Query.Add("@DEVICE_AREA", DEVICE_AREA);
            ht_Query.Add("@DEVICE_ID", DEVICE_ID);
            Hashtable ht_return = new Hashtable();
            ht_return.Add("@S_result", "");
            DataTable dt = Program.ioDB.SqlSp("DDI", cmd_Query, ht_Query, ref ht_return).Tables[0];
            TXTFileName = ht_return["@S_result"].ToString();
            return dt;
        }
        #endregion
    }
}
