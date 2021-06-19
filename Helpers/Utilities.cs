using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using accAfpslaiEmvSrvc.Models;


namespace accAfpslaiEmvSrvc.Helpers
{
    public class Utilities
    {      

        public static string HandleErrorMessage(string errMsg)
        {
            if (errMsg.Contains("Authentication to host")) return "Invalid database credential";
            else return errMsg;
        }

        public static void SavePayload(requestPayload reqPayload)
        {
            var payloadAuthEncrypted = reqPayload.authentication;
            var payloadAuth = Newtonsoft.Json.JsonConvert.DeserializeObject<requestCredential>(accAfpslaiEmvEncDec.Aes256CbcEncrypter.Decrypt(payloadAuthEncrypted));

            string directoryPath = string.Format(@"{0}\PAYLOAD\{1}\{2}", Properties.Settings.Default.LogRepo, Convert.ToDateTime(payloadAuth.dateRequest).ToString("yyyy-MM-dd"), payloadAuth.branch);
            string fileName = string.Format(@"{0}\{1}_{2}.txt", directoryPath, payloadAuth.userName, Convert.ToDateTime(payloadAuth.dateRequest).ToString("yyyyMMdd_hhmmss"), payloadAuth.branch);
            if (!System.IO.Directory.Exists(directoryPath)) System.IO.Directory.CreateDirectory(directoryPath);
            System.IO.File.WriteAllText(fileName, Newtonsoft.Json.JsonConvert.SerializeObject(reqPayload));
        }

        public static void SaveSystemLog(string system, int userId, string logDesc)
        {
            system_log log = new system_log();
            log.system = system;
            log.log_desc = logDesc;
            log.log_type = "System";
            log.user_id = userId;
            AddSysLog(log);
        }

        public static void AddSysLog(system_log system_log)
        {
            afpslai_emvEntities ent = new afpslai_emvEntities();
            system_log.date_post = DateTime.Now.Date;
            system_log.time_post = DateTime.Now.TimeOfDay;
            ent.system_log.Add(system_log);
            ent.SaveChanges();
        }

        public static int ValidateRequest(requestPayload reqPayload, ref int userId) //, ref string payload)
        {
            try
            {
                if (string.IsNullOrEmpty(reqPayload.authentication)) return (int)System.Net.HttpStatusCode.BadRequest;
                else
                {
                    var payloadAuthEncrypted = reqPayload.authentication;
                    var payloadAuth = Newtonsoft.Json.JsonConvert.DeserializeObject<requestCredential>(accAfpslaiEmvEncDec.Aes256CbcEncrypter.Decrypt(payloadAuthEncrypted));
                    //if (reqPayload.payload.obj != null) payload = reqPayload.payload.obj.ToString();
                    //payload = reqPayload.payload;

                    //save payload
                    string directoryPath = string.Format(@"{0}\PAYLOAD\{1}\{2}", Properties.Settings.Default.LogRepo, Convert.ToDateTime(payloadAuth.dateRequest).ToString("yyyy-MM-dd"), payloadAuth.branch);
                    string fileName = string.Format(@"{0}\{1}_{2}.txt", directoryPath, payloadAuth.userName, Convert.ToDateTime(payloadAuth.dateRequest).ToString("yyyyMMdd_hhmmss"), payloadAuth.branch);
                    if (!System.IO.Directory.Exists(directoryPath)) System.IO.Directory.CreateDirectory(directoryPath);
                    System.IO.File.WriteAllText(fileName, Newtonsoft.Json.JsonConvert.SerializeObject(reqPayload));

                    if (accAfpslaiEmvEncDec.Aes256CbcEncrypter.Decrypt(payloadAuth.key) == Properties.Settings.Default.ApiAuth)
                    {
                        afpslai_emvEntities ent = new afpslai_emvEntities();
                        var user = ent.system_user.Where(o => o.user_name.Equals(payloadAuth.userName) && o.user_pass.Equals(payloadAuth.userPass));
                        if (user.Count() == 0) return (int)System.Net.HttpStatusCode.Unauthorized;
                        else
                        {
                            userId = payloadAuth.userId;
                            return 0;
                        }
                    }
                    else return (int)System.Net.HttpStatusCode.Unauthorized;
                }
            }
            catch (Exception ex)
            {
                accAfpslaiEmvSrvc.Controllers.ValuesController.logger.Error(ex.Message);
                return (int)System.Net.HttpStatusCode.InternalServerError;
            }            
        }    


        //public static string GetEnumValue()
        //{
        //    string description = ((DataKeysEnum.table)value).AsString(Enum. EnumFormat.Description);
        //}       

    }
}