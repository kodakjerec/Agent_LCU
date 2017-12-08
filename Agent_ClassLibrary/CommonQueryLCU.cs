using System.Data;
using System.Collections;
using System.Threading;
using System;
using System.IO;
using System.Text;

namespace Agent_ClassLibrary
{
    public class CommonQueryLCU
    {
        public DB_IO.Connect db_io = new DB_IO.Connect();
        public string Parameter_DEVICE_AREA;//區域
        public string Parameter_DEVICE_ID;  //裝置
        public string Parameter_DEVICE_IP = "";       //裝置IP
        public int Parameter_Version;   //版本
        public string Step;

        #region DDI_Upper轉檔至DDI_Under
        /// <summary>
        /// DDI_Upper轉檔至DDI_Under
        /// </summary>
        public void UpperToUnder()
        {
            string cmd_Query = "";
            switch (Parameter_Version)
            {
                case 1:
                    cmd_Query = "spUD_LCU_TERAOKA_V1_EXAMINE";
                    break;
                case 2:
                    cmd_Query = "spUD_LCU_TERAOKA_V2_EXAMINE";
                    break;
            }
            Agent_WriteLog("UpperTOUnder " + cmd_Query);

            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("@DEVICE_AREA", Parameter_DEVICE_AREA);
            ht_Query.Add("@DEVICE_ID", Parameter_DEVICE_ID);
            ht_Query.Add("@Status", 0);
            ht_Query.Add("@OrderType", "");
            Hashtable ht_Return = new Hashtable();

            db_io.SqlSp("DDI_UNDER", cmd_Query, ht_Query, ref ht_Return);
        }
        #endregion

        #region 待執行的清單
        /// <summary>
        /// 取得待執行的清單
        /// </summary>
        /// <param name="DEVICE_AREA">區域</param>
        /// <param name="Step">狀態, 0=DB->DB, 1=DB->TXT->DEVICE</param>
        /// <returns></returns>
        public DataTable GetMiddleList()
        {
            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("@DEVICE_AREA", Parameter_DEVICE_AREA);
            ht_Query.Add("@DEVICE_ID", Parameter_DEVICE_ID);
            ht_Query.Add("@Status", 1);
            ht_Query.Add("@OrderType", "");
            Hashtable ht_Return = new Hashtable();

            DataTable dt = db_io.SqlSp("DDI_UNDER", "spUD_LCU_TERAOKA_V1_EXAMINE", ht_Query, ref ht_Return).Tables[0];
            return dt;
        }

        /// <summary>
        /// 更改待執行清單(執行結果正確)
        /// </summary>
        /// <param name="ORDER_TYPE"></param>
        /// <param name="GUID_Msg"></param>
        /// <returns></returns>
        public void UpdMiddleList(string ORDER_TYPE, string GUID_Msg)
        {
            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("@DEVICE_AREA", Parameter_DEVICE_AREA);
            ht_Query.Add("@DEVICE_ID", Parameter_DEVICE_ID);
            ht_Query.Add("@Status", 2);
            ht_Query.Add("@OrderType", ORDER_TYPE);
            ht_Query.Add("@Memo", "");
            ht_Query.Add("@GUIDMessage", GUID_Msg);
            Hashtable ht_Return = new Hashtable();

            db_io.SqlSp("DDI_UNDER", "spUD_LCU_TERAOKA_V1_EXAMINE", ht_Query, ref ht_Return);
        }
        /// <summary>
        /// 執行結果錯誤
        /// </summary>
        /// <param name="ORDER_TYPE"></param>
        /// <param name="MEMO"></param>
        /// <param name="GUID_Msg"></param>
        public void UpdMiddleList(string ORDER_TYPE, string GUID_Msg, string MEMO)
        {
            if (MEMO.Length > 200)
                MEMO = MEMO.Substring(0, 200);
            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("@DEVICE_AREA", Parameter_DEVICE_AREA);
            ht_Query.Add("@DEVICE_ID", Parameter_DEVICE_ID);
            ht_Query.Add("@Status", 3);
            ht_Query.Add("@OrderType", ORDER_TYPE);
            ht_Query.Add("@Memo", MEMO);
            ht_Query.Add("@GUIDMessage", GUID_Msg);
            Hashtable ht_Return = new Hashtable();

            db_io.SqlSp("DDI_UNDER", "spUD_LCU_TERAOKA_V1_EXAMINE", ht_Query, ref ht_Return);
        }
        #endregion

