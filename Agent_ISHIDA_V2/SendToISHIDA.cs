using System;
using System.IO;
using System.Data;

namespace Agent_ISHIDA
{
    class SendToISHIDA
    {
        /// <summary>
        /// 主程式
        /// </summary>
        /// <param name="args"></param>
        public string MainFunction(string ORDER_TYPE, string GUID)
        {
            Program.comQryLCU.Step = "錯誤處理";
            string ErrMsg = "";

            #region 多檔傳送的DataTable
            DataTable dt_FileList = new DataTable("dt_FileList");
            dt_FileList.Columns.Add("KeyValue", typeof(string));
            dt_FileList.Columns.Add("ReturnValue", typeof(string));
            #endregion

            switch (ORDER_TYPE)
            {
                case "11":
                    //商品
                    dt_FileListAddRow(dt_FileList, "SFM001", "RFM001");
                    break;
                case "12":
                    //店鋪
                    dt_FileListAddRow(dt_FileList, "SFM002", "RFM002");
                    break;
                case "15":
                    //分揀
                    dt_FileListAddRow(dt_FileList, "SFD001", "RFD001");
                    break;
                case "13":
                    //添加物.產地
                    dt_FileListAddRow(dt_FileList, "SFM005", "RFM005");
                    break;
                case "14":
                    //保存方法
                    dt_FileListAddRow(dt_FileList, "SFM006", "RFM006");
                    //班別
                    dt_FileListAddRow(dt_FileList, "SFM007", "RFM007");
                    //托盤
                    dt_FileListAddRow(dt_FileList, "SFM008", "RFM008");
                    //廣告文
                    dt_FileListAddRow(dt_FileList, "SFM009", "RFM009");
                    break;
                default:
                    ////注釋
                    //TXTfile_KeyValue = "SFM004";
                    //托盤
                    dt_FileListAddRow(dt_FileList, ORDER_TYPE, ORDER_TYPE.Replace("SFM", "RFM"));
                    break;
            }

            foreach (DataRow dr in dt_FileList.Rows)
            {
                ErrMsg = TransTXT(dr[0].ToString(), dr[1].ToString(), GUID);
            }

            return ErrMsg;
        }

        /// <summary>
        /// 新增Row
        /// </summary>
        /// <param name="dt1"></param>
        /// <param name="keyValue"></param>
        /// <param name="returnValue"></param>
        private void dt_FileListAddRow(DataTable dt1, string keyValue, string returnValue)
        {
            DataRow dr = dt1.NewRow();
            dr[0] = keyValue;
            dr[1] = returnValue;
            dt1.Rows.Add(dr);
        }

