using System;
using System.IO;
using System.Data;
using System.Threading;

namespace Agent_LCU
{
    class DeleToLCU
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
                    TXTfile_KeyValue = "HST0121"; break;
                case "112":
                    //店鋪
                    TXTfile_KeyValue = "HST0122"; break;
                case "115":
                    //分揀
                    TXTfile_KeyValue = "HST0123"; break;
                case "113":
                    //添加物
                    TXTfile_KeyValue = "HST0129"; break;
                case "114":
                    //班別
                    TXTfile_KeyValue = "HST0133"; break;
                case "120":
                    //實績
                    TXTfile_KeyValue = "HST0125"; break;
            }

            Program.comQryLCU.Step = "刪除TXT";

            string FileName = TXTfile_KeyValue + ".TXT"                 //檔案名稱
                , FilePath = Program.FileDirectory + @"\" + FileName;     //輸出文字檔目錄

            #region 根據檔案類別, 輸出空白TXT
            using (StreamWriter sw_OutPutSPACETXT = new StreamWriter(FilePath, false, System.Text.Encoding.Default))
            {
                sw_OutPutSPACETXT.Write("CS");
                sw_OutPutSPACETXT.Dispose();
                sw_OutPutSPACETXT.Close();
                Program.ftpclient.upload(FileName, FilePath);
                Program.comQryLCU.Agent_WriteLog(" " + FileName + " 下傳成功");

                //不會回傳訊息, 給包裝機緩衝時間
                Thread.Sleep(2000);
            }
            #endregion

            return ErrMsg;
        }
    }
}