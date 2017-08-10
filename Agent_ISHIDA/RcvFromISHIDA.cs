using System;
using System.IO;
using System.Data;
using System.Data.SqlClient;

namespace Agent_ISHIDA
{
    class RcvFromISHIDA
    {
        /// <summary>
        /// 主程式
        /// </summary>
        /// <param name="args"></param>
        public string MainFunction(string ORDER_TYPE, string OrderNo)
        {
            Program.comQryLCU.Step = "錯誤處理";
            string ErrMsg = "";
            string TXTfile_KeyValue = "";
            string TXTfile_ReturnValue = "";

            TXTfile_KeyValue = "SFD002";
            TXTfile_ReturnValue = "RFD100";

            Program.comQryLCU.Step = "下傳TXT";

            //開始下傳TXT, 根據指定的文字檔和裝置
            string FileName = "ISHIDA_" + TXTfile_KeyValue + ".csv"                 //檔案名稱
                , FileNameReturn = "ISHIDA_" + TXTfile_ReturnValue + ".csv"
                , FilePath = Program.FileDirectory + @"\" + FileName     //輸出文字檔目錄
                , FilePath_SendBackup = Program.FileDirectory_SendBackup + @"\" + DateTime.Now.ToString("yyyyMMddHHmmss") + FileName   //備份檔案
                , FilePath_ReturnBackup = Program.FileDirectory_ReturnBackup + @"\" + DateTime.Now.ToString("yyyyMMddHHmmss") + FileNameReturn;  //備份檔案
            #region 變數
            DataTable dt_ISHIDA_RFD100; //RFD100文字檔
            #endregion

            #region 呼叫sp轉換成TXT
            DataTable dt_Inbound_ISHIDA_TXT = Program.comQryISHIDA.GetTxtFromISHIDA(ref TXTfile_KeyValue, ref OrderNo);
            Program.comQryLCU.Agent_WriteLog(" 取得TXT Count:" + dt_Inbound_ISHIDA_TXT.Rows.Count.ToString());
            #endregion

            #region 有資料, 開始上傳
            if (dt_Inbound_ISHIDA_TXT.Rows.Count > 0)
            {
                Program.comQryLCU.Step = "回收TXT";

                #region 輸出指定編號的TXT
                using (StreamWriter sw_OutPutTXT = new StreamWriter(FilePath, false, System.Text.Encoding.UTF8))
                {
                    string data = "";
                    int ColumnCount = dt_Inbound_ISHIDA_TXT.Columns.Count;
                    int i = 0;

                    data += "\n";   //取代欄位定義, 第一行留白, 第二行起才是實際資料

                    foreach (DataRow row in dt_Inbound_ISHIDA_TXT.Rows)
                    {
                        i = 0;
                        foreach (DataColumn column in dt_Inbound_ISHIDA_TXT.Columns)
                        {
                            data += row[column].ToString().Trim();
                            i++;
                            if (i < ColumnCount)
                                data += ",";
                        }
                        data += "\n";
                        sw_OutPutTXT.Write(data);
                        data = "";
                    }
                    data += "\n";
                }
                #endregion

                #region 清除ftp舊資料
                Program.ftpclient.delete(FileName);
                Program.comQryLCU.Agent_WriteLog(" " + FileName + "刪除完成");
                Program.ftpclient.delete(FileNameReturn);
                Program.comQryLCU.Agent_WriteLog(" " + FileNameReturn + "刪除完成");
                #endregion

                #region 下傳TXT.tmp, 確定下傳完成後再更改名稱
                Program.ftpclient.upload(FileName + ".tmp", FilePath);

                ErrMsg = Program.comQryLCU.FTPCheckFileUploadOK(FileName + ".tmp", ref Program.ftpclient, 0);
                if (ErrMsg != "")
                {
                    return ErrMsg;
                }

                Program.ftpclient.rename(FileName + ".tmp", FileName);

                Program.comQryLCU.Agent_WriteLog(" " + FileName + "下傳成功");
                #endregion

                #region 檢查檔案是否已經成功上傳,ISHIDA會回傳【分揀實績】
                FilePath = Program.FileDirectory + @"\" + FileNameReturn;     //輸出文字檔目錄

                ErrMsg = Program.comQryLCU.FTPCheckFileUploadOK(FileNameReturn, ref Program.ftpclient, 0);
                if (ErrMsg != "")
                {
                    return ErrMsg;
                }
                #endregion

                #region 下載【ISHIDA分揀實績】
                Program.ftpclient.download(FileNameReturn, FilePath);
                if (File.Exists(FilePath))
                {
                    Program.comQryLCU.Agent_WriteLog(" " + FileNameReturn + " 下載成功");
                }
                else
                {
                    ErrMsg = " " + FileNameReturn + " 下載失敗";
                    return ErrMsg;
                }

                //備份回傳結果
                File.Copy(FilePath, FilePath_ReturnBackup);
                #endregion

                #region 【ISHIDA分揀實績】轉為C#_DataTable
                //CSV -> DataTable
                dt_ISHIDA_RFD100 = Program.comQryISHIDA.GetWhiteDT_ISHIDA_RFD100();
                DateTime BatchTime_RFD100 = DateTime.Now;
                using (StreamReader sr = new StreamReader(FilePath, System.Text.Encoding.UTF8))
                {
                    String AllData = sr.ReadToEnd();
                    AllData = AllData.Replace("\r", "");   //回收檔案有\r, 過濾掉
                    string[] rows = AllData.Split("\n".ToCharArray());
                    for (int row_Number = 1; row_Number < rows.Length; row_Number++)
                    {
                        String[] inputList = rows[row_Number].Split(',');
                        int inputListLength = inputList.Length;
                        int inputListForLoop = 0;

                        DataRow dr = dt_ISHIDA_RFD100.NewRow();
                        inputListLength = inputList.Length;
                        for (int StartIndex = inputListForLoop; StartIndex < inputListLength; StartIndex++)
                        {
                            dr[inputListForLoop] = inputList[StartIndex];
                            inputListForLoop++;
                        }

                        dr[36] = Program.comQryLCU.Parameter_DEVICE_AREA;
                        dr[37] = Program.comQryLCU.Parameter_DEVICE_ID;
                        dr[38] = OrderNo;
                        dr[39] = BatchTime_RFD100;
                        dt_ISHIDA_RFD100.Rows.Add(dr);
                    }
                }
                #endregion

                #region 上傳C#_DataTable給DB
                //開始傳送
                Program.comQryISHIDA.TXTTODB_RFD100(dt_ISHIDA_RFD100);

                Program.comQryLCU.Agent_WriteLog(" " + FileNameReturn + " 上傳DB成功");
                #endregion
            }
            #endregion

            #region 將上傳的資料移到備份區並作整理
            string IsUploadISHIDARFD100OK = Program.comQryISHIDA.UploadISHIDA_RFD100();
            Program.comQryLCU.Agent_WriteLog(IsUploadISHIDARFD100OK + " " + FileNameReturn + " 整理資料 成功");
            #endregion

            ErrMsg = "";

            return ErrMsg;
        }
    }
}