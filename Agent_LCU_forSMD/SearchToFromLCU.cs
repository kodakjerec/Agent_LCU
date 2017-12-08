using System;
using System.IO;
using System.Threading;

namespace Agent_LCU
{
    class SearchToFromLCU
    {
        /// <summary>
        /// 主程式
        /// </summary>
        /// <param name="args"></param>
        public string MainFunction(string ORDER_TYPE)
        {
            Program.comQryLCU.Step = "錯誤處理";
            string ErrMsg = "";
            string TXTfile_KeyValue = "";

            switch (ORDER_TYPE)
            {
                case "111":
                    //商品
                    TXTfile_KeyValue = "HST0221"; break;
                case "112":
                    //店鋪
                    TXTfile_KeyValue = "HST0222"; break;
                case "115":
                    //分揀
                    TXTfile_KeyValue = "HST0223"; break;
                case "113":
                    //添加物
                    TXTfile_KeyValue = "HST0229"; break;
                case "114":
                    //班別
                    TXTfile_KeyValue = "HST0233"; break;
            }

            Program.comQryLCU.Step = "查詢TXT";

            #region 開始下傳TXT, 根據指定的文字檔和裝置
            string FileName = TXTfile_KeyValue + ".TXT"                 //檔案名稱
                , FilePath = Program.FileDirectory + @"\" + FileName     //輸出文字檔目錄
                , FileNameReturn = TXTfile_KeyValue.Replace("HST02", "LCU03") + ".TXT"    //檔案名稱(回收)
                , FilePathReturn = Program.FileDirectory + @"\" + FileNameReturn;//輸出文字檔目錄(回收)
            #endregion

            #region 根據檔案類別, 輸出空白TXT
            using (StreamWriter sw_OutPutSPACETXT = new StreamWriter(FilePath, false, System.Text.Encoding.Default))
            {
                sw_OutPutSPACETXT.Write("");
            }
            Program.ftpclient.upload(FileName, FilePath);
            Program.comQryLCU.Agent_WriteLog(" " + FileName + " 下傳成功");

            //不會回傳訊息, 給包裝機緩衝時間
            Thread.Sleep(2000);

            #endregion

            #region 檢查回收檔案是否已經產生
            ErrMsg = Program.comQryLCU.FTPCheckFileUploadOK(FileNameReturn, ref Program.ftpclient, 0);
            if (ErrMsg != "")
            {
                return ErrMsg;
            }
            #endregion

            #region 下載回收檔案
            Program.ftpclient.download(FileNameReturn, FilePathReturn);
            if (File.Exists(FilePathReturn))
            {
                Program.comQryLCU.Agent_WriteLog(" " + FileNameReturn + " " + "下載成功");
            }
            else
            {
                ErrMsg = " " + FileNameReturn + " " + "下載失敗";
                return ErrMsg;
            }
            #endregion

            return ErrMsg;
        }
    }
}