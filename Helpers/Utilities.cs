using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
//using accAfpslaiEmvSrvc.Models;
using accAfpslaiEmvObjct;

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

        public static void SavePayloadWithResponse(requestPayload reqPayload, string apiResponse)
        {
            var payloadAuthEncrypted = reqPayload.authentication;
            var payloadAuth = Newtonsoft.Json.JsonConvert.DeserializeObject<requestCredential>(accAfpslaiEmvEncDec.Aes256CbcEncrypter.Decrypt(payloadAuthEncrypted));

            string directoryPath = string.Format(@"{0}\PAYLOAD\{1}\{2}", Properties.Settings.Default.LogRepo, Convert.ToDateTime(payloadAuth.dateRequest).ToString("yyyy-MM-dd"), payloadAuth.branch);
            string fileName = string.Format(@"{0}\{1}_{2}.txt", directoryPath, payloadAuth.userName, Convert.ToDateTime(payloadAuth.dateRequest).ToString("yyyyMMdd_hhmmss"), payloadAuth.branch);
            if (!System.IO.Directory.Exists(directoryPath)) System.IO.Directory.CreateDirectory(directoryPath);
            System.IO.File.WriteAllText(fileName, Newtonsoft.Json.JsonConvert.SerializeObject(reqPayload) + Environment.NewLine + Environment.NewLine + "api response:" + Environment.NewLine + apiResponse);
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

        public static void SaveApiRequestLog(api_request_log arl)
        {          
            afpslai_emvEntities ent = new afpslai_emvEntities();
            arl.date_post = DateTime.Now.Date;
            arl.time_post = DateTime.Now.TimeOfDay;
            ent.api_request_log.Add(arl);
            ent.SaveChanges();
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

                    TimeSpan ts = System.DateTime.Now - Convert.ToDateTime(payloadAuth.dateRequest);

                    //save payload
                    try
                    {
                        string branch = "OTHER";
                        if (!string.IsNullOrEmpty(payloadAuth.branch)) branch = payloadAuth.branch;
                        string directoryPath = string.Format(@"{0}\PAYLOAD\{1}\{2}", Properties.Settings.Default.LogRepo, Convert.ToDateTime(payloadAuth.dateRequest).ToString("yyyy-MM-dd"), branch);
                        string fileName = string.Format(@"{0}\{1}_{2}.txt", directoryPath, payloadAuth.userName, Convert.ToDateTime(payloadAuth.dateRequest).ToString("yyyyMMdd_hhmmss"), branch);
                        if (!System.IO.Directory.Exists(directoryPath)) System.IO.Directory.CreateDirectory(directoryPath);
                        System.IO.File.WriteAllText(fileName, Newtonsoft.Json.JsonConvert.SerializeObject(reqPayload));
                    }
                    catch (Exception ex)
                    {
                        accAfpslaiEmvSrvc.Controllers.ValuesController.logger.Error(ex.Message);
                    }

                    bool isAuthorize = true;

                    if (ts.TotalMinutes <= 30)
                    {
                        if (accAfpslaiEmvEncDec.Aes256CbcEncrypter.Decrypt(payloadAuth.key) == Properties.Settings.Default.ApiAuth)
                        {
                            afpslai_emvEntities ent = new afpslai_emvEntities();

                            string userName = payloadAuth.userName;
                            string userPass = payloadAuth.userPass;

                            //var user = ent.system_user.Where(o => o.user_name.Equals(userName) && o.user_pass.Equals(userPass));
                            //if (user.Count() == 0) isAuthorize = false; else userId = payloadAuth.userId;

                            //var oldUser = ent.system_user.Where(o => o.user_name.Equals(userName));
                            //string dbPass = accAfpslaiEmvEncDec.Aes256CbcEncrypter.Decrypt(user.FirstOrDefault().user_pass);
                            //string paramPass = accAfpslaiEmvEncDec.Aes256CbcEncrypter.Decrypt(userPass);
                            var user = ent.system_user.Where(o => o.user_name.Equals(userName));
                            //string dbPass = user.FirstOrDefault().user_pass;
                            //string paramPass = userPass;
                            string dbPass = accAfpslaiEmvEncDec.Aes256CbcEncrypter.Decrypt(user.FirstOrDefault().user_pass);
                            string paramPass = accAfpslaiEmvEncDec.Aes256CbcEncrypter.Decrypt(userPass);
                            bool isMatch = (dbPass == paramPass);                                                     

                            if (!isMatch) isAuthorize = false; else userId = payloadAuth.userId;

                        }
                        else isAuthorize = false;
                    }
                    else isAuthorize = false;

                    if (isAuthorize) return 0; else return (int)System.Net.HttpStatusCode.Unauthorized;
                }
            }
            catch (Exception ex)
            {
                accAfpslaiEmvSrvc.Controllers.ValuesController.logger.Error(ex.Message);
                return (int)System.Net.HttpStatusCode.InternalServerError;
            }            
        }

        public static bool wiseCardcardBindCifNo(cbsCms cbsCms, ref cmsResponse cmsResponse, ref string msg)
        {
            string soapResponse = "";
            string err = "";            
            string soapStr = Newtonsoft.Json.JsonConvert.SerializeObject(new cmsRequest { cif = cbsCms.cif, cardNo = cbsCms.cardNo, mobileNo = cbsCms.mobileNo});
            bool response = ExecuteApiRequest(Properties.Settings.Default.WiseCard_cardBindCifNo_Url, soapStr, ref soapResponse, ref err);
            if (response)
            {
                cmsResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<cmsResponse>(soapResponse);

                if (cmsResponse.resultCode == "00")
                {
                    msg = cmsResponse.resultMessage;

                    return true;
                }
                else
                {
                    msg = string.Format("{0} {1}", cmsResponse.resultCode, cmsResponse.resultMessage);
                    return false;
                }
            }
            else
            {                
                msg = err;
                return false;
            }
        }

        public static bool ExecuteApiRequest(string url, string soapStr, ref string soapResponse, ref string err)
        {
            //System.Net.HttpWebRequest myHttpWebRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();

            try
            {
                var uri = new Uri(url);
                string baseUrl = string.Format("http://{0}", uri.Authority);
                if (url.Contains("https://")) baseUrl = string.Format("https://{0}", uri.Authority);
                string otherUrl = uri.LocalPath;

                client.BaseAddress = new Uri(baseUrl);                
                //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", txtToken.Text);

                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var buffer = System.Text.Encoding.UTF8.GetBytes(soapStr);
                var byteContent = new System.Net.Http.ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                byteContent.Headers.ContentLength = buffer.Length;

                System.Net.Http.HttpResponseMessage response = client.PostAsync(otherUrl, byteContent).Result;
                if (response.IsSuccessStatusCode)
                {
                    soapResponse = response.Content.ReadAsStringAsync().Result;
                    return true;
                }
                else
                {
                    err = string.Format("{0} {1}", response.StatusCode, response.ReasonPhrase);
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Equals("One or more errors occurred.")) err = "Unable to reach middle server api.";
                else err = ex.Message;
                return false;
            }
            finally
            {
                client.Dispose();                
            }
        }       

            

    }
}