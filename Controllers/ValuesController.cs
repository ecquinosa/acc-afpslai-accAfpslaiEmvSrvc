
using System;
using System.Linq;
using System.Web.Http;
//using accAfpslaiEmvSrvc.Helpers;
//sing accAfpslaiEmvSrvc.Models;
using accAfpslaiEmvObjct;

namespace accAfpslaiEmvSrvc.Controllers
{
    public class ValuesController : ApiController
    {

        public static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private int authUserId = 0;
        private string dateFormat = "yyyy-MM-dd";
        //private int processId;

        #region " Select/ Get "

        [Route("~/api/getOnlineRegistration")]
        [HttpPost]
        public IHttpActionResult GetOnlineRegistration(requestPayload reqPayload)
        {
            try
            {
                //Random rn = new Random();
                //processId = rn.Next()

                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());
                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        string cif = "";
                        string reference_number = "";

                        if (objPayload.cif != null) cif = objPayload.cif;
                        if (objPayload.reference_number != null) reference_number = objPayload.reference_number;

                        if (string.IsNullOrEmpty(cif) && string.IsNullOrEmpty(reference_number)) return apiResponse(new responseFailedBadRequest { message = "Empty cif or reference numbber" });
                        else
                        {
                            afpslai_emvEntities ent = new afpslai_emvEntities();
                            var obj = ent.online_registration.Where(o => (o.cif.Equals(cif)) || (o.reference_number.Equals(reference_number)));

                            return apiResponse(new response { result = 0, obj = obj });
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/pushCMSData")]
        [HttpPost]
        public IHttpActionResult PushCMSData(requestPayload reqPayload)
        //public IHttpActionResult PushCMSData(cbsCms cbsCms)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());
                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var cbsCms = Newtonsoft.Json.JsonConvert.DeserializeObject<cbsCms>(objPayload.ToString());

                        Helpers.Utilities.SavePayloadWithResponse(reqPayload, Newtonsoft.Json.JsonConvert.SerializeObject(reqPayload));
                        string msg = "";
                        cmsResponse cmsResponse = null;
                        if (Helpers.Utilities.wiseCardcardBindCifNo(cbsCms, ref cmsResponse, ref msg))
                        {
                            Helpers.Utilities.SavePayloadWithResponse(reqPayload, Newtonsoft.Json.JsonConvert.SerializeObject(cmsResponse));
                            return apiResponse(new responseSuccess());
                        }
                        else
                        {
                            api_request_log arl = new api_request_log();
                            arl.card_id = cbsCms.cardId;
                            arl.api_owner = "cms";
                            arl.api_name = Properties.Settings.Default.WiseCard_cardBindCifNo_Url.Substring(Properties.Settings.Default.WiseCard_cardBindCifNo_Url.LastIndexOf("/") + 1);
                            arl.is_success = false;
                            arl.request = objPayload.ToString();
                            arl.response = msg;
                            Helpers.Utilities.SaveApiRequestLog(arl);
                            Helpers.Utilities.SavePayloadWithResponse(reqPayload, Newtonsoft.Json.JsonConvert.SerializeObject(cmsResponse));
                            logger.Error(string.Format("Failed to bind cif {0} and card no {1} to CMS. {2}", cbsCms.cif, cbsCms.cardNo, msg));
                            if (!Properties.Settings.Default.IsEnforcePMS) return apiResponse(new response { result = 1, obj = string.Format("Failed to bind cif {0} and card no {1} to CMS. {2}", cbsCms.cif, cbsCms.cardNo, msg) });
                            else return apiResponse(new responseSuccess());
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                if (!Properties.Settings.Default.IsEnforcePMS) return apiResponse(new responseFailedSystemError { obj = ex.Message });
                else return apiResponse(new responseSuccess());
            }
        }

        [Route("~/api/pullCBSData")]
        [HttpPost]
        public IHttpActionResult PullCBSData(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());
                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);

                        string cif = "";

                        if (objPayload.cif != null) cif = objPayload.cif;

                        if (string.IsNullOrEmpty(cif)) return apiResponse(new responseFailedBadRequest { message = "Missing required field" });
                        else
                        {
                            string[] lines = System.IO.File.ReadAllLines(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).Replace("file:\\", "") + @"\cbs.txt");
                            string selectedLine = "";
                            foreach (var line in lines)
                            {
                                if (line.Trim() != "")
                                {
                                    if (line.Split('|')[0] == cif)
                                    {
                                        selectedLine = line;
                                        break;
                                    }
                                }
                            }

                            cbsData memberCBS = new cbsData();
                            if (selectedLine != "")
                            {
                                string[] selLineArr = selectedLine.Split('|');

                                if (lines != null)
                                {
                                    memberCBS.cif = selLineArr[0];
                                    memberCBS.first_name = selLineArr[1];
                                    memberCBS.middle_name = selLineArr[2];
                                    memberCBS.last_name = selLineArr[3];
                                    memberCBS.suffix = selLineArr[4];
                                    memberCBS.gender = selLineArr[5];
                                    memberCBS.civilStatus = selLineArr[6];
                                    memberCBS.membership_date = Convert.ToDateTime(selLineArr[7]);
                                    memberCBS.membershipStatus = selLineArr[8];
                                    memberCBS.membershipType = selLineArr[9];
                                    memberCBS.date_birth = Convert.ToDateTime(selLineArr[10]);
                                    memberCBS.contact_nos = selLineArr[11];
                                    memberCBS.mobile_nos = selLineArr[12];
                                    memberCBS.address1 = selLineArr[13];
                                    memberCBS.address2 = selLineArr[14];
                                    memberCBS.address3 = selLineArr[15];
                                    memberCBS.city = selLineArr[16];
                                    memberCBS.province = selLineArr[17];
                                    memberCBS.zipCode = selLineArr[18];
                                    memberCBS.emergency_contact_name = selLineArr[19];
                                    memberCBS.emergency_contact_nos = selLineArr[20];
                                    memberCBS.associateType = selLineArr[21];
                                    memberCBS.principal_cif = selLineArr[22];
                                    memberCBS.principal_name = selLineArr[23];
                                    memberCBS.cca_no = selLineArr[24];

                                    return apiResponse(new response { result = 0, obj = memberCBS });
                                }
                            }
                            else
                            {
                                api_request_log arl = new api_request_log();
                                arl.api_owner = "cbs";
                                arl.api_name = "pullCBSData";
                                arl.is_success = false;
                                arl.request = cif;
                                arl.response = "No record found";
                                Helpers.Utilities.SaveApiRequestLog(arl);
                            }

                            return apiResponse(new response { result = 0, obj = memberCBS });
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/pushCBSData")]
        [HttpPost]
        public IHttpActionResult PushCBSData(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());
                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        //afpslai_emvEntities ent = new afpslai_emvEntities();
                        //var obj = ent.dcs_system_setting;

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var cbsCms = Newtonsoft.Json.JsonConvert.DeserializeObject<cbsCms>(objPayload.ToString()); ;

                        string cardNo = cbsCms.cardNo;
                        if (Properties.Settings.Default.CBS_CardNo_Req.Contains(","))
                        {
                            cardNo = cbsCms.cardNo.substring(Convert.ToInt32(Properties.Settings.Default.CBS_CardNo_Req.Split(',')[0]), Convert.ToInt32(Properties.Settings.Default.CBS_CardNo_Req.Split(',')[1]));
                        }

                        //api_request_log arl = new api_request_log();
                        //arl.api_owner = "cbs";
                        //arl.api_name = "pushCBSData";
                        //arl.is_success = false;
                        //arl.request = objPayload.ToString();
                        //arl.response = "CBS push response";
                        //Helpers.Utilities.SaveApiRequestLog(arl);

