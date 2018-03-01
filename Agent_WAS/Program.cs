using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using Agent_ClassLibrary;

namespace Agent_WAS
{
    /*
     * 程式只做
     * 1.傳送txt
     * 2.回寫orderList到完成狀態
     */
    public partial class Program
    {
        public static ftpClient ftpclient;
        public static string FileDirectory = ""             //存檔路徑
                            , FileDirectory_SendBackup = ""      //傳送檔案備份
                            , FileDirectory_ReturnBackup = "";   //回收檔案備份
        public static CommonQueryLCU comQryLCU = new CommonQueryLCU();
        public static CommonQueryWAS comQryWAS = new CommonQueryWAS();
        static string appGuidExe = "", appGuid = "";        //每支程式以不同GUID當成Mutex名稱，可避免執行檔同名同姓的風險

        static void Main(string[] args)
        {
            string paras = "";
            string ErrMsg = "";

            #region 錯誤處理
            comQryLCU.Step = "進入程式";
            foreach (string arg in args)
                paras += arg + ",";
            switch (args.Length)
            {
                case 2:
                case 3:
                    break;
                default:
                    Console.WriteLine("用法:WAS_TERAOKA_WAS_1 區域 裝置 指定檔案 \n"
                                     + "範例:WAS_TERAOKA_WAS_1 \"A4WS1\" \"WAS1\" \"HST0030\" ");

                    comQryLCU.Agent_WriteLog("參數數量不對 " + paras);
                    return;
            }

            //如果有錯
            if (ErrMsg != "")
            {
                comQryLCU.Agent_WriteLog(ErrMsg);
                return;
            }
            comQryLCU.Agent_WriteLog("傳入參數：" + paras);
            #endregion

            //如果要做到跨Session唯一，名稱可加入"Global\"前綴字
            //如此即使用多個帳號透過Terminal Service登入系統
            //整台機器也只能執行一份

            //Agent_WAS專屬Mutex
            appGuidExe = Process.GetCurrentProcess().ProcessName + ".exe";
            //同裝置, 不需要重複執行
            appGuid = appGuidExe + "^" + args[0] + "^" + args[1];

            using (Mutex m = new Mutex(false, "Global\\" + appGuid))
            {
                if (!m.WaitOne(0, true))
                {
                    comQryLCU.Agent_WriteLog(appGuid + " 同區域同裝置, 不用重複執行.");
                    return;
                }

                //應用程式例外訊息處理
                AppExceptionHandle();

                comQryLCU.Parameter_DEVICE_AREA = args[0];
                comQryLCU.Parameter_DEVICE_ID = args[1];
                comQryLCU.Parameter_Version = 1;
                comQryWAS.comQryLCUforISHIDA = comQryLCU;

                //建立子資料夾
                FileDirectory = comQryLCU.Parameter_DEVICE_AREA + "\\" + comQryLCU.Parameter_DEVICE_ID;
                FileDirectory_SendBackup = FileDirectory + "\\SendDataBackup";
                FileDirectory_ReturnBackup = FileDirectory + "\\RcvDataBackup";
                Directory.CreateDirectory(FileDirectory);
                Directory.CreateDirectory(FileDirectory_SendBackup);
                Directory.CreateDirectory(FileDirectory_ReturnBackup);

                try
                {
                    //WAS都是由 sql sp負責轉檔
                    //前端只有負責呼叫sp
                    comQryWAS.UpperToUnder();
                    comQryWAS.UnderToWAS();
                    if (args.Length > 2)
                        comQryLCU.Agent_Clean();
                }
                catch (Exception ex)
                {
                    comQryLCU.Agent_WriteLog(ex.ToString());
                }
            }
        }

        #region 根據ErrMsg採取動作
        private static void ErrMsg_WAS_Active(string ORDER_TYPE, string GUID_Msg, string ErrMsg)
        {
            if (ErrMsg != "")
            {
                comQryLCU.Agent_WriteLog(ErrMsg);
                comQryLCU.UpdMiddleList(ORDER_TYPE, GUID_Msg, ErrMsg);
            }
            else
            {
                comQryLCU.UpdMiddleList(ORDER_TYPE, GUID_Msg);
            }
        }
        #endregion

        #region 應用程式例外訊息處理
        public static void AppExceptionHandle()
        {
            //非處理UI執行緒錯誤
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

        }
        //處理UI執行緒錯誤
        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Exception err = e.Exception as Exception;
            comQryLCU.Agent_WriteLog(err.Message);
        }

        //非處理UI執行緒錯誤
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception err = e.ExceptionObject as Exception;
            comQryLCU.Agent_WriteLog(err.Message);
        }
        #endregion
    }
}
