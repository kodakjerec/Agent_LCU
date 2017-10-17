using System;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Agent_LCU
{
    class RcvFromLCU
    {
        /*
         * 合併0124+0201+0301
         */
        /// <summary>
        /// 主程式
        /// </summary>
        /// <param name="args"></param>
        public string MainFunction(string ORDER_TYPE, string OrderNo, string IsForce)
        {
            Program.comQryLCU.Step = "錯誤處理";
            string ErrMsg = "";

            Program.comQryLCU.Step = "回收TXT";

            string FileName = ""     //檔案名稱
                , FilePath = Program.FileDirectory + @"\" + FileName;     //輸出文字檔目錄

            #region 變數
            DataTable dt_LCU0301;       //LCU0301文字檔
            #endregion

            #region 清除ftp舊LCU0301.TXT
            FileName = "LCU0301.TXT";
            Program.ftpclient.delete(FileName);
            Program.comQryLCU.Agent_WriteLog(" " + FileName + " 刪除完成");
            #endregion

            #region 傳送回收檔案的起始編號 HST0124.TXT
            FileName = "HST0124.TXT";
            FilePath = Program.FileDirectory + @"\" + FileName;
            string TXTfile_KeyValue = "HST0124";
            DataTable dt_Inbound_LCU_TXT = Program.comQryLCU.GetTxtFromLCU(ref TXTfile_KeyValue, ref OrderNo, IsForce);
            if (TXTfile_KeyValue=="FULL")
            {
                using (StreamWriter sw_OutPutHST0124 = new StreamWriter(FilePath, false, System.Text.Encoding.Default))
                {
                    string data = "";
                    foreach (DataRow row in dt_Inbound_LCU_TXT.Rows)
                    {
                        foreach (DataColumn column in dt_Inbound_LCU_TXT.Columns)
                        {
                            data += row[column].ToString() + ",";
                        }
                        data += "\n";
                        sw_OutPutHST0124.Write(data);
                        data = "";
                    }
                    data += "\n";
                }
                Program.ftpclient.upload(FileName, FilePath);
                Program.comQryLCU.Agent_WriteLog(" 起始位置：" + dt_Inbound_LCU_TXT.Rows[0][1].ToString());
            }
            #endregion

            #region 通知開始回收 HST0201.TXT
            FileName = "HST0201.TXT";
            FilePath = Program.FileDirectory + @"\" + FileName;

            using (StreamWriter sw_OutPutHST0201 = new StreamWriter(FilePath, false, System.Text.Encoding.Default))
            {
                sw_OutPutHST0201.Write("");
            }

            Program.ftpclient.upload(FileName, FilePath);
            #endregion

            #region 檢查LCU0301.TXT是否已經產生
            FileName = "LCU0301.TXT";
            FilePath = Program.FileDirectory + @"\" + FileName;

            ErrMsg = Program.comQryLCU.FTPCheckFileUploadOK(FileName, ref Program.ftpclient, 0);
            if (ErrMsg != "")
            {
                return ErrMsg;
            }
            #endregion

            #region 下載LCU0301.TXT
            Program.ftpclient.download(FileName, FilePath);
            if (File.Exists(FilePath))
            {
                Program.comQryLCU.Agent_WriteLog(" " + FileName + " 下載成功");
            }
            else
            {
                ErrMsg = " " + FileName + " 下載失敗";
                return ErrMsg;
            }
            #endregion

            #region LCU0301.TXT轉為C#_DataTable
            //TXT -> DataTable
            dt_LCU0301 = Program.comQryLCU.GetWhiteDT_LCU0301();
            DateTime BatchTime_LCU0301 = DateTime.Now;
            using (StreamReader sr = File.OpenText(FilePath))
            {
                String input = "";
                while ((input = sr.ReadLine()) != null)
                {
                    DataRow dr = dt_LCU0301.NewRow();
                    dr[0] = "CS";
                    dr[1] = input.Substring(0, 2);  //班次編號
                    dr[2] = input.Substring(2, 6);  //商品編號
                    dr[3] = input.Substring(8, 2);  //機械編號
                    dr[4] = input.Substring(10, 6); //店舖編號
                    dr[5] = input.Substring(16, 5); //售單價
                    dr[6] = input.Substring(21, 4); //成本
                    dr[7] = input.Substring(25, 1); //Close選擇
                    dr[8] = input.Substring(26, 3); //Opening
                    dr[9] = input.Substring(29, 4); //實績數
                    dr[10] = input.Substring(33, 8);//實績重量
                    dr[11] = input.Substring(41, 1);//計價方式
                    dr[12] = input.Substring(42, 1);//特價選擇
                    dr[13] = input.Substring(43, 8);//實績金額
                    dr[14] = input.Substring(51, 6);//加工年月日
                    dr[15] = input.Substring(57, 1);//處理選擇
                    dr[16] = input.Substring(58, 3);//部門編號
                    dr[17] = input.Substring(61, 8);//條碼內容
                    dr[18] = input.Substring(69, 1);//實績選擇
                    dr[19] = input.Substring(70, 5);//特價
                    dr[20] = input.Substring(75, 4);//指示數
                    dr[21] = input.Substring(79, 2);//加工時間
                    dr[22] = input.Substring(81, 6);//有效日
                    dr[23] = input.Substring(87, 2);//有效時間
                    dr[24] = input.Substring(89, 6);//值付開始時間
                    dr[25] = input.Substring(95, 6);//值付終了時間
                    dr[26] = input.Substring(101, 1);//［Anistu］定額標示
                    dr[27] = input.Substring(102, 1);//［Anistu］割引模式
                    dr[28] = input.Substring(103, 8);//［Anistu］計算重量
                    dr[29] = input.Substring(111, 8);//［Anistu］標籤重量
                    dr[30] = input.Substring(119, 4);//［Anistu］剩餘數
                    dr[31] = input.Substring(123, 4);//［Anistu］連番
                    dr[32] = input.Substring(127, 1);//系統型態
                    dr[33] = Program.comQryLCU.Parameter_DEVICE_AREA;
                    dr[34] = Program.comQryLCU.Parameter_DEVICE_ID;
                    dr[35] = OrderNo;
                    dr[36] = BatchTime_LCU0301;
                    dt_LCU0301.Rows.Add(dr);
                }
            }
            #endregion

            #region 上傳C#_DataTable給DB
            //開始傳送
            Program.comQryLCU.TXTTODB_LCU0301(dt_LCU0301);

            Program.comQryLCU.Agent_WriteLog(" " + FileName + " 上傳DB成功");
            #endregion

            #region 將上傳的資料移到備份區並作整理
            string IsUploadLCU0301OK = Program.comQryLCU.Upload_LCU0301();
            Program.comQryLCU.Agent_WriteLog(IsUploadLCU0301OK + " " + FileName + " 整理資料 成功");
            #endregion

            ErrMsg = "";

            return ErrMsg;
        }
    }
}
