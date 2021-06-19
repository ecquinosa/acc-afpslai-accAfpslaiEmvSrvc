using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;

namespace accAfpslaiEmvSrvc.Models
{
    public class response
    {

        public int result { get; set; }
        public string message { get; set; }
        public object obj { get; set; }

        public response()
        {
            message = "";
            obj = null;
        }

    }

    public class responseSuccess
    {

        public int result { get; set; }
        public string message { get; set; }
        public object obj { get; set; }

        public responseSuccess()
        {
            result = 0;
            message = "Success";
            obj = null;
        }

    }

    public class responseSuccessNewRecord
    {

        public int result { get; set; }
        public string message { get; set; }
        public object obj { get; set; }

        public responseSuccessNewRecord()
        {
            result = 0;
            message = "New record added";
            obj = null;
        }

    }

    public class responseFailedNewRecord
    {

        public int result { get; set; }
        public string message { get; set; }
        public object obj { get; set; }

        public responseFailedNewRecord()
        {
            result = (int)HttpStatusCode.InternalServerError;
            message = "Failed to add record";
            obj = null;
        }
    }

    public class responseFailedDuplicateRecord
    {

        public int result { get; set; }
        public string message { get; set; }
        public object obj { get; set; }

        public responseFailedDuplicateRecord()
        {
            result = (int)HttpStatusCode.InternalServerError;
            message = "Duplicate entry is not allowed";
            obj = null;
        }
    }

    public class responseFailedBadRequest
    {

        public int result { get; set; }
        public string message { get; set; }
        public object obj { get; set; }

        public responseFailedBadRequest()
        {
            result = (int)HttpStatusCode.BadRequest;
            message = "Bad input parameter(s)";
            obj = null;
        }
    }

    public class responseFailedUnauthorized
    {

        public int result { get; set; }
        public string message { get; set; }
        public object obj { get; set; }

        public responseFailedUnauthorized()
        {
            result = (int)HttpStatusCode.Unauthorized;
            message = "Access denied";
            obj = null;
        }
    }

    public class responseFailedSystemError
    {

        public int result { get; set; }
        public string message { get; set; }
        public object obj { get; set; }

        public responseFailedSystemError()
        {
            result = (int)HttpStatusCode.InternalServerError;
            message = "System error";
            obj = null;
        }
    }

    public class responseSuccessUpdateRecord
    {

        public int result { get; set; }
        public string message { get; set; }
        public object obj { get; set; }

        public responseSuccessUpdateRecord()
        {
            result = 0;
            message = "Sucessfully updated record";
            obj = null;
        }

    }

    public class responseFailedUpdateRecord
    {

        public int result { get; set; }
        public string message { get; set; }
        public object obj { get; set; }

        public responseFailedUpdateRecord()
        {
            result = (int)HttpStatusCode.InternalServerError;
            message = "Failed to updated record";
            obj = null;
        }

    }

    public class responseSuccessDeleteRecord
    {

        public int result { get; set; }
        public string message { get; set; }
        public object obj { get; set; }

        public responseSuccessDeleteRecord()
        {
            result = 0;
            message = "Sucessfully deleted record";
            obj = null;
        }

    }

    public class responseFailedDeleteRecord
    {

        public int result { get; set; }
        public string message { get; set; }
        public object obj { get; set; }

        public responseFailedDeleteRecord()
        {
            result = (int)HttpStatusCode.InternalServerError;
            message = "Failed to delete record";
            obj = null;
        }

    }
}