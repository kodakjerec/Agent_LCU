using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Data;
using Agent_ClassLibrary;

namespace Agent_LCU
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
                    Console.WriteLine("用法:LCU_TERAOKA_LCU_1 區域 裝置 指定檔案 \n"
                                     + "範例:LCU_TERAOKA_LCU_1 \"A4WS1\" \"LCU1\" \"HST0030\" ");

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

            //Agent_LCU專屬Mutex
            appGuidExe = Process.GetCurrentProcess().ProcessName + ".exe";
            //同裝置, 不需要重複執行
            appGuid = appGuidExe + "^" + args[1];

            using (Mutex m = new Mutex(false, "Global\\" + appGuid))
            {
                if (!m.WaitOne(0, true))
                {
                    comQryLCU.Agent_WriteLog(appGuid + " 同裝置, 不用重複執行.");
                    return;
                }

                //應用程式例外訊息處理
                AppExceptionHandle();

                comQryLCU.Parameter_DEVICE_AREA = args[0];
                comQryLCU.Parameter_DEVICE_ID = args[1];

                string OrderNo = "";    //批次
                string ORDER_TYPE = ""; //工作類型
                bool IsAssignFile = false;   //1-有指定特定檔案

                if (args.Length == 3)
                {
                    IsAssignFile = true;
                }

                try
                {
                    //建立子資料夾
                    FileDirectory = comQryLCU.Parameter_DEVICE_AREA + "\\" + comQryLCU.Parameter_DEVICE_ID;
                    FileDirectory_SendBackup = FileDirectory + "\\SendDataBackup";
                    FileDirectory_ReturnBackup = FileDirectory + "\\RcvDataBackup";
                    Directory.CreateDirectory(FileDirectory);
                    Directory.CreateDirectory(FileDirectory_SendBackup);
                    Directory.CreateDirectory(FileDirectory_ReturnBackup);

                    //FTP測試連線
                    comQryLCU.FTP_TryConnection(ref ftpclient);

                    ORDER_Start:

                    #region 先執行sp轉檔
                    comQryLCU.Step = "UPPER->UNDER";
                    comQryLCU.UpperToUnder();
                    comQryLCU.Agent_WriteLog("轉檔結束");
                    #endregion

                    #region 取得待執行的sp清單 From OrderList
                    comQryLCU.Step = "取得Orders";
                    DataTable dt_WaitingForExe = comQryLCU.GetMiddleList();
                    comQryLCU.Agent_WriteLog("待執行Orders: " + dt_WaitingForExe.Rows.Count.ToString());
                    #endregion

                    #region **Special** 如果有指定param3, 直接輸出檔案
                    /*20170116 如果有指定param3, 直接輸出檔案*/
                    if (IsAssignFile)
                    {
                        DataRow dr = dt_WaitingForExe.NewRow();
                        dr[0] = comQryLCU.GetOrderNo();
                        dr[1] = args[2];
                        dr[2] = 1;
                        dr[3] = DateTime.Now;
                        dt_WaitingForExe.Rows.Add(dr);
                        IsAssignFile = false;
                    }
                    #endregion

                    if (dt_WaitingForExe.Rows.Count > 0)
                    {
                        DataRow[] drs_ORDER_TYPE;

                        //以下ORDER_TYPE依照優先順序排序

                        #region ORDER_TYPE=20 回收實績
                        drs_ORDER_TYPE = dt_WaitingForExe.Select("ORDER_TYPE=20");

                        if (drs_ORDER_TYPE.Length > 0)
                        {
                            RcvFromLCU program1 = new RcvFromLCU();

                            foreach (DataRow dr_ORDER_TYPE in drs_ORDER_TYPE)
                            {
                                ORDER_TYPE = dr_ORDER_TYPE["ORDER_TYPE"].ToString();
                                OrderNo = dr_ORDER_TYPE["OrderNo"].ToString();

                                ErrMsg = program1.MainFunction(ORDER_TYPE, OrderNo
                                    );
                                if (ErrMsg != "")
                                {
                                    comQryLCU.Agent_WriteLog(ErrMsg);
                                    comQryLCU.UpdMiddleList(ORDER_TYPE, ErrMsg);
                                    continue;
                                }
                                comQryLCU.UpdMiddleList(ORDER_TYPE);
                            }
                        }
                        #endregion

                        #region ORDER_TYPE in (120) 先清除實績 PCtoLCU

                        drs_ORDER_TYPE = dt_WaitingForExe.Select("ORDER_TYPE in (120)");

                        if (drs_ORDER_TYPE.Length > 0)
                        {
                            DeleToLCU program1 = new DeleToLCU();

                            ORDER_TYPE = drs_ORDER_TYPE[0]["ORDER_TYPE"].ToString();
                            OrderNo = drs_ORDER_TYPE[0]["OrderNo"].ToString();

                            ErrMsg = program1.MainFunction(ORDER_TYPE);
                            if (ErrMsg != "")
                            {
                                comQryLCU.Agent_WriteLog(ErrMsg);
                                comQryLCU.UpdMiddleList(ORDER_TYPE, ErrMsg);
                            }
                            else
                            {
                                comQryLCU.UpdMiddleList(ORDER_TYPE);
                            }

                        }
                        #endregion

                        #region ORDER_TYPE in (111,112,113,114,115) 清除主檔 PCtoLCU

                        drs_ORDER_TYPE = dt_WaitingForExe.Select("ORDER_TYPE in (111,112,113,114,115)");

                        if (drs_ORDER_TYPE.Length > 0)
                        {
                            DeleToLCU program1 = new DeleToLCU();

                            ORDER_TYPE = drs_ORDER_TYPE[0]["ORDER_TYPE"].ToString();
                            OrderNo = drs_ORDER_TYPE[0]["OrderNo"].ToString();

                            ErrMsg = program1.MainFunction(ORDER_TYPE);
                            if (ErrMsg != "")
                            {
                                comQryLCU.Agent_WriteLog(ErrMsg);
                                comQryLCU.UpdMiddleList(ORDER_TYPE, ErrMsg);
                            }
                            else
                            {
                                comQryLCU.UpdMiddleList(ORDER_TYPE);
                            }
                        }
                        #endregion

                        #region ORDER_TYPE=10 開始
                        drs_ORDER_TYPE = dt_WaitingForExe.Select("ORDER_TYPE=10");

                        if (drs_ORDER_TYPE.Length > 0)
                        {
                            ORDER_TYPE = drs_ORDER_TYPE[0]["ORDER_TYPE"].ToString();
                            OrderNo = drs_ORDER_TYPE[0]["OrderNo"].ToString();

                            comQryLCU.UpdMiddleList(ORDER_TYPE);
                        }
                        #endregion

                        #region ORDER_TYPE in (11,12,13,14) 同步主檔 PCtoLCU

                        drs_ORDER_TYPE = dt_WaitingForExe.Select("ORDER_TYPE in (11,12,13,14)");

                        if (drs_ORDER_TYPE.Length > 0)
                        {
                            SendToLCU program1 = new SendToLCU();

                            ORDER_TYPE = drs_ORDER_TYPE[0]["ORDER_TYPE"].ToString();
                            OrderNo = drs_ORDER_TYPE[0]["OrderNo"].ToString();

                            ErrMsg = program1.MainFunction(ORDER_TYPE, OrderNo);
                            if (ErrMsg != "")
                            {
                                comQryLCU.Agent_WriteLog(ErrMsg);
                                comQryLCU.UpdMiddleList(ORDER_TYPE, ErrMsg);
                            }
                            else
                            {
                                comQryLCU.UpdMiddleList(ORDER_TYPE);
                            }
                        }
                        #endregion

                        #region ORDER_TYPE=15 下分揀

                        drs_ORDER_TYPE = dt_WaitingForExe.Select("ORDER_TYPE=15");

                        if (drs_ORDER_TYPE.Length > 0)
                        {
                            SendToLCU program1 = new SendToLCU();

                            ORDER_TYPE = drs_ORDER_TYPE[0]["ORDER_TYPE"].ToString();
                            OrderNo = drs_ORDER_TYPE[0]["OrderNo"].ToString();

                            ErrMsg = program1.MainFunction(ORDER_TYPE, OrderNo);
                            if (ErrMsg != "")
                            {
                                comQryLCU.Agent_WriteLog(ErrMsg);
                                comQryLCU.UpdMiddleList(ORDER_TYPE, ErrMsg);
                            }
                            else
                            {
                                comQryLCU.UpdMiddleList(ORDER_TYPE);
                            }
                        }
                        #endregion

                        #region ORDER_TYPE=21 結束
                        drs_ORDER_TYPE = dt_WaitingForExe.Select("ORDER_TYPE=21");

                        if (drs_ORDER_TYPE.Length > 0)
                        {
                            ORDER_TYPE = drs_ORDER_TYPE[0]["ORDER_TYPE"].ToString();

                            comQryLCU.Agent_Clean();

                            comQryLCU.UpdMiddleList(ORDER_TYPE);
                        }
                        #endregion

                        goto ORDER_Start;
                    }
                }
                catch (Exception e)
                {
                    comQryLCU.UpdMiddleList(ORDER_TYPE, e.Message);
                    comQryLCU.Agent_WriteLog(e.Message);
                }
                finally
                {
                    m.ReleaseMutex();
                }
                //Console.ReadLine();
                //如果是Windows Form，Application.Run()要包在using Mutex範圍內
                //以確保WinForm執行期間Mutex一直存在
            }
        }

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