        #region 設備清單
        /// <summary>
        /// 取得區域內的設備清單
        /// </summary>
        /// <param name="DEVICE_AREA"></param>
        /// <returns></returns>
        public DataTable GetIPList()
        {
            string cmd_Query =
            @"Select 
                DEVICE_IP, DEVICE_VERSION
            From 
                [vDDI_DEVICE] with(nolock) 
            WHERE 
                WORKSPACE=@DEVICE_AREA 
                AND DEVICE_ID=@DEVICE_ID";
            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("@DEVICE_AREA", Parameter_DEVICE_AREA);
            ht_Query.Add("@DEVICE_ID", Parameter_DEVICE_ID);
            DataTable dt = db_io.SqlQuery("DDI", cmd_Query, ht_Query).Tables[0];

            //取得要連結到的FTP IP
            if (dt.Rows.Count <= 0)
            {
                throw new Exception("無法取得ftp IP");
            }
            else
            {
                Parameter_DEVICE_IP = dt.Rows[0][0].ToString();
                Parameter_Version = Convert.ToInt32(dt.Rows[0][1]);
            }
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
        public string GetOrderNo()
        {
            Hashtable ht_Query = new Hashtable();
            string cmd_Query =
            @"SELECT OrderNo FROM DDI.dbo.DDI_WORKSPACE_STATUS with(nolock) where WORKSPACE=@DEVICE_AREA and DEVICE_ID=@DEVICE_ID";
            ht_Query.Add("@DEVICE_AREA", Parameter_DEVICE_AREA);
            ht_Query.Add("@DEVICE_ID", Parameter_DEVICE_ID);
            ht_Query.Add("@Status", 0);
            DataTable dt = db_io.SqlQuery("DDI", cmd_Query, ht_Query).Tables[0];
            if (dt.Rows.Count > 0)
                return dt.Rows[0][0].ToString();
            else
                return "";
        }
        #endregion

        #region FTP 延伸功能
        /// <summary>
        /// 檢查檔案上傳是否完成
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="ftp"></param>
        /// <param name="ExistType">0:檔案要存在 1:檔案要不存在</param>
        /// <returns></returns>
        public string FTPCheckFileUploadOK(string FileName, ref ftpClient ftp, int ExistType)
        {
            bool IsOK = false;
            int CheckSecond = 0;
            int MaxSeconds = 30;
            string[] files;

            //第一關：檢查檔案存在
            //如果是要檢查檔案消失的設定, 有可能檔案下傳後馬上消失, 來不及先進入第一關的確認檔案存在
            try
            {
                IsOK = false;

                while (true)
                {
                    //CheckFileSize = ftp.getFileSize(FileName);
                    //CheckSecond = MaxWaitSecond + 1;
                    //Steps = 1;

                    files = ftp.directoryListSimple("");
                    foreach (string file in files)
                    {
                        if (file == FileName)
                        {
                            IsOK = true;
                            break;
                        }
                    }

                    if (IsOK)
                        break;

                    if (ExistType == 1)
                    {
                        if (CheckSecond >= MaxSeconds)
                            break;
                    }

                    Thread.Sleep(1000);
                    CheckSecond += 1;
                    Console.CursorLeft = 0;
                    Console.Write(CheckSecond.ToString() + "...");
                }
                Console.WriteLine("");

                //第二關：檔案要消失, 才能離開
                if (ExistType == 1)
                {
                    while (true)
                    {
                        IsOK = true;
                        files = ftp.directoryListSimple("");

                        foreach (string file in files)
                        {
                            if (file == FileName)
                                IsOK = false;
                        }

                        if (IsOK)
                            break;

                        Thread.Sleep(1000);
                        CheckSecond += 1;
                        Console.CursorLeft = 0;
                        Console.Write(CheckSecond.ToString() + "...");
                    }
                    Console.WriteLine("");
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            #region 檔案下傳時間寫入log
            string strlog = " 下傳 " + FileName;
            if (ExistType == 1)
                strlog += " 並等待消失.";
            strlog += " 共花費:" + CheckSecond.ToString() + " 秒";
            Agent_WriteLog(strlog);
            #endregion

            Console.WriteLine("");
            return "";
        }

        /// <summary>
        /// ftp測試連線
        /// </summary>
        public void FTP_TryConnection(ref ftpClient ftpclient)
        {
            #region 設定ftp連線
            Step = "設定ftp";

            //連結ftp
            ftpclient = new ftpClient(@"ftp://" + Parameter_DEVICE_IP + @":21/", "anonymous", "anonymous");

            //測試連線是否正常
            ftpclient.directoryListSimple(@"/");
            Agent_WriteLog("PC<->" + Parameter_DEVICE_ID + " " + Parameter_DEVICE_IP);

            //建立暫存檔資料夾
            string FileDirectory = System.AppDomain.CurrentDomain.BaseDirectory + Parameter_DEVICE_AREA + @"\" + Parameter_DEVICE_ID;
            Directory.CreateDirectory(FileDirectory);
            #endregion
        }
        #endregion

        #region Log, 清潔功能
        private ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        /// <summary>
        /// 寫入Log
        /// </summary>
        /// <param name="ErrMsg">錯誤訊息</param>
        public void Agent_WriteLog(string ErrMsg)
        {
            ErrMsg = DateTime.Now.ToString("HH:mm:ss")
                                + "^" + Parameter_DEVICE_AREA
                                + "^" + Parameter_DEVICE_ID
                                + "^" + Step
                                + "^" + ErrMsg;

            //PrintLog
            Console.WriteLine(ErrMsg);

            //WriteLog
            string FilePath = System.AppDomain.CurrentDomain.BaseDirectory + @"log\" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
            Directory.CreateDirectory(System.AppDomain.CurrentDomain.BaseDirectory + @"log");

            _readWriteLock.EnterWriteLock();
            using (FileStream fs = new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.Default, 4096))
                {
                    sw.WriteLine(ErrMsg);
                    sw.Close();
                }
            }
            _readWriteLock.ExitWriteLock();

        }

        /// <summary>
        /// 清除系統資料
        /// </summary>
        public void Agent_Clean()
        {
            string FilePathLog = System.AppDomain.CurrentDomain.BaseDirectory + "\\log";
            string FilePath = System.AppDomain.CurrentDomain.BaseDirectory + "\\" + Parameter_DEVICE_AREA + "\\" + Parameter_DEVICE_ID;
            string FileDirectory_SendBackup = FilePath + "\\SendDataBackup";
            string FileDirectory_ReturnBackup = FilePath + "\\RcvDataBackup";

            //清除 agent本身的log
            string[] files = Directory.GetFiles(FilePathLog);
            foreach (string file in files)
            {
                if (File.GetLastWriteTime(file) <= DateTime.Now.AddDays(-7))
                    File.Delete(file);
            }

            //清除 上傳資料
            files = Directory.GetFiles(FileDirectory_SendBackup);
            foreach (string file in files)
            {
                if (File.GetLastWriteTime(file) <= DateTime.Now.AddDays(-7))
                    File.Delete(file);
            }

            //清除 回收資料
            files = Directory.GetFiles(FileDirectory_ReturnBackup);
            foreach (string file in files)
            {
                if (File.GetLastWriteTime(file) <= DateTime.Now.AddDays(-7))
                    File.Delete(file);
            }

        }
        #endregion

        #region LCU
        #region LCU:取得TXT
        /// <summary>
        /// 取得TXT
        /// </summary>
        /// <param name="DEVICE_AREA"></param>
        /// <param name="DEVICE_ID"></param>
        /// <returns></returns>
        public DataTable GetTxtFromLCU(ref string TXTFileName, ref string BATCHID)
        {
            string cmd_Query = "";
            switch (Parameter_Version)
            {
                case 1:
                    cmd_Query = "spUD_LCU_TERAOKA_LCU700_V1_CreateTXT";
                    break;
                case 2:
                    cmd_Query = "spUD_LCU_TERAOKA_LCU700_V2_CreateTXT";
                    break;
            }
            Agent_WriteLog("GetTxtFromLCU " + cmd_Query);

            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("@TXTName", TXTFileName);
            ht_Query.Add("@DEVICE_AREA", Parameter_DEVICE_AREA);
            ht_Query.Add("@DEVICE_ID", Parameter_DEVICE_ID);

            Hashtable ht_return = new Hashtable();
            ht_return.Add("@S_result", "");
            DataTable dt = db_io.SqlSp("DDI_UNDER", cmd_Query, ht_Query, ref ht_return).Tables[0];
            TXTFileName = ht_return["@S_result"].ToString();
            return dt;
        }
        public DataTable GetTxtFromLCU(ref string TXTFileName, ref string BATCHID, string Mode)
        {
            string cmd_Query = "";
            switch (Parameter_Version)
            {
                case 1:
                    cmd_Query = "spUD_LCU_TERAOKA_LCU700_V1_CreateTXT";
                    break;
                case 2:
                    cmd_Query = "spUD_LCU_TERAOKA_LCU700_V2_CreateTXT";
                    break;
            }
            Agent_WriteLog("GetTxtFromLCU:" + cmd_Query + " Mode:" + Mode);

            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("@TXTName", TXTFileName);
            ht_Query.Add("@DEVICE_AREA", Parameter_DEVICE_AREA);
            ht_Query.Add("@DEVICE_ID", Parameter_DEVICE_ID);
            ht_Query.Add("@Mode", Mode);
            Hashtable ht_return = new Hashtable();
            ht_return.Add("@S_result", "");
            DataTable dt = db_io.SqlSp("DDI_UNDER", cmd_Query, ht_Query, ref ht_return).Tables[0];
            TXTFileName = ht_return["@S_result"].ToString();
            return dt;
        }
        #endregion

        #region LCU:回收專用
        /// <summary>
        /// 暫存LCU0301(回收專用)
        /// </summary>
        /// <returns></returns>
        public DataTable GetWhiteDT_LCU0301()
        {
            string cmd_Query = "Select * from [ib.DDI_UD_LCU_TERAOKA_LCU700_V1_LCU0301] with(nolock) where 1=0";
            Hashtable ht_Query = new Hashtable();
            DataTable dt = db_io.SqlQuery("DDI_UNDER", cmd_Query, ht_Query).Tables[0];
            return dt;
        }

        /// <summary>
        /// TxtTODB 大量塞入資料
        /// </summary>
        /// <param name="dt_LCU0301"></param>
        public void TXTTODB_LCU0301(DataTable dt_LCU0301)
        {
            Hashtable ht1 = new Hashtable();
            ht1.Add("@myTable", dt_LCU0301);
            Hashtable ht2 = new Hashtable();
            db_io.SqlSp("DDI_UNDER", "spUD_LCU_TERAOKA_LCU700_V1_BulkInsert", ht1, ref ht2);
        }

        /// <summary>
        /// 上傳LCU0301
        /// </summary>
        /// <param name="DEVICE_AREA"></param>
        /// <param name="DEVICE_ID"></param>
        /// <param name="BATCHID"></param>
        public string Upload_LCU0301()
        {
            string cmd_Query = "spUD_LCU_TERAOKA_LCU700_V1_GET_LCU0301";
            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("@DEVICE_AREA", Parameter_DEVICE_AREA);
            ht_Query.Add("@DEVICE_ID", Parameter_DEVICE_ID);
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

        #endregion
    }
}
