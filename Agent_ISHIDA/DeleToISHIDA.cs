using System;
using System.IO;
using System.Data;

namespace Agent_ISHIDA
{
    class DeleToISHIDA
    {
        /// <summary>
        /// 主程式
        /// </summary>
        /// <param name="args"></param>
        public string MainFunction(string ORDER_TYPE, string GUID)
        {
            Program.comQryLCU.Step = "錯誤處理";
            string ErrMsg = "";
            string TXTfile_KeyValue = "";
            string TXTfile_ReturnValue = "";

            switch (ORDER_TYPE)
            {
                case "111":
                    //商品
                    TXTfile_KeyValue = "SFM001";
                    TXTfile_ReturnValue = "RFM001";
                    break;
                case "112":
                    //店鋪
                    TXTfile_KeyValue = "SFM002";
                    TXTfile_ReturnValue = "RFM002";
                    break;
                case "113":
                    //添加物.產地
                    TXTfile_KeyValue = "SFM005";
                    TXTfile_ReturnValue = "RFM005";
                    break;
                case "114":
                    //班別
                    TXTfile_KeyValue = "SFM007";
                    TXTfile_ReturnValue = "RFM007";
                    break;
                case "115":
                    //分揀
                    TXTfile_KeyValue = "SFD001";
                    TXTfile_ReturnValue = "RFD001";
                    break;
                case "120":
                    //實績
                    TXTfile_KeyValue = "SFD003";
                    TXTfile_ReturnValue = "RFD003";
                    break;
            }

            Program.comQryLCU.Step = "下傳TXT";

            //開始下傳TXT, 根據指定的文字檔和裝置
            string FileName = "ISHIDA_" + TXTfile_KeyValue + ".csv"                 //檔案名稱
                , FileNameReturn = "ISHIDA_" + TXTfile_ReturnValue + "_ANS.csv"
                , FilePath = Program.FileDirectory + @"\" + FileName     //輸出文字檔目錄
                , FilePath_SendBackup = Program.FileDirectory_SendBackup + @"\" + DateTime.Now.ToString("yyyyMMddHHmmss") + FileName   //備份檔案
                , FilePath_ReturnBackup = Program.FileDirectory_ReturnBackup + @"\" + DateTime.Now.ToString("yyyyMMddHHmmss") + FileNameReturn;  //備份檔案

            #region 呼叫sp轉換成TXT
            DataTable dt_Inbound_ISHIDA_TXT = new DataTable();
            if (ORDER_TYPE == "115")
            {
                string delFileName = "SFD001DEL";
                dt_Inbound_ISHIDA_TXT = Program.comQryISHIDA.GetTxtFromISHIDA(ref delFileName, ref GUID);
            }
            else
            {
                dt_Inbound_ISHIDA_TXT = Program.comQryISHIDA.GetTxtFromISHIDA(ref TXTfile_KeyValue, ref GUID);
            }
            Program.comQryLCU.Agent_WriteLog(" 取得TXT Count:" + dt_Inbound_ISHIDA_TXT.Rows.Count.ToString());
            #endregion

            #region 刪除特有 更新區分改為3
            foreach (DataRow row in dt_Inbound_ISHIDA_TXT.Rows)
            {
                if (row["FileDefine"].ToString() == TXTfile_KeyValue)
                    row["Action"] = "3";
            }
            #endregion

            #region 有資料, 開始上傳
            ErrMsg = "";
            if (dt_Inbound_ISHIDA_TXT.Rows.Count > 0)
            {
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

                //備份上傳資料
                File.Copy(FilePath, FilePath_SendBackup);
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

                #region 檢查檔案是否已經成功上傳,ISHIDA會回傳【結果】
                FilePath = Program.FileDirectory + @"\" + FileNameReturn;     //輸出文字檔目錄

                ErrMsg = Program.comQryLCU.FTPCheckFileUploadOK(FileNameReturn, ref Program.ftpclient, 0);
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
                string AllData = "";
                using (StreamReader s = new StreamReader(FilePath, System.Text.Encoding.UTF8))
                {
                    AllData = s.ReadToEnd();
                }
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
                            Program.comQryLCU.Agent_WriteLog(" 成功刪除筆數" + items[4]);
                        }
                    }
                }

                #endregion
            }
            #endregion

            return ErrMsg;
        }
    }
}