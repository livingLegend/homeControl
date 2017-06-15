﻿using System;
using System.Linq;
using System.Reflection;

namespace homeControl.ClientServerShared
{
    public sealed class ApiCaller<TApi> where TApi: class
    {
        private readonly TApi _api;

        public ApiCaller(TApi api)
        {
            Guard.DebugAssertArgumentNotNull(api, nameof(api));

            _api = api;
        }

        public ApiResponse Call(ApiRequest request)
        {
            Guard.DebugAssertArgumentNotNull(request, nameof(request));

            try
            {
                return CallImpl(request);
            }
            catch (Exception ex)
            {
                return new ApiResponse(request.RequestId, ResponseCode.Failure, ex.ToString());
            }
        }

        private static readonly MethodInfo[] _apiMethods
            = typeof(TApi)
                .GetTypeInfo()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .ToArray();

        private ApiResponse CallImpl(ApiRequest request)
        {
            Guard.DebugAssertArgumentNotNull(request, nameof(request));

            if (request.ApiName != typeof(TApi).FullName)
            {
                throw new NotSupportedException("Requested API is not supported.");
            }

            var method = _apiMethods
                .Where(m => m.Name == request.MethodName)
                .Single(m => ParametersAreMatch(m, request.Parameters));

            var returnValue = method.Invoke(_api, request.Parameters.Select(p => p.ParameterValue).ToArray());
            return new ApiResponse(request.RequestId, ResponseCode.Success, returnValue);
        }

        private static bool ParametersAreMatch(MethodInfo method, ApiRequestParameter[] requestParameters)
        {
            Guard.DebugAssertArgumentNotNull(method, nameof(method));
            Guard.DebugAssertArgumentNotNull(requestParameters, nameof(requestParameters));

            var methodParameters = method.GetParameters();
            if (methodParameters.Length != requestParameters.Length)
                return false;

            for (var idx = 0; idx < methodParameters.Length; idx++)
            {
                var methodParam = methodParameters[idx];
                var requestParam = requestParameters[idx];

                if (methodParam.ParameterType != requestParam.GetActualType())
                    return false;
            }

            return true;
        }
    }
}