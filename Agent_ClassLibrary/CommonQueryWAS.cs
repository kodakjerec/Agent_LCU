using System.Data;
using System.Collections;
using System.Threading;
using System;
using System.IO;
using System.Text;

namespace Agent_ClassLibrary
{
    public class CommonQueryWAS
    {
        public CommonQueryLCU comQryLCUforISHIDA;

        public DB_IO.Connect db_io = new DB_IO.Connect();
        public string Step;

        #region DDI_Upper轉檔至DDI_Under
        /// <summary>
        /// DDI_Upper轉檔至DDI_Under
        /// </summary>
        public void UpperToUnder()
        {
            string cmd_Query = "";
            switch (comQryLCUforISHIDA.Parameter_Version)
            {
                case 1:
                    cmd_Query = "spUD_WAS_RD2_V1_EXAMINE";
                    break;
                case 2:
                    cmd_Query = "spUD_WAS_RD2_V1_EXAMINE";
                    break;
            }
            comQryLCUforISHIDA.Agent_WriteLog("UpperTOUnder " + cmd_Query);

            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("@DEVICE_AREA", comQryLCUforISHIDA.Parameter_DEVICE_AREA);
            ht_Query.Add("@DEVICE_ID", comQryLCUforISHIDA.Parameter_DEVICE_ID);
            ht_Query.Add("@Status", 0);
            ht_Query.Add("@OrderType", "");
            Hashtable ht_Return = new Hashtable();

            db_io.SqlSp("250_DDI_UNDER", cmd_Query, ht_Query, ref ht_Return);
        }
        #endregion

        #region DDI_Under轉檔至WAS
        /// <summary>
        /// DDI_Upper轉檔至DDI_Under
        /// </summary>
        public void UnderToWAS()
        {
            string cmd_Query = "";
            switch (comQryLCUforISHIDA.Parameter_Version)
            {
                case 1:
                    cmd_Query = "spUD_WAS_RD2_V1_EXAMINE";
                    break;
                case 2:
                    cmd_Query = "spUD_WAS_RD2_V1_EXAMINE";
                    break;
            }
            comQryLCUforISHIDA.Agent_WriteLog("UpperTOUnder " + cmd_Query);

            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("@DEVICE_AREA", comQryLCUforISHIDA.Parameter_DEVICE_AREA);
            ht_Query.Add("@DEVICE_ID", comQryLCUforISHIDA.Parameter_DEVICE_ID);
            ht_Query.Add("@Status", 1);
            ht_Query.Add("@OrderType", "");
            Hashtable ht_Return = new Hashtable();

            db_io.SqlSp("250_DDI_UNDER", cmd_Query, ht_Query, ref ht_Return);
        }
        #endregion
    }
}
