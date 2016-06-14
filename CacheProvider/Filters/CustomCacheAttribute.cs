using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using CacheProvider.ConfigSections;
using CacheProvider.Services.Cache;
using Newtonsoft.Json;

namespace CacheProvider.Filters
{
    public class CustomCacheAttribute : ActionFilterAttribute
    {
        private readonly string _queryParams, _formParams;
        private readonly int _duration;
        private readonly bool _getAdditionalData;
        private readonly ICacheService _cacheService;
        public CustomCacheAttribute(string cacheClass, string queryParams, string formParams,
            bool getAdditionalData = true)
        {
            _queryParams = queryParams;
            _formParams = formParams;
            _getAdditionalData = getAdditionalData;
            _cacheService = _cacheService.Resolve();
            _duration = GetDurationByClassName(cacheClass);
        }

        private int GetDurationByClassName(string cacheClass)
        {
            CacheSettings cacheSettings =
                ConfigurationManager.GetSection("CacheSettings") as CacheSettings;
            if (cacheSettings == null)
            {
                throw new ConfigurationErrorsException($"Missing {nameof(CacheSettings)} configuration");
            }
            foreach (CacheClass item in cacheSettings.CacheClasses.Cast<CacheClass>().Where(item => item.Name == cacheClass))
            {
                return item.Duration;
            }
            throw new ConfigurationErrorsException($"Cache class {_cacheService} is not found");
        }

        protected virtual bool ValidateObject(string output, AvailableContentType contentType)
        {
            return true;
        }

        protected virtual string GetAdditionalData(HttpActionContext actionContext)
        {
            return string.Empty;
        }

        private AvailableContentType GetContentType(HttpRequestHeaders headers)
        {
            List<string> mediaTypes = headers.Accept.Select(x => x.MediaType).ToList();
            if (mediaTypes.Any(x => x.ToLower().Contains("json")))
            {
                return AvailableContentType.Json;
            }
            return mediaTypes.Any(x => x.ToLower().Contains("xml")) ? AvailableContentType.Xml : AvailableContentType.Json;
        }

        private string GetCustomKey(HttpActionContext actionContext, AvailableContentType contentType)
        {
            string controllerName = actionContext.ControllerContext.ControllerDescriptor.ControllerName;
            string actionName = actionContext.ActionDescriptor.ActionName;
            string result = $"{controllerName}.{actionName}.{contentType}.{GetQueryParams(actionContext)}|{GetFormParams(actionContext)}";
            if (_getAdditionalData)
            {
                result += $"|{GetAdditionalData(actionContext)}";
            }
            return result;
        }

        private string GetFormParams(HttpActionContext actionContext)
        {
            if (string.IsNullOrWhiteSpace(_formParams)) return string.Empty;
            IEnumerable<string> paramsAsList;
            List<string> formParamas = _formParams.Split(';').ToList();
            Dictionary<string, object> ctxFormParams = actionContext.ActionArguments;
            ctxFormParams = ctxFormParams.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            if (formParamas.Any(x => x == "*"))
            {
                paramsAsList = ctxFormParams.Select(x => x.Key + "=" + JsonConvert.SerializeObject(x.Value));
            }
            else
            {
                paramsAsList = ctxFormParams.Where(x => formParamas.Any(fp => x.Key == fp)).Select(x => x.Key + "=" + JsonConvert.SerializeObject(x.Value));
            }
            string result = string.Join("<>", paramsAsList);
            byte[] bytes = Encoding.UTF8.GetBytes(result);
            return Convert.ToBase64String(bytes);
        }

        private string GetQueryParams(HttpActionContext actionContext)
        {
            if (string.IsNullOrWhiteSpace(_queryParams)) return string.Empty;
            List<string> allParams;
            List<string> queryParams = _queryParams.Split(';').ToList();
            Dictionary<string, string> queryString = actionContext.Request.GetQueryNameValuePairs().OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            if (queryParams.Any(x => x == "*"))
            {
                allParams = queryString.Select(x => $"{x.Key}={x.Value}").ToList();
            }
            else
            {
                allParams = queryString.Where(x => queryParams.Any(qp => qp == x.Key)).Select(x => $"{x.Key}={x.Value}").ToList();
            }
            return string.Join("<>", allParams);
        }

        private Action<HttpActionExecutedContext> Callback { set; get; }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }
            AvailableContentType contentType = GetContentType(actionContext.Request.Headers);
            string customKey = GetCustomKey(actionContext, contentType);
            if (_cacheService.IsSet(customKey))
            {
                string content = _cacheService.Get<string>(customKey);
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.OK, content);
                actionContext.Response.Content = new StringContent(content);
                switch (contentType)
                {
                    case AvailableContentType.Json:
                        actionContext.Response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                        break;
                    case AvailableContentType.Xml:
                        actionContext.Response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                return;
            }
            Callback = actionExecutedContext =>
            {
                string output = actionExecutedContext.Response.Content.ReadAsStringAsync().Result;
                if (!actionExecutedContext.Response.IsSuccessStatusCode) return;
                if (ValidateObject(output, contentType))
                {
                    _cacheService.Set(customKey, output, _duration);
                }
            };
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext == null)
            {
                throw new ArgumentNullException(nameof(actionExecutedContext));
            }
            Callback(actionExecutedContext);
        }
    }

    public enum AvailableContentType
    {
        Json,
        Xml
    }
}