                        return apiResponse(new response { result = 0, obj = string.Format("Success receiving mid {0} and card no {1}", cbsCms.cif, cardNo) });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/getDCSSystemSetting")]
        [HttpPost]
        public IHttpActionResult GetDCSSystemSetting(requestPayload reqPayload)
        {
            try
            {

                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();
                        var obj = ent.dcs_system_setting;

                        return apiResponse(new response { result = 0, obj = obj });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/getCpsCardElements")]
        [HttpPost]
        public IHttpActionResult GetCpsCardElements(requestPayload reqPayload)
        {
            try
            {

                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();
                        var obj = ent.cps_card_elements;

                        return apiResponse(new response { result = 0, obj = obj });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/getRole")]
        [HttpPost]
        public IHttpActionResult GetRole(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();
                        var obj = ent.system_role.Where(o => o.is_deleted == false);

                        return apiResponse(new response { result = 0, obj = obj });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }

        }

        [Route("~/api/checkServerDBStatus")]
        [HttpPost]
        public IHttpActionResult CheckServerDBStatus()
        {
            try
            {
                //string payload = reqPayload.payload; 

                afpslai_emvEntities ent = new afpslai_emvEntities();
                var obj = ent.associate_type;

                return apiResponse(new response { result = 0, obj = obj.Count() });
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/getAssociateType")]
        [HttpPost]
        public IHttpActionResult GetAssociateType(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        var payloadEnt = Newtonsoft.Json.JsonConvert.DeserializeObject<associate_type>(payload);

                        afpslai_emvEntities ent = new afpslai_emvEntities();
                        var obj = ent.associate_type.Where(o => o.is_deleted == false);

                        return apiResponse(new response { result = 0, obj = obj });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/getBranch")]
        [HttpPost]
        public IHttpActionResult GetBranch(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());
                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();
                        var obj = ent.branches.Where(o => o.is_deleted == false);

                        return apiResponse(new response { result = 0, obj = obj });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/getCivilStatus")]
        [HttpPost]
        public IHttpActionResult GetCivilStatus(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();
                        var obj = ent.civil_status.Where(o => o.is_deleted == false);

                        return apiResponse(new response { result = 0, obj = obj });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/getCountry")]
        [HttpPost]
        public IHttpActionResult GetCountry(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();
                        var obj = ent.countries.ToList();

                        return apiResponse(new response { result = 0, obj = obj });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/getMembershipStatus")]
        [HttpPost]
        public IHttpActionResult GetMembershipStatus(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();
                        var obj = ent.membership_status.Where(o => o.is_deleted == false);

                        return apiResponse(new response { result = 0, obj = obj });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/getMembershipType")]
        [HttpPost]
        public IHttpActionResult GetMembershipType(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();
                        var obj = ent.membership_type.Where(o => o.is_deleted == false);

                        return apiResponse(new response { result = 0, obj = obj });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/getSystemUser")]
        [HttpPost]
        public IHttpActionResult GetSystemUser(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();
                        int userId = 0;

                        if (payload != "")
                        {
                            dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                            if (objPayload.userId == null) return apiResponse(new responseFailedBadRequest { message = "Missing required field(s)" });
                            else
                            {
                                userId = objPayload.userId;
                                //var obj = ent.system_user.Where(o => o.id == userId);

                                var users = from u in ent.system_user
                                            join r in ent.system_role on u.role_id equals r.id into table1
                                            from r in table1.ToList()
                                            where u.id == userId
                                            select new
                                            {
                                                id = u.id,
                                                user_name = u.user_name,
                                                first_name = u.first_name,
                                                middle_name = u.middle_name,
                                                last_name = u.last_name,
                                                suffix = u.suffix,
                                                role_id = u.role_id,
                                                role = r.role,
                                                status = u.status,
                                                fullName = u.first_name + " " + u.middle_name + " " + u.last_name + " " + u.suffix,
                                                is_deleted = u.is_deleted
                                            };

                                return apiResponse(new response { result = 0, obj = users });
                            }
                        }
                        else
                        {
                            var users = from u in ent.system_user
                                        join r in ent.system_role on u.role_id equals r.id into table1
                                        from r in table1.ToList()
                                        select new
                                        {
                                            userId = u.id,
                                            userName = u.user_name,
                                            firstName = u.first_name,
                                            middleName = u.middle_name,
                                            lastName = u.last_name,
                                            suffix = u.suffix,
                                            roleId = u.role_id,
                                            roleDesc = r.role,
                                            status = u.status,
                                            fullName = u.first_name + " " + u.middle_name + " " + u.last_name + " " + u.suffix,
                                            is_deleted = u.is_deleted
                                        };

                            return apiResponse(new response { result = 0, obj = users });
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/getPrintType")]
        [HttpPost]
        public IHttpActionResult GetPrintType(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();
                        var obj = ent.print_type.Where(o => o.is_deleted == false);

                        return apiResponse(new response { result = 0, obj = obj });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/getRecardReason")]
        [HttpPost]
        public IHttpActionResult GetRecardReason(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();
                        var obj = ent.recard_reason.Where(o => o.is_deleted == false);

                        return apiResponse(new response { result = 0, obj = obj });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/getCard")]
        [HttpPost]
        public IHttpActionResult GetCard(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        string value = objPayload.value;

                        string cif = "";
                        int cardId = 0;
                        int memberId = 0;
                        string cardNo = "";

                        if (objPayload.cif != null) cif = objPayload.cif;
                        if (objPayload.cardId != null) cardId = objPayload.cardId;
                        if (objPayload.memberId != null) memberId = objPayload.memberId;
                        if (objPayload.cardNo != null) cardNo = objPayload.cardNo;

                        if (string.IsNullOrEmpty(cif) && string.IsNullOrEmpty(cardNo) && cardId == 0 && memberId == 0) return apiResponse(new responseFailedBadRequest { message = "Empty cif or card no. or card id or member id" });
                        else
                        {
                            var memberCard = ent.cards
                                          .Join(
                                              ent.members,
                                              c => c.id,
                                              m => m.id,
                                              (c, m) => new
                                              {
                                                  cardId = c.id,
                                                  memberId = m.id,
                                                  cif = m.cif,
                                                  cardNo = c.cardNo,
                                                  cDatePost = c.date_post,
                                                  mIsCancel = m.is_cancel,
                                                  cIsCancel = c.is_cancel
                                              }
                                          )
                                          .Where(o => o.mIsCancel == false && o.cIsCancel == false && (o.cif.Equals(cif) || o.cardNo.Equals(cardNo) || o.cardId == cardId || o.memberId == memberId))
                                          .LastOrDefault();

                            return apiResponse(new response { result = 0, obj = memberCard });
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/getCardForPrint")]
        [HttpPost]
        public IHttpActionResult GetCardForPrint(requestPayload reqPayload)
        //public IHttpActionResult GetCardForPrint(string payload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);

                        string cif = "";
                        int cardId = 0;
                        int memberId = 0;
                        string cardNo = "";

                        if (objPayload.cif != null) cif = objPayload.cif;
                        if (objPayload.cardId != null) cardId = objPayload.cardId;
                        if (objPayload.memberId != null) memberId = objPayload.memberId;
                        if (objPayload.cardNo != null) cardNo = objPayload.cardNo;

                        //where m.is_cancel == false && c.is_cancel == false && (m.cif.Equals(cif) || c.cardNo.Equals(cardNo) || c.id == cardId || m.id == memberId)
                        //where m.is_cancel == false && (m.cif.Equals(cif) || c.cardNo.Equals(cardNo) || c.id == cardId || m.id == memberId)

                        if (string.IsNullOrEmpty(cif) && string.IsNullOrEmpty(cardNo) && cardId == 0 && memberId == 0) return apiResponse(new responseFailedBadRequest { message = "Empty cif or card no. or card id or member id" });
                        else
                        {
                            var memberCards = (from m in ent.members
                                               join c in ent.cards on m.id equals c.member_id into table1
                                               from c in table1.DefaultIfEmpty()
                                               join b in ent.branches on m.branch_id equals b.id into table2
                                               from b in table2.DefaultIfEmpty()
                                               where m.is_cancel == false && (m.cif.Equals(cif) || c.cardNo.Equals(cardNo) || c.id == cardId || m.id == memberId)
                                               select new
                                               {
                                                   cardId = c == null ? 0 : c.id,
                                                   memberId = m.id,
                                                   cif = m.cif,
                                                   cardNo = c == null ? string.Empty : c.cardNo,
                                                   cardName = m.card_name,
                                                   first_name = m.first_name,
                                                   middle_name = m.middle_name,
                                                   last_name = m.last_name,
                                                   suffix = m.suffix,
                                                   gender = m.gender,
                                                   membership_date = m.membership_date,
                                                   branchName = b == null ? string.Empty : b.branchName,
                                                   mDatePost = m.date_post,
                                                   cDatePost = c == null ? string.Empty : c.date_post.ToString(),
                                                   cTimePost = c == null ? string.Empty : c.time_post.ToString(),
                                                   mIsCancel = m.is_cancel,
                                                   cIsCancel = c == null ? false : c.is_cancel
                                               });

                            cardForPrint cfp = new cardForPrint();

                            if (memberCards.Count() > 0)
                            {
                                var memberCard = memberCards.ToList().Where(o => o.cIsCancel.Equals(false)).LastOrDefault();

                                string photoRepo = string.Format(@"{0}\{1}", Properties.Settings.Default.PhotoRepo, Convert.ToDateTime(memberCard.mDatePost).ToString(dateFormat));

                                string photoFile = string.Format(@"{0}\{1}.jpg", photoRepo, memberCard.cif);
                                if (System.IO.File.Exists(photoFile))
                                {
                                    var base64Photo = System.Convert.ToBase64String(System.IO.File.ReadAllBytes(photoFile));

                                    cfp.cardId = memberCard.cardId;
                                    cfp.memberId = memberCard.memberId;
                                    cfp.cif = memberCard.cif;
                                    cfp.cardNo = memberCard.cardNo;
                                    cfp.cardName = memberCard.cardName;
                                    cfp.first_name = memberCard.first_name;
                                    cfp.middle_name = memberCard.middle_name;
                                    cfp.last_name = memberCard.last_name;
                                    cfp.suffix = memberCard.suffix;
                                    cfp.gender = memberCard.gender;
                                    cfp.dateCaptured = Convert.ToDateTime(memberCard.mDatePost).ToString("MM/dd/yyyy");
                                    cfp.membership_date = Convert.ToDateTime(memberCard.membership_date).ToString("MM/dd/yyyy");
                                    cfp.branch_issued = memberCard.branchName;
                                    if (!string.IsNullOrEmpty(memberCard.cDatePost)) cfp.datePrinted = Convert.ToDateTime(memberCard.cDatePost).ToString("MM/dd/yyyy") + " " + memberCard.cTimePost;
                                    cfp.base64Photo = base64Photo;
                                }
                                else return apiResponse(new response { result = 1, obj = "Photo not found" });
                            }

                            return apiResponse(new response { result = 0, obj = cfp });
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/getAddress")]
        [HttpPost]
        public IHttpActionResult GetAddress(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        string value = objPayload.value;

                        var memberAddress = from m in ent.members
                                            join a in ent.addresses on m.id equals a.member_id into table1
                                            from a in table1.ToList()
                                            join c in ent.countries on a.country_id equals c.id into table2
                                            from c in table2.ToList()
                                            where m.cif == value && m.is_cancel == false && a.is_cancel == false
                                            select new
                                            {
                                                cif = m.cif,
                                                address1 = a.address1,
                                                address2 = a.address2,
                                                address3 = a.address3,
                                                city = a.city,
                                                province = a.province,
                                                zipcode = a.zipcode,
                                                countryId = c.id,
                                                country = c.countryName,
                                                countryCode = c.code
                                            };

                        return apiResponse(new response { result = 0, obj = memberAddress });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        #endregion

        #region " Add/ Update "

        [Route("~/api/validateLogIn")]
        [HttpPost]
        public IHttpActionResult ValidateLogIn(requestPayload reqPayload)
        {
            try
            {
                //Helpers.Utilities.SavePayload(reqPayload);

                afpslai_emvEntities ent = new afpslai_emvEntities();
                dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(reqPayload.payload);
                var user = Newtonsoft.Json.JsonConvert.DeserializeObject<loginRequest>(objPayload.ToString());

                if (user == null) return apiResponse(new responseFailedBadRequest { message = "User is empty" });
                else if (string.IsNullOrEmpty(user.user_name) || string.IsNullOrEmpty(user.user_pass)) return apiResponse(new responseFailedBadRequest { message = "Invalid credential" });
                else
                {
                    try
                    {
                        string userName = user.user_name;
                        var objUsername = ent.system_user.Where(o => o.user_name.Equals(userName)).FirstOrDefault();
                        if (objUsername == null) return apiResponse(new responseFailedBadRequest { message = "User does not exist" });
                        else if (objUsername.status == "Not active") return apiResponse(new responseFailedBadRequest { message = "User does not exist" });
                        else if (objUsername.status == "Hold") return apiResponse(new responseFailedBadRequest { message = "User account status is on hold" });
                        else
                        {
                            var dss = ent.dcs_system_setting;
                            bool isChangePassword = (dss.FirstOrDefault().system_default_password == user.user_pass);

                            if (user.user_pass == accAfpslaiEmvEncDec.Aes256CbcEncrypter.Decrypt(objUsername.user_pass))
                            {
                                var systemUser = ent.system_user
                                  .Join(
                                      ent.system_role,
                                      u => u.role_id,
                                      r => r.id,
                                      (u, r) => new
                                      {
                                          userId = u.id,
                                          roleId = r.id,
                                          roleDesc = r.role,
                                          userName = u.user_name,
                                          userPass = u.user_pass,
                                          fullName = u.first_name + " " + u.middle_name + " " + u.last_name + " " + u.suffix,
                                          isChangePassword = isChangePassword
                                      }
                                  )
                                  .Where(o => o.userName.Equals(userName))
                                  .ToList();

                                Helpers.Utilities.SaveSystemLog(reqPayload.system, systemUser[0].userId, string.Format("{0} log in ", userName));

                                return apiResponse(new response { result = 0, obj = systemUser });
                            }
                            else
                            {
                                return apiResponse(new responseFailedBadRequest { message = "Invalid credential" });
                            }


                        }
                    }
                    catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                    {
                        //Exception raise = dbEx;
                        foreach (var validationErrors in dbEx.EntityValidationErrors)
                        {
                            System.Text.StringBuilder sb = new System.Text.StringBuilder();

                            foreach (var validationError in validationErrors.ValidationErrors)
                            {
                                string message = string.Format("{0}:{1}",
                                    validationErrors.Entry.Entity.ToString(),
                                    validationError.ErrorMessage);

                                sb.Append(message + ". ");
                            }
                        }
                        //throw raise;
                    }

                    return apiResponse(new responseSuccessNewRecord());
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError
                {
                    obj = ex.Message
                });
            }
        }

        [Route("~/api/addSystemUser")]
        [HttpPost]
        public IHttpActionResult AddSystemUser(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var user = Newtonsoft.Json.JsonConvert.DeserializeObject<system_user>(objPayload.ToString()); ;

                        if (string.IsNullOrEmpty(user.user_name) || string.IsNullOrEmpty(user.first_name) || string.IsNullOrEmpty(user.last_name)) return apiResponse(new responseFailedBadRequest { message = "Missing required field(s)" });
                        else if (user.role_id == null || user.role_id == 0) return apiResponse(new responseFailedBadRequest { message = "Invalid system role" });
                        else
                        {
                            int userId = user.id;
                            string user_name = user.user_name;
                            string first_name = user.first_name;
                            string middle_name = user.middle_name;
                            string last_name = user.last_name;
                            string suffix = user.suffix;

                            if (user.id == 0)
                            {
                                var dss = ent.dcs_system_setting;
                                var objUsername = ent.system_user.Where(o => o.user_name.Equals(user_name));
                                var objFirstAndLast = ent.system_user.Where(o => o.first_name.Equals(first_name) && o.middle_name.Equals(middle_name) && o.last_name.Equals(last_name) && o.suffix.Equals(suffix));
                                if (objUsername.Count() > 0) return apiResponse(new responseFailedDuplicateRecord());
                                else if (objFirstAndLast.Count() > 0) return apiResponse(new responseFailedDuplicateRecord());
                                else
                                {
                                    if (string.IsNullOrEmpty(user.middle_name)) user.middle_name = "";
                                    if (string.IsNullOrEmpty(user.suffix)) user.suffix = "";
                                    user.status = "Active";
                                    user.user_pass = accAfpslaiEmvEncDec.Aes256CbcEncrypter.Encrypt(dss.FirstOrDefault().system_default_password);
                                    user.date_post = DateTime.Now.Date;
                                    user.time_post = DateTime.Now.TimeOfDay;
                                    user.is_deleted = false;
                                    ent.system_user.Add(user);
                                    ent.SaveChanges();

                                    Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("{0} user is added", user_name));

                                    return apiResponse(new responseSuccessNewRecord());
                                }
                            }
                            else
                            {
                                var objUsername = ent.system_user.Where(o => o.user_name.Equals(user_name) && o.id != userId).FirstOrDefault();
                                var objFirstAndLast = ent.system_user.Where(o => o.first_name.Equals(first_name) && o.middle_name.Equals(middle_name) && o.last_name.Equals(last_name) && o.suffix.Equals(suffix) && o.id != userId).FirstOrDefault();
                                if (objUsername != null) return apiResponse(new responseFailedDuplicateRecord());
                                else if (objFirstAndLast != null) return apiResponse(new responseFailedDuplicateRecord());
                                else
                                {
                                    var obj = ent.system_user.Where(o => o.id == userId).FirstOrDefault();
                                    if (obj != null)
                                    {
                                        System.Text.StringBuilder sb = new System.Text.StringBuilder();
                                        sb.Append("");
                                        if (obj.user_name != user.user_name) sb.Append(". Username changed");
                                        if (obj.first_name != user.first_name) sb.Append(". Firstname changed");
                                        if (obj.last_name != user.last_name) sb.Append(". Lastname changed");
                                        if (obj.status != user.status) sb.Append(". Status changed");
                                        if (string.IsNullOrEmpty(user.middle_name)) if (obj.middle_name != user.middle_name) sb.Append(". Middlename changed");
                                        if (string.IsNullOrEmpty(user.suffix)) if (obj.suffix != user.suffix) sb.Append(". Suffix changed");
                                        if (sb.ToString() != "") sb.Append(".");

                                        obj.user_name = user.user_name;
                                        obj.first_name = user.first_name;
                                        obj.last_name = user.last_name;
                                        if (string.IsNullOrEmpty(user.middle_name)) obj.middle_name = "";
                                        if (string.IsNullOrEmpty(user.suffix)) obj.suffix = "";
                                        obj.status = user.status;
                                        obj.role_id = user.role_id;
                                        ent.SaveChanges();

                                        Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("{0} details were modified{1}", user.user_name, sb.ToString()));

                                        return apiResponse(new responseSuccessUpdateRecord());
                                    }
                                    else return apiResponse(new responseFailedUpdateRecord { message = "No record changed" });
                                }
                            }
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/resetUserPassword")]
        [HttpPost]
        public IHttpActionResult ResetUserPassword(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var user = Newtonsoft.Json.JsonConvert.DeserializeObject<system_user>(objPayload.ToString());

                        int userId = user.id;

                        //if (objPayload.userId != null) userId = objPayload.userId;

                        if (userId == 0) return apiResponse(new responseFailedBadRequest { message = "Missing required field" });
                        else
                        {
                            var dss = ent.dcs_system_setting;

                            var obj = ent.system_user.Where(o => o.id == userId).FirstOrDefault();
                            if (obj == null) return apiResponse(new responseFailedBadRequest { message = "User not found" });
                            else
                            {

                                obj.user_pass = accAfpslaiEmvEncDec.Aes256CbcEncrypter.Encrypt(dss.FirstOrDefault().system_default_password);
                                ent.SaveChanges();

                                Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("{0} password has been reset", obj.user_name));

                                return apiResponse(new responseSuccess { message = "User password has been reset" });

                            }

                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/changeUserPassword")]
        [HttpPost]
        public IHttpActionResult ChangeUserPassword(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);

                        int userId = 0;
                        string userOldPass = "";
                        string userNewPass = "";

                        if (objPayload.userId != null) userId = objPayload.userId;
                        if (objPayload.userOldPass != null) userOldPass = objPayload.userOldPass;
                        if (objPayload.userNewPass != null) userNewPass = objPayload.userNewPass;

                        if (userId == 0 || userOldPass == "" || userNewPass == "") return apiResponse(new responseFailedBadRequest { message = "Missing required field(s)" });
                        else
                        {
                            //var dss = ent.dcs_system_setting;

                            var obj = ent.system_user.Where(o => o.id == userId).FirstOrDefault();
                            if (obj == null) return apiResponse(new responseFailedBadRequest { message = "User not found" });
                            else
                            {
                                if (accAfpslaiEmvEncDec.Aes256CbcEncrypter.Decrypt(obj.user_pass) == userOldPass)
                                {
                                    obj.user_pass = accAfpslaiEmvEncDec.Aes256CbcEncrypter.Encrypt(userNewPass);
                                    ent.SaveChanges();

                                    Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("{0} password has been changed", obj.user_name));

                                    return apiResponse(new responseSuccess { message = "User password has been changed" });
                                }
                                else return apiResponse(new response { result = 1, message = "Invalid old password" });
                            }
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        //[Route("~/api/addOnlineRegistration")]
        //[HttpPost]
        //public IHttpActionResult AddOnlineRegistration(requestPayload reqPayload)
        //{
        //    string payload = reqPayload.payload;

        //    var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

        //    switch (validationResponse)
        //    {
        //        case (int)System.Net.HttpStatusCode.Unauthorized:
        //            return apiResponse(new responseFailedUnauthorized());
        //        case (int)System.Net.HttpStatusCode.BadRequest:
        //            return apiResponse(new responseFailedBadRequest());

        //        case (int)System.Net.HttpStatusCode.InternalServerError:
        //            return apiResponse(new responseFailedSystemError());
        //        default:
        //            afpslai_emvEntities ent = new afpslai_emvEntities();

        //            dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
        //            var online_registration = Newtonsoft.Json.JsonConvert.DeserializeObject<online_registration>(objPayload.value);

        //            if (string.IsNullOrEmpty(online_registration.first_name) || string.IsNullOrEmpty(online_registration.last_name) || string.IsNullOrEmpty(online_registration.reference_number)) return apiResponse(new responseFailedBadRequest());

        //            else
        //            {
        //                online_registration.date_post = DateTime.Now.Date;
        //                online_registration.time_post = DateTime.Now.TimeOfDay;
        //                ent.online_registration.Add(online_registration);
        //                ent.SaveChanges();

        //                return apiResponse(new responseSuccessNewRecord());
        //            }
        //    }           
        //}

        [Route("~/api/addOnlineRegistration")]
        [HttpPost]
        public IHttpActionResult AddOnlineRegistration(online_registration online_registration)
        {
            try
            {
                afpslai_emvEntities ent = new afpslai_emvEntities();

                if (string.IsNullOrEmpty(online_registration.first_name) || string.IsNullOrEmpty(online_registration.last_name) || string.IsNullOrEmpty(online_registration.reference_number)) return apiResponse(new responseFailedBadRequest { message = "Missing required field(s)" });

                else
                {
                    online_registration.date_post = DateTime.Now.Date;
                    online_registration.time_post = DateTime.Now.TimeOfDay;
                    ent.online_registration.Add(online_registration);
                    ent.SaveChanges();

                    return apiResponse(new responseSuccessNewRecord());
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/addSystemLog")]
        [HttpPost]
        public IHttpActionResult AddSystemLog(system_log system_log)
        {
            try
            {
                afpslai_emvEntities ent = new afpslai_emvEntities();

                //if (string.IsNullOrEmpty(online_registration.first_name) || string.IsNullOrEmpty(online_registration.last_name) || string.IsNullOrEmpty(online_registration.reference_number)) return apiResponse(new responseFailedBadRequest());

                //else
                //{
                //system_log.date_post = DateTime.Now.Date;
                //system_log.time_post = DateTime.Now.TimeOfDay;
                //ent.system_log.Add(system_log);
                //ent.SaveChanges();

                Helpers.Utilities.AddSysLog(system_log);

                return apiResponse(new responseSuccessNewRecord());
                //}
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/spx100")]
        [HttpPost]
        public IHttpActionResult EncryptValue(searchParam data)
        {
            try
            {
                if (string.IsNullOrEmpty(data.value)) return apiResponse(new responseFailedBadRequest { message = "No data to encode" });
                else
                {
                    var obj = accAfpslaiEmvEncDec.Aes256CbcEncrypter.Encrypt(data.value);

                    return apiResponse(new response { obj = obj.ToString() });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/spx101")]
        [HttpPost]
        public IHttpActionResult DecryptValue(searchParam data)
        {
            try
            {
                if (string.IsNullOrEmpty(data.value)) return apiResponse(new responseFailedBadRequest { message = "No data to decode" });
                else
                {
                    var obj = accAfpslaiEmvEncDec.Aes256CbcEncrypter.Decrypt(data.value);

                    return apiResponse(new response { obj = obj.ToString() });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/saveMemberImages")]
        [HttpPost]
        public IHttpActionResult SaveMemberImages(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var images = Newtonsoft.Json.JsonConvert.DeserializeObject<memberImages>(objPayload.ToString());

                        if (string.IsNullOrEmpty(images.cif) || string.IsNullOrEmpty(images.dateCaptured) || string.IsNullOrEmpty(images.base64Photo)) return apiResponse(new responseFailedBadRequest { message = "Missing required field(s)" });
                        else
                        {
                            string photoRepo = string.Format(@"{0}\{1}", Properties.Settings.Default.PhotoRepo, Convert.ToDateTime(images.dateCaptured).ToString(dateFormat));
                            string memberRepo = string.Format(@"{0}\{1}", Properties.Settings.Default.MemberDataRepo, Convert.ToDateTime(images.dateCaptured).ToString(dateFormat));

                            if (!System.IO.Directory.Exists(photoRepo)) System.IO.Directory.CreateDirectory(photoRepo);
                            if (!System.IO.Directory.Exists(memberRepo)) System.IO.Directory.CreateDirectory(memberRepo);

                            var bytePhoto = System.Convert.FromBase64String(images.base64Photo);
                            //return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
                            string photoFile = string.Format(@"{0}\{1}.jpg", photoRepo, images.cif);
                            if (System.IO.File.Exists(photoFile)) System.IO.File.Delete(photoFile);
                            System.IO.File.WriteAllBytes(photoFile, bytePhoto);
                            if (images.base64ZipFile != null)
                            {
                                string zipFile = string.Format(@"{0}\{1}.zip", memberRepo, images.cif);
                                if (System.IO.File.Exists(zipFile)) System.IO.File.Delete(zipFile);
                                var byteZipFile = System.Convert.FromBase64String(images.base64ZipFile);
                                System.IO.File.WriteAllBytes(zipFile, byteZipFile);
                            }

                            return apiResponse(new response());
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/addMember")]
        [HttpPost]
        public IHttpActionResult AddMember(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var member = Newtonsoft.Json.JsonConvert.DeserializeObject<member>(objPayload.ToString());

                        if (string.IsNullOrEmpty(member.first_name) || string.IsNullOrEmpty(member.last_name) || string.IsNullOrEmpty(member.cif)) return apiResponse(new responseFailedBadRequest { message = "Missing required field(s)" });
                        //if (address.member_id == null) return apiResponse(new responseFailedBadRequest());
                        else
                        {
                            member.date_post = DateTime.Now.Date;
                            member.time_post = DateTime.Now.TimeOfDay;
                            member.is_cancel = false;
                            ent.members.Add(member);
                            ent.SaveChanges();

                            string reference_number = member.online_reference_number;
                            var objOnlineReg = ent.online_registration.Where(o => o.reference_number.Equals(reference_number)).FirstOrDefault();
                            if (objOnlineReg != null)
                            {
                                objOnlineReg.date_captured = DateTime.Now.Date;
                                objOnlineReg.reference_id = member.id;
                                ent.SaveChanges();
                            }

                            return apiResponse(new responseSuccessNewRecord { obj = member.id });
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/checkMemberIfCaptured")]
        [HttpPost]
        public IHttpActionResult CheckMemberIfCaptured(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var member = Newtonsoft.Json.JsonConvert.DeserializeObject<member>(objPayload.ToString());

                        if (string.IsNullOrEmpty(member.cif) || string.IsNullOrEmpty(member.first_name) || string.IsNullOrEmpty(member.last_name)) return apiResponse(new responseFailedBadRequest { message = "Missing required field(s)" });
                        //if (address.member_id == null) return apiResponse(new responseFailedBadRequest());
                        else
                        {
                            //new
                            if (member.print_type_id == 1)
                            {
                                string cif = member.cif;
                                string first_name = member.first_name;
                                string middle_name = member.middle_name;
                                string last_name = member.last_name;
                                string suffix = member.suffix;
                                //int printTypeId = member.print_;
                                var objCheckCif = ent.members.Where(o => o.cif.Equals(cif));
                                var objCheckName = ent.members.Where(o => o.first_name.Equals(first_name) && o.middle_name.Equals(middle_name) && o.last_name.Equals(last_name) && o.suffix.Equals(suffix));

                                if (objCheckCif.Count() > 0) return apiResponse(new response { result = 1, message = "Duplicate CIF is not allowed" });
                                else if (objCheckName.Count() > 0) return apiResponse(new response { result = 1, message = "Duplicate Name is not allowed" });
                                else return apiResponse(new responseSuccess());
                            }
                            else return apiResponse(new responseSuccess());
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        //[Route("~/api/checkIfChangePasswordRequired")]
        //[HttpPost]
        //public IHttpActionResult CheckIfChangePasswordRequired(requestPayload reqPayload)
        //{
        //    try
        //    {
        //        string payload = reqPayload.payload;

        //        var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

        //        switch (validationResponse)
        //        {
        //            case (int)System.Net.HttpStatusCode.Unauthorized:
        //                return apiResponse(new responseFailedUnauthorized());
        //            case (int)System.Net.HttpStatusCode.BadRequest:
        //                return apiResponse(new responseFailedBadRequest());

        //            case (int)System.Net.HttpStatusCode.InternalServerError:
        //                return apiResponse(new responseFailedSystemError());
        //            default:
        //                afpslai_emvEntities ent = new afpslai_emvEntities();                        

        //                dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);

        //                string cif = "";
        //                int cardId = 0;
        //                int memberId = 0;
        //                string cardNo = "";

        //                if (objPayload.cif != null) cif = objPayload.cif;
        //                if (objPayload.cardId != null) cardId = objPayload.cardId;

        //                dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
        //                var member = Newtonsoft.Json.JsonConvert.DeserializeObject<member>(objPayload.ToString());

        //                if (string.IsNullOrEmpty(member.cif) || string.IsNullOrEmpty(member.first_name) || string.IsNullOrEmpty(member.last_name)) return apiResponse(new responseFailedBadRequest { message = "Missing required field(s)" });
        //                //if (address.member_id == null) return apiResponse(new responseFailedBadRequest());
        //                else
        //                {
        //                    //new
        //                    if (member.print_type_id == 1)
        //                    {
        //                        string cif = member.cif;
        //                        string first_name = member.first_name;
        //                        string middle_name = member.middle_name;
        //                        string last_name = member.last_name;
        //                        string suffix = member.suffix;
        //                        //int printTypeId = member.print_;
        //                        var objCheckCif = ent.members.Where(o => o.cif.Equals(cif));
        //                        var objCheckName = ent.members.Where(o => o.first_name.Equals(first_name) && o.middle_name.Equals(middle_name) && o.last_name.Equals(last_name) && o.suffix.Equals(suffix));

        //                        if (objCheckCif.Count() > 0) return apiResponse(new response { result = 1, message = "Duplicate CIF is not allowed" });
        //                        else if (objCheckName.Count() > 0) return apiResponse(new response { result = 1, message = "Duplicate Name is not allowed" });
        //                        else return apiResponse(new responseSuccess());
        //                    }
        //                    else return apiResponse(new responseSuccess());
        //                }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Error(ex.Message);
        //        return apiResponse(new responseFailedSystemError { obj = ex.Message });
        //    }
        //}

        [Route("~/api/addCard")]
        [HttpPost]
        public IHttpActionResult AddCard(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var card = Newtonsoft.Json.JsonConvert.DeserializeObject<card>(objPayload.ToString());

                        if (string.IsNullOrEmpty(card.cardNo)) return apiResponse(new responseFailedBadRequest { message = "Missing required field(s)" });
                        else if (card.member_id == null || card.member_id == 0) return apiResponse(new responseFailedBadRequest { message = "Missing reference member id" });
                        else
                        {
                            card.date_post = DateTime.Now.Date;
                            card.time_post = DateTime.Now.TimeOfDay;
                            card.is_cancel = false;
                            ent.cards.Add(card);
                            ent.SaveChanges();

                            return apiResponse(new responseSuccessNewRecord { obj = card.id });
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/cancelCapture")]
        [HttpPost]
        public IHttpActionResult CancelCapture(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var cancelCapture = Newtonsoft.Json.JsonConvert.DeserializeObject<cancelCapture>(objPayload.ToString()); ;

                        if (cancelCapture.memberId == null || cancelCapture.memberId == 0) { }
                        else
                        {
                            int memberId = cancelCapture.memberId;
                            var obj = ent.members.Where(o => o.id == memberId).FirstOrDefault();
                            if (obj != null)
                            {
                                obj.is_cancel = true;
                                ent.SaveChanges();
                            }

                            var objOnlineReg = ent.online_registration.Where(o => o.reference_id == memberId).FirstOrDefault();
                            if (obj != null)
                            {
                                objOnlineReg.date_captured = null;
                                objOnlineReg.reference_id = null;
                                ent.SaveChanges();
                            }
                        }

                        if (cancelCapture.addressId == null || cancelCapture.addressId == 0) { }
                        else
                        {
                            int addressId = cancelCapture.addressId;
                            var obj = ent.addresses.Where(o => o.id == addressId).FirstOrDefault();
                            if (obj != null)
                            {
                                obj.is_cancel = true;
                                ent.SaveChanges();
                            }
                        }

                        if (cancelCapture.cardId == null || cancelCapture.cardId == 0) { }
                        else
                        {
                            int cardId = cancelCapture.cardId;
                            var obj = ent.cards.Where(o => o.id == cardId).FirstOrDefault();
                            if (obj != null)
                            {
                                obj.is_cancel = true;
                                ent.SaveChanges();
                            }
                        }

                        return apiResponse(new response { result = 0 });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/addAddress")]
        [HttpPost]
        public IHttpActionResult AddAddress(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var address = Newtonsoft.Json.JsonConvert.DeserializeObject<address>(objPayload.ToString());

                        //if (string.IsNullOrEmpty(address.cardNo)) return apiResponse(new responseFailedBadRequest());
                        if (address.member_id == null) return apiResponse(new responseFailedBadRequest { message = "Missing reference member id" });
                        else
                        {
                            address.date_post = DateTime.Now.Date;
                            address.time_post = DateTime.Now.TimeOfDay;
                            address.is_cancel = false;
                            ent.addresses.Add(address);
                            ent.SaveChanges();

                            return apiResponse(new responseSuccessNewRecord { obj = address.id });
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/addSystemRole")]
        [HttpPost]
        public IHttpActionResult AddSystemRole(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var role = Newtonsoft.Json.JsonConvert.DeserializeObject<system_role>(objPayload.ToString()); ;

                        if (string.IsNullOrEmpty(role.role)) return apiResponse(new responseFailedBadRequest { message = "Missing required field(s)" });
                        else
                        {
                            int roleId = role.id;
                            string roleDesc = role.role;

                            if (role.id == 0)
                            {
                                var obj = ent.system_role.Where(o => o.role.Equals(roleDesc));
                                if (obj.Count() > 0) return apiResponse(new responseFailedDuplicateRecord());
                                else
                                {
                                    //role.is_deleted = false;
                                    ent.system_role.Add(role);
                                    ent.SaveChanges();

                                    Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("{0} role is added", roleDesc));

                                    return apiResponse(new responseSuccessNewRecord());
                                }
                            }
                            else
                            {
                                var obj = ent.system_role.Where(o => o.id == roleId).FirstOrDefault();
                                if (obj != null)
                                {
                                    obj.role = role.role;
                                    ent.SaveChanges();

                                    Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("role id {0} is modified", roleId));

                                    return apiResponse(new responseSuccessUpdateRecord());
                                }
                                else return apiResponse(new responseFailedUpdateRecord { message = "No record changed" });
                            }
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/delSystemRole")]
        [HttpPost]
        public IHttpActionResult DeleteSystemRole(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var role = Newtonsoft.Json.JsonConvert.DeserializeObject<system_role>(objPayload.ToString()); ;

                        if (role.id == 0) return apiResponse(new responseFailedBadRequest { message = "Missing required field(s)" });
                        else
                        {
                            int roleId = role.id;
                            var obj = ent.system_role.Where(o => o.id == roleId).FirstOrDefault();
                            if (obj != null)
                            {
                                obj.is_deleted = true;
                                ent.SaveChanges();

                                return apiResponse(new responseSuccessDeleteRecord());
                            }
                            else return apiResponse(new responseFailedUpdateRecord { message = "No record changed" });
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/delSystemUser")]
        [HttpPost]
        public IHttpActionResult DeleteSystemUser(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var user = Newtonsoft.Json.JsonConvert.DeserializeObject<system_user>(objPayload.ToString());

                        if (user.id == 0) return apiResponse(new responseFailedBadRequest { message = "Missing required field(s)" });
                        else
                        {
                            int userId = user.id;
                            var obj = ent.system_user.Where(o => o.id == userId).FirstOrDefault();
                            if (obj != null)
                            {
                                obj.is_deleted = true;
                                ent.SaveChanges();

                                return apiResponse(new responseSuccessDeleteRecord());
                            }
                            else return apiResponse(new responseFailedUpdateRecord { message = "No record changed" });
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }


        [Route("~/api/addAssociateType")]
        [HttpPost]
        public IHttpActionResult AddAssociateType(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var assocType = Newtonsoft.Json.JsonConvert.DeserializeObject<associate_type>(objPayload.ToString());

                        if (string.IsNullOrEmpty(assocType.associateType)) return apiResponse(new responseFailedBadRequest { message = "Missing required field(s)" });
                        else
                        {
                            if (assocType.id == 0)
                            {
                                string associateType = assocType.associateType;
                                var obj = ent.associate_type.Where(o => o.associateType.Equals(associateType));
                                if (obj.Count() > 0) return apiResponse(new responseFailedDuplicateRecord());
                                else
                                {
                                    assocType.is_deleted = false;
                                    ent.associate_type.Add(assocType);
                                    ent.SaveChanges();

                                    Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("{0} associate type is added", assocType));

                                    return apiResponse(new responseSuccessNewRecord());
                                }
                            }
                            else
                            {
                                int assocTypeId = assocType.id;
                                var obj = ent.associate_type.Where(o => o.id == assocTypeId).FirstOrDefault();
                                if (obj != null)
                                {
                                    obj.associateType = assocType.associateType;
                                    ent.SaveChanges();

                                    Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("associate type id {0} is modified", assocTypeId));

                                    return apiResponse(new responseSuccessUpdateRecord());
                                }
                                else return apiResponse(new responseFailedUpdateRecord { message = "No record changed" });
                            }
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/delAssociateType")]
        [HttpPost]
        public IHttpActionResult DeleteAssociateType(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var assocType = Newtonsoft.Json.JsonConvert.DeserializeObject<associate_type>(objPayload.ToString()); ;

                        if (assocType.id == 0) return apiResponse(new responseFailedBadRequest());
                        else
                        {
                            int assocTypeId = assocType.id;
                            var obj = ent.associate_type.Where(o => o.id == assocTypeId).FirstOrDefault();
                            if (obj != null)
                            {
                                obj.is_deleted = true;
                                ent.SaveChanges();

                                Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("{0} is changed to is_deleted=true", obj.associateType));

                                return apiResponse(new responseSuccessDeleteRecord());
                            }
                            else return apiResponse(new responseFailedUpdateRecord { message = "No record changed" });
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/addBranch")]
        [HttpPost]
        public IHttpActionResult AddBranch(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var branch = Newtonsoft.Json.JsonConvert.DeserializeObject<branch>(objPayload.ToString()); ;

                        if (string.IsNullOrEmpty(branch.branchName) || string.IsNullOrEmpty(branch.code)) return apiResponse(new responseFailedBadRequest());
                        else
                        {
                            if (branch.id == 0)
                            {
                                string branchName = branch.branchName;
                                var obj = ent.branches.Where(o => o.branchName.Equals(branchName));
                                if (obj.Count() > 0) return apiResponse(new responseFailedDuplicateRecord());
                                else
                                {
                                    branch.is_deleted = false;
                                    ent.branches.Add(branch);
                                    ent.SaveChanges();

                                    Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("{0} branch is added", branchName));

                                    return apiResponse(new responseSuccessNewRecord());
                                }
                            }
                            else
                            {
                                int branchId = branch.id;
                                var obj = ent.branches.Where(o => o.id == branchId).FirstOrDefault();
                                if (obj != null)
                                {
                                    obj.branchName = branch.branchName;
                                    ent.SaveChanges();

                                    Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("branch id {0} is modified", branchId));

                                    return apiResponse(new responseSuccessUpdateRecord());
                                }
                                else return apiResponse(new responseFailedUpdateRecord { message = "No record changed" });
                            }
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/delBranch")]
        [HttpPost]
        public IHttpActionResult DeleteBranch(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var branch = Newtonsoft.Json.JsonConvert.DeserializeObject<branch>(objPayload.ToString()); ;

                        if (branch.id == 0) return apiResponse(new responseFailedBadRequest());
                        else
                        {
                            int branchId = branch.id;
                            var obj = ent.branches.Where(o => o.id == branchId).FirstOrDefault();
                            if (obj != null)
                            {
                                obj.is_deleted = true;
                                ent.SaveChanges();

                                Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("{0} is changed to is_deleted=true", obj.branchName));

                                return apiResponse(new responseSuccessDeleteRecord());
                            }
                            else return apiResponse(new responseFailedUpdateRecord { message = "No record changed" });
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/addCivilStatus")]
        [HttpPost]
        public IHttpActionResult AddCivilStatus(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var civilStatus = Newtonsoft.Json.JsonConvert.DeserializeObject<civil_status>(objPayload.ToString()); ;

                        if (string.IsNullOrEmpty(civilStatus.civilStatus)) return apiResponse(new responseFailedBadRequest());
                        else
                        {
                            if (civilStatus.id == 0)
                            {
                                int civilStatusDesc = civilStatus.civilStatus;
                                var obj = ent.civil_status.Where(o => o.civilStatus.Equals(civilStatusDesc));
                                if (obj.Count() > 0) return apiResponse(new responseFailedDuplicateRecord());
                                else
                                {
                                    civilStatus.is_deleted = false;
                                    ent.civil_status.Add(civilStatus);
                                    ent.SaveChanges();

                                    Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("{0} civil status is added", civilStatusDesc));

                                    return apiResponse(new responseSuccessNewRecord());
                                }
                            }
                            else
                            {
                                int civilStatusId = civilStatus.id;
                                var obj = ent.civil_status.Where(o => o.id == civilStatusId).FirstOrDefault();
                                if (obj != null)
                                {
                                    obj.civilStatus = civilStatus.civilStatus;
                                    ent.SaveChanges();

                                    Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("civil status id {0} is modified", civilStatusId));

                                    return apiResponse(new responseSuccessUpdateRecord());
                                }
                                else return apiResponse(new responseFailedUpdateRecord { message = "No record changed" });
                            }
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/delCivilStatus")]
        [HttpPost]
        public IHttpActionResult DeleteCivilStatus(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var civilStatus = Newtonsoft.Json.JsonConvert.DeserializeObject<civil_status>(objPayload.ToString()); ;

                        if (civilStatus.id == 0) return apiResponse(new responseFailedBadRequest());
                        else
                        {
                            int civilStatusId = civilStatus.id;
                            var obj = ent.civil_status.Where(o => o.id == civilStatusId).FirstOrDefault();
                            if (obj != null)
                            {
                                obj.is_deleted = true;
                                ent.SaveChanges();

                                Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("{0} is changed to is_deleted=true", obj.civilStatus));

                                return apiResponse(new responseSuccessDeleteRecord());
                            }
                            else return apiResponse(new responseFailedUpdateRecord { message = "No record changed" });
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/addMembershipStatus")]
        [HttpPost]
        public IHttpActionResult AddMembershipStatus(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var membershipStatus = Newtonsoft.Json.JsonConvert.DeserializeObject<membership_status>(objPayload.ToString()); ;

                        if (string.IsNullOrEmpty(membershipStatus.membershipStatus)) return apiResponse(new responseFailedBadRequest());
                        else
                        {
                            if (membershipStatus.id == 0)
                            {
                                string membershipStatusDesc = membershipStatus.membershipStatus;
                                var obj = ent.membership_status.Where(o => o.membershipStatus.Equals(membershipStatusDesc));
                                if (obj.Count() > 0) return apiResponse(new responseFailedDuplicateRecord());
                                else
                                {
                                    membershipStatus.is_deleted = false;
                                    ent.membership_status.Add(membershipStatus);
                                    ent.SaveChanges();

                                    Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("{0} membership status is added", membershipStatusDesc));

                                    return apiResponse(new responseSuccessNewRecord());
                                }
                            }
                            else
                            {
                                int membershipStatusId = membershipStatus.id;
                                var obj = ent.membership_status.Where(o => o.id == membershipStatusId).FirstOrDefault();
                                if (obj != null)
                                {
                                    obj.membershipStatus = membershipStatus.membershipStatus;
                                    ent.SaveChanges();

                                    Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("membership status id {0} is modified", membershipStatusId));

                                    return apiResponse(new responseSuccessUpdateRecord());
                                }
                                else return apiResponse(new responseFailedUpdateRecord { message = "No record changed" });
                            }
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/addDCSSystemSettings")]
        [HttpPost]
        public IHttpActionResult AddDCSSystemSettings(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();
                        var obj = ent.dcs_system_setting.FirstOrDefault();

                        if (obj == null)
                        {
                            dcs_system_setting dss = new dcs_system_setting();
                            dss.cif_length = 13;
                            dss.member_type_assoc_allow_yrs = 21;
                            dss.member_type_reg_allow_yrs = 15;
                            dss.cardname_length = 26;

                            ent.dcs_system_setting.Add(dss);
                            ent.SaveChanges();

                            Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("Dcs systsem settings is added"));

                            return apiResponse(new responseSuccessNewRecord());
                        }
                        else
                        {
                            dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                            var dcs_system_setting = Newtonsoft.Json.JsonConvert.DeserializeObject<dcs_system_setting>(objPayload.ToString());
                            obj.cif_length = dcs_system_setting.cif_length;
                            obj.member_type_assoc_allow_yrs = dcs_system_setting.member_type_assoc_allow_yrs;
                            obj.member_type_reg_allow_yrs = dcs_system_setting.member_type_reg_allow_yrs;
                            obj.cardname_length = dcs_system_setting.cardname_length;
                            ent.SaveChanges();

                            Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("Dcs systsem settings is modified"));

                            return apiResponse(new responseSuccessUpdateRecord());
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/addCPSCardElements")]
        [HttpPost]
        public IHttpActionResult AddCPSCardElements(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var cce = Newtonsoft.Json.JsonConvert.DeserializeObject<cps_card_elements>(objPayload.ToString());

                        if (string.IsNullOrEmpty(cce.element) || cce.x == null || cce.y == null || cce.width == null || cce.height == null || cce.element_type == null) return apiResponse(new responseFailedBadRequest { message = "Missing required field(s)" });
                        {
                            string element = cce.element;
                            afpslai_emvEntities ent = new afpslai_emvEntities();
                            var obj = ent.cps_card_elements.Where(o => o.element.Equals(element)).FirstOrDefault();
                            if (obj == null)
                            {
                                cce.date_post = DateTime.Now.Date;
                                cce.time_post = DateTime.Now.TimeOfDay;
                                cce.last_updated = DateTime.Now;
                                ent.cps_card_elements.Add(cce);
                                ent.SaveChanges();

                                return apiResponse(new responseSuccessNewRecord());
                            }
                            else
                            {
                                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                                sb.Append("");
                                if (obj.x != cce.x) sb.Append(". x changed");
                                if (obj.y != cce.y) sb.Append(". y changed");
                                if (obj.height != cce.height) sb.Append(". height changed");
                                if (obj.width != cce.width) sb.Append(". width changed");
                                if (obj.font_name != cce.font_name) sb.Append(". font name changed");
                                if (obj.font_size != cce.font_size) sb.Append(". font size changed");
                                if (obj.font_style != cce.font_style) sb.Append(". font stype changed");
                                if (obj.element_type != cce.element_type) sb.Append(". element type changed");
                                if (sb.ToString() != "") sb.Append(".");

                                obj.x = cce.x;
                                obj.y = cce.y;
                                obj.width = cce.width;
                                obj.height = cce.height;
                                if (!string.IsNullOrEmpty(cce.font_name)) obj.font_name = cce.font_name;
                                if (cce.font_size!=null) obj.font_size = cce.font_size;
                                if (cce.font_style != null) obj.font_style = cce.font_style;
                                obj.element_type = cce.element_type;
                                obj.last_updated = DateTime.Now;
                                ent.SaveChanges();

                                Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("{0} details were modified{1}", cce.element, sb.ToString()));

                                return apiResponse(new responseSuccessUpdateRecord());
                            }
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/delMembershipStatus")]
        [HttpPost]
        public IHttpActionResult DeleteMembershipStatus(membership_status membershipStatus)
        {
            try
            {
                afpslai_emvEntities ent = new afpslai_emvEntities();

                if (membershipStatus.id == 0) return apiResponse(new responseFailedBadRequest());
                else
                {
                    var obj = ent.membership_status.Where(o => o.id == membershipStatus.id).FirstOrDefault();
                    if (obj != null)
                    {
                        obj.is_deleted = true;
                        ent.SaveChanges();

                        return apiResponse(new responseSuccessDeleteRecord());
                    }
                    else return apiResponse(new responseFailedUpdateRecord { message = "No record changed" });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/addMembershipType")]
        [HttpPost]
        public IHttpActionResult AddMembershipType(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var membershipType = Newtonsoft.Json.JsonConvert.DeserializeObject<membership_type>(objPayload.ToString()); ;

                        if (string.IsNullOrEmpty(membershipType.membershipType)) return apiResponse(new responseFailedBadRequest());
                        else
                        {
                            if (membershipType.id == 0)
                            {
                                string membershipTypeDesc = membershipType.membershipType;
                                var obj = ent.membership_type.Where(o => o.membershipType.Equals(membershipTypeDesc));
                                if (obj.Count() > 0) return apiResponse(new responseFailedDuplicateRecord());
                                else
                                {
                                    membershipType.is_deleted = false;
                                    ent.membership_type.Add(membershipType);
                                    ent.SaveChanges();

                                    Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("{0} membership type is added", membershipTypeDesc));

                                    return apiResponse(new responseSuccessNewRecord());
                                }
                            }
                            else
                            {
                                int membershipTypeId = membershipType.id;
                                var obj = ent.membership_type.Where(o => o.id == membershipTypeId).FirstOrDefault();
                                if (obj != null)
                                {
                                    obj.membershipType = membershipType.membershipType;
                                    ent.SaveChanges();

                                    Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("membership type id {0} is modified", membershipTypeId));

                                    return apiResponse(new responseSuccessUpdateRecord());
                                }
                                else return apiResponse(new responseFailedUpdateRecord { message = "No record changed" });
                            }
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/delMembershipType")]
        [HttpPost]
        public IHttpActionResult DeleteMembershipType(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var membershipType = Newtonsoft.Json.JsonConvert.DeserializeObject<membership_type>(objPayload.ToString()); ;

                        if (membershipType.id == 0) return apiResponse(new responseFailedBadRequest());
                        else
                        {
                            int membershipTypeId = membershipType.id;
                            var obj = ent.membership_type.Where(o => o.id == membershipTypeId).FirstOrDefault();
                            if (obj != null)
                            {
                                obj.is_deleted = true;
                                ent.SaveChanges();

                                return apiResponse(new responseSuccessDeleteRecord());
                            }
                            else return apiResponse(new responseFailedUpdateRecord { message = "No record changed" });
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/addPrintType")]
        [HttpPost]
        public IHttpActionResult AddPrintType(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var printType = Newtonsoft.Json.JsonConvert.DeserializeObject<print_type>(objPayload.ToString()); ;

                        if (string.IsNullOrEmpty(printType.printType)) return apiResponse(new responseFailedBadRequest { message = "Missing required field(s)" });
                        else
                        {
                            if (printType.id == 0)
                            {
                                string printTypeDesc = printType.printType;
                                var obj = ent.print_type.Where(o => o.printType.Equals(printTypeDesc));
                                if (obj.Count() > 0) return apiResponse(new responseFailedDuplicateRecord());
                                else
                                {
                                    printType.is_deleted = false;
                                    ent.print_type.Add(printType);
                                    ent.SaveChanges();

                                    Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("{0} print type is added", printTypeDesc));

                                    return apiResponse(new responseSuccessNewRecord());
                                }
                            }
                            else
                            {
                                int printTypeId = printType.id;
                                var obj = ent.print_type.Where(o => o.id == printTypeId).FirstOrDefault();
                                if (obj != null)
                                {
                                    obj.printType = printType.printType;
                                    ent.SaveChanges();

                                    Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("print type id {0} is modified", printTypeId));

                                    return apiResponse(new responseSuccessUpdateRecord());
                                }
                                else return apiResponse(new responseFailedUpdateRecord { message = "No record changed" });
                            }
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/delPrintType")]
        [HttpPost]
        public IHttpActionResult DeletePrintType(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var printType = Newtonsoft.Json.JsonConvert.DeserializeObject<print_type>(objPayload.ToString()); ;

                        if (printType.id == 0) return apiResponse(new responseFailedBadRequest());
                        else
                        {
                            int printTypeId = printType.id;
                            var obj = ent.print_type.Where(o => o.id == printTypeId).FirstOrDefault();
                            if (obj != null)
                            {
                                obj.is_deleted = true;
                                ent.SaveChanges();

                                return apiResponse(new responseSuccessDeleteRecord());
                            }
                            else return apiResponse(new responseFailedUpdateRecord { message = "No record changed" });
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/addRecardReason")]
        [HttpPost]
        public IHttpActionResult AddRecardReason(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var recardReason = Newtonsoft.Json.JsonConvert.DeserializeObject<recard_reason>(objPayload.ToString()); ;

                        if (string.IsNullOrEmpty(recardReason.recardReason)) return apiResponse(new responseFailedBadRequest());
                        else
                        {
                            if (recardReason.id == 0)
                            {
                                int recardReasonDesc = recardReason.recardReason;
                                var obj = ent.recard_reason.Where(o => o.recardReason.Equals(recardReasonDesc));
                                if (obj.Count() > 0) return apiResponse(new responseFailedDuplicateRecord());
                                else
                                {
                                    recardReason.is_deleted = false;
                                    ent.recard_reason.Add(recardReason);
                                    ent.SaveChanges();

                                    Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("{0} recard reason is added", recardReasonDesc));

                                    return apiResponse(new responseSuccessNewRecord());
                                }
                            }
                            else
                            {
                                int recardReasonId = recardReason.id;
                                var obj = ent.recard_reason.Where(o => o.id == recardReasonId).FirstOrDefault();
                                if (obj != null)
                                {
                                    obj.recardReason = recardReason.recardReason;
                                    ent.SaveChanges();

                                    Helpers.Utilities.SaveSystemLog(reqPayload.system, authUserId, string.Format("recard reason id {0} is modified", recardReasonId));

                                    return apiResponse(new responseSuccessUpdateRecord());
                                }
                                else return apiResponse(new responseFailedUpdateRecord { message = "No record changed" });
                            }
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        [Route("~/api/delRecardReason")]
        [HttpPost]
        public IHttpActionResult DeleteRecardReason(requestPayload reqPayload)
        {
            try
            {
                string payload = reqPayload.payload;

                var validationResponse = Helpers.Utilities.ValidateRequest(reqPayload, ref authUserId);

                switch (validationResponse)
                {
                    case (int)System.Net.HttpStatusCode.Unauthorized:
                        return apiResponse(new responseFailedUnauthorized());
                    case (int)System.Net.HttpStatusCode.BadRequest:
                        return apiResponse(new responseFailedBadRequest());

                    case (int)System.Net.HttpStatusCode.InternalServerError:
                        return apiResponse(new responseFailedSystemError());
                    default:
                        afpslai_emvEntities ent = new afpslai_emvEntities();

                        dynamic objPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                        var recardReason = Newtonsoft.Json.JsonConvert.DeserializeObject<recard_reason>(objPayload.ToString()); ;

                        if (recardReason.id == 0) return apiResponse(new responseFailedBadRequest());
                        else
                        {
                            int recardReasonId = recardReason.id;
                            var obj = ent.recard_reason.Where(o => o.id == recardReasonId).FirstOrDefault();
                            if (obj != null)
                            {
                                obj.is_deleted = true;
                                ent.SaveChanges();

                                return apiResponse(new responseSuccessDeleteRecord());
                            }
                            else return apiResponse(new responseFailedUpdateRecord { message = "No record changed" });
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return apiResponse(new responseFailedSystemError { obj = ex.Message });
            }
        }

        #endregion

        public IHttpActionResult apiResponse(object objResp)
        {
            var resp = (dynamic)null;

            if (objResp is response) resp = (response)objResp;
            else if (objResp is responseSuccess) resp = (responseSuccess)objResp;
            else if (objResp is responseSuccessNewRecord) resp = (responseSuccessNewRecord)objResp;
            else if (objResp is responseFailedNewRecord) resp = (responseFailedNewRecord)objResp;
            else if (objResp is responseSuccessUpdateRecord) resp = (responseSuccessUpdateRecord)objResp;
            else if (objResp is responseFailedUpdateRecord) resp = (responseFailedUpdateRecord)objResp;
            else if (objResp is responseSuccessDeleteRecord) resp = (responseSuccessDeleteRecord)objResp;
            else if (objResp is responseFailedDeleteRecord) resp = (responseFailedDeleteRecord)objResp;
            else if (objResp is responseFailedDuplicateRecord) resp = (responseFailedDuplicateRecord)objResp;
            else if (objResp is responseFailedBadRequest) resp = (responseFailedBadRequest)objResp;
            else if (objResp is responseFailedUnauthorized) resp = (responseFailedUnauthorized)objResp;

            return Ok(resp);
        }

    }
}