        /// <summary>
        /// 傳檔主程式
        /// 因為工作17 有複數主檔要傳送, 特別改為函數
        /// </summary>
        /// <param name="TXTfile_KeyValue">傳給LCU的檔案名稱</param>
        /// <param name="TXTfile_ReturnValue">LCU回傳的檔案名稱</param>
        /// <param name="GUID">批次</param>
        /// <returns>錯誤訊息</returns>
        private string TransTXT(string TXTfile_KeyValue, string TXTfile_ReturnValue, string GUID)
        {
            Program.comQryLCU.Step = "下傳TXT_" + TXTfile_KeyValue;

            string ErrMsg = "";

            //開始下傳TXT, 根據指定的文字檔和裝置
            string FileName = "ISHIDA_" + TXTfile_KeyValue + ".csv"                 //檔案名稱
                , FileNameReturn = "ISHIDA_" + TXTfile_ReturnValue + "_ANS.csv"
                , FilePath = Program.FileDirectory + @"\" + FileName     //輸出文字檔目錄
                , FilePath_SendBackup = Program.FileDirectory_SendBackup+@"\" + DateTime.Now.ToString("yyyyMMddHHmmss") + FileName   //備份檔案
                , FilePath_ReturnBackup = Program.FileDirectory_ReturnBackup + @"\" + DateTime.Now.ToString("yyyyMMddHHmmss") + FileNameReturn;  //備份檔案

            #region 呼叫sp轉換成TXT
            DataTable dt_Inbound_ISHIDA_TXT = Program.comQryISHIDA.GetTxtFromISHIDA_V2(ref TXTfile_KeyValue, ref GUID);
            Program.comQryLCU.Agent_WriteLog(" 取得TXT Count:" + dt_Inbound_ISHIDA_TXT.Rows.Count.ToString());
            #endregion

            #region 有資料, 開始上傳
            if (dt_Inbound_ISHIDA_TXT.Rows.Count > 0)
            {
                #region 輸出指定編號的TXT
                StreamWriter sw_OutPutTXT = new StreamWriter(FilePath, false, System.Text.Encoding.UTF8);
                string data = "";
                int ColumnCount = dt_Inbound_ISHIDA_TXT.Columns.Count;
                int i = 0;

                data += "\n";   //取代欄位定義, 第一行留白, 第二行起才是實際資料

                foreach (DataRow row in dt_Inbound_ISHIDA_TXT.Rows)
                {
                    i = 0;
                    foreach (DataColumn column in dt_Inbound_ISHIDA_TXT.Columns)
                    {
                        data += row[column].ToString();
                        i++;
                        if (i < ColumnCount)
                            data += ",";
                    }
                    data += "\n";
                    sw_OutPutTXT.Write(data);
                    data = "";
                }
                data += "\n";

                sw_OutPutTXT.Dispose();
                sw_OutPutTXT.Close();

                //備份上傳資料
                File.Copy(FilePath, FilePath_SendBackup);
                #endregion

                //V2專用, 禁止下傳到正式主機
                Program.comQryLCU.Agent_WriteLog("V2禁止下傳到正式主機");
                return "";

                #region 清除ftp舊資料
                Program.ftpclient.delete(FileName);
                Program.comQryLCU.Agent_WriteLog(" " + FileName + "刪除完成");
                Program.ftpclient.delete(FileNameReturn);
                Program.comQryLCU.Agent_WriteLog(" " + FileNameReturn + "刪除完成");
                #endregion

                #region 下傳TXT.tmp, 確定下傳完成後再更改名稱
                Program.ftpclient.upload(FileName + ".tmp", FilePath);

                ErrMsg = Program.comQryLCU.FTPCheckFileUploadOK(FileName + ".tmp", ref Program.ftpclient,0);
                if (ErrMsg != "")
                {
                    return ErrMsg;
                }

                Program.ftpclient.rename(FileName + ".tmp", FileName);

                Program.comQryLCU.Agent_WriteLog(" " + FileName + "下傳成功");
                #endregion

                #region 檢查檔案是否已經成功上傳,ISHIDA會回傳【結果】
                FilePath = Program.FileDirectory + @"\" + FileNameReturn;     //輸出文字檔目錄

                ErrMsg=Program.comQryLCU.FTPCheckFileUploadOK(FileNameReturn, ref Program.ftpclient,0);
                if (ErrMsg != "")
                {
                    return ErrMsg;
                }
                #endregion

                #region 下載【ISHIDA回傳結果】
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

                #region 解析【ISHIDA回傳結果】是否正常
                StreamReader s = new StreamReader(FilePath, System.Text.Encoding.UTF8);
                string AllData = s.ReadToEnd();
                string[] rows = AllData.Split("\n".ToCharArray());


                foreach (string r in rows)
                {
                    string[] items = r.Split(',');
                    //第一行是欄位名稱
                    //第二行裁示回傳結果
                    if (items[0] == TXTfile_ReturnValue)
                    {
                        //異常
                        if (items[1] == "9")
                        {
                            string MSG = items[3];
                            MSG = MSG.Substring(MSG.IndexOf("(") + 1, MSG.IndexOf(")") - MSG.IndexOf("(") - 1);
                            switch (MSG)
                            {
                                case "400040":  //已經生產完成
                                    break;
                                case "500030":  //找不到要刪除的資料
                                    break;
                                case "500050":  //要登錄的資料已有登錄了
                                    break;
                                case "500060":  //主檔中未登錄
                                    break;
                                default:
                                    ErrMsg += "ERROR: Line:" + items[2] + " Msg:" + items[3] + "\n";
                                    break;
                            }
                            continue;
                        }
                        else
                        {
                            if (ErrMsg != "")
                            {
                                Program.comQryLCU.Agent_WriteLog(ErrMsg);
                                string UpdateDB = "SFD001NO";
                                Program.comQryISHIDA.GetTxtFromISHIDA_V2(ref UpdateDB, ref GUID);
                                return "";
                            }
                            else
                                Program.comQryLCU.Agent_WriteLog(" 成功更新筆數" + items[4]);
                        }
                    }
                }
                #endregion

                #region 下傳txt成功後，回寫輸出檔
                if (TXTfile_KeyValue == "SFD001")
                {
                    string UpdateDB = "SFD001OK";
                    Program.comQryISHIDA.GetTxtFromISHIDA_V2(ref UpdateDB, ref GUID);
                }
                #endregion
            }
            #endregion

            return ErrMsg;
        }

        #region 其他函數
        /// <summary>
        /// 比對兩個時間的差異秒數
        /// </summary>
        /// <param name="d1">時間1(較新)</param>
        /// <param name="d2">時間2(較晚)</param>
        /// <returns></returns>
        private static int CompareTwoTime(DateTime d1, DateTime d2)
        {
            TimeSpan t1 = new TimeSpan(d1.Ticks),
                     t2 = new TimeSpan(d2.Ticks);
            TimeSpan ts = t1.Subtract(t2).Duration();
            return ts.Seconds;
        }
        #endregion
    }
}
