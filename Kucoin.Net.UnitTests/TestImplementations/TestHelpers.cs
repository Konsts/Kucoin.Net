﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CryptoExchange.Net;
using CryptoExchange.Net.Interfaces;
using CryptoExchange.Net.Logging;
using Kucoin.Net.Clients.Rest.Futures;
using Kucoin.Net.Clients.Rest.Spot;
using Kucoin.Net.Enums;
using Kucoin.Net.Interfaces;
using Kucoin.Net.Interfaces.Clients.Rest.Spot;
using Kucoin.Net.Objects;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;

namespace Kucoin.Net.UnitTests.TestImplementations
{
    public class TestHelpers
    {
        [ExcludeFromCodeCoverage]
        public static bool AreEqual(object? self, object? to, params string[] ignore)
        {
            if (self == null && to == null)
                return true;

            if ((self != null && to == null) || (self == null && to != null))
                return false;

            if (self == null || to == null)
                throw new Exception("Null");

            var type = self.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                || typeof(IList).IsAssignableFrom(type))
            {
                var list = (IList)self;
                var listOther = (IList)to;
                for (int i = 0; i < list.Count; i++)
                {
                    if (!AreEqual(list[i], listOther[i]))
                        return false;
                }

                return true;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var dict = (IDictionary)self;
                var other = (IDictionary)to;
                var items1 = new List<DictionaryEntry>();
                var items2 = new List<DictionaryEntry>();
                foreach (DictionaryEntry item in dict)
                    items1.Add(item);
                foreach (DictionaryEntry item in other)
                    items2.Add(item);

                for (int i = 0; i < items1.Count; i++)
                {
                    if (!AreEqual(items1[i].Key, items2[i].Key))
                        return false;
                    if (!AreEqual(items1[i].Value, items2[i].Value))
                        return false;
                }

                return true;
            }


            if (type.IsValueType || type == typeof(string))
                return Equals(self, to);

            var ignoreList = new List<string>(ignore);
            foreach (var pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (ignoreList.Contains(pi.Name))
                    continue;

                var selfValue = type.GetProperty(pi.Name)!.GetValue(self, null);
                var toValue = type.GetProperty(pi.Name)!.GetValue(to, null);

                if (pi.PropertyType.IsValueType || pi.PropertyType == typeof(string))
                {
                    if (!Equals(selfValue, toValue))
                        return false;
                    continue;
                }

                if (pi.PropertyType.IsClass)
                {
                    if (!AreEqual(selfValue, toValue, ignore))
                        return false;
                    continue;
                }

                if (selfValue != toValue && (selfValue == null || !selfValue.Equals(toValue)))
                    return false;
            }

            return true;
        }

        public static IKucoinClientSpot CreateClient(KucoinClientSpotOptions? options = null)
        {
            IKucoinClientSpot client;
            client = options != null ? new KucoinClientSpot(options) : new KucoinClientSpot(new KucoinClientSpotOptions() { LogLevel = LogLevel.Debug });
            client.RequestFactory = Mock.Of<IRequestFactory>();
            return client;
        }

        public static IKucoinClientFutures CreateClient(KucoinClientFuturesOptions? options = null)
        {
            IKucoinClientFutures client;
            client = options != null ? new KucoinClientFutures(options) : new KucoinClientFutures(new KucoinClientFuturesOptions() { LogLevel = LogLevel.Debug });
            client.RequestFactory = Mock.Of<IRequestFactory>();
            return client;
        }


        public static IKucoinClientSpot CreateResponseClient(string response, KucoinClientSpotOptions? options = null)
        {
            var client = (KucoinClientSpot)CreateClient(options);
            SetResponse(client, response);
            return client;
        }

        public static IKucoinClientFutures CreateResponseClientFutures(string response, KucoinClientFuturesOptions? options = null)
        {
            var client = (KucoinClientFutures)CreateClient(options);
            SetResponseFutures(client, response);
            return client;
        }

        public static IKucoinClientSpot CreateResponseClient<T>(T response, KucoinClientSpotOptions? options = null)
        {
            var client = (KucoinClientSpot)CreateClient(options);
            SetResponse(client, JsonConvert.SerializeObject(response));
            return client;
        }

        public static void SetResponse(KucoinClientSpot client, string responseData, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var expectedBytes = Encoding.UTF8.GetBytes(responseData);
            var responseStream = new MemoryStream();
            responseStream.Write(expectedBytes, 0, expectedBytes.Length);
            responseStream.Seek(0, SeekOrigin.Begin);

            var response = new Mock<IResponse>();
            response.Setup(c => c.StatusCode).Returns(statusCode);
            response.Setup(c => c.IsSuccessStatusCode).Returns(statusCode == HttpStatusCode.OK);
            response.Setup(c => c.GetResponseStreamAsync()).Returns(Task.FromResult((Stream)responseStream));

            var request = new Mock<IRequest>();
            request.Setup(c => c.Uri).Returns(new Uri("http://www.test.com"));
            request.Setup(c => c.GetResponseAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(response.Object));

            var factory = Mock.Get(client.RequestFactory);
            factory.Setup(c => c.Create(It.IsAny<HttpMethod>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(request.Object);
        }

        public static void SetResponseFutures(KucoinClientFutures client, string responseData, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var expectedBytes = Encoding.UTF8.GetBytes(responseData);
            var responseStream = new MemoryStream();
            responseStream.Write(expectedBytes, 0, expectedBytes.Length);
            responseStream.Seek(0, SeekOrigin.Begin);

            var response = new Mock<IResponse>();
            response.Setup(c => c.StatusCode).Returns(statusCode);
            response.Setup(c => c.IsSuccessStatusCode).Returns(statusCode == HttpStatusCode.OK);
            response.Setup(c => c.GetResponseStreamAsync()).Returns(Task.FromResult((Stream)responseStream));

            var request = new Mock<IRequest>();
            request.Setup(c => c.Uri).Returns(new Uri("http://www.test.com"));
            request.Setup(c => c.GetResponseAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(response.Object));

            var factory = Mock.Get(client.RequestFactory);
            factory.Setup(c => c.Create(It.IsAny<HttpMethod>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(request.Object);
        }

        public static object? GetTestValue(Type type, int i)
        {
            if (type == typeof(bool))
                return true;

            if (type == typeof(bool?))
                return (bool?)true;

            if (type == typeof(decimal))
                return i / 100m;

            if (type == typeof(decimal?))
                return (decimal?)(i / 100m);

            if (type == typeof(int))
                return 20;

            if (type == typeof(int?))
                return (int?)i;

            if (type == typeof(long))
                return (long)i;

            if (type == typeof(long?))
                return (long?)i;

            if (type == typeof(DateTime))
                return new DateTime(2019, 1, Math.Max(i, 1));

            if (type == typeof(DateTime?))
                return (DateTime?)new DateTime(2019, 1, Math.Max(i, 1));

            if (type == typeof(string))
                return "ETH-BTC" + i;

            if (type == typeof(IEnumerable<string>))
                return new[] { "string" + i };

            if (type == typeof(StopCondition))
                return StopCondition.Entry;

            if (type.IsEnum)
            {
                return Activator.CreateInstance(type);
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType()!;
                var result = Array.CreateInstance(elementType, 2);
                result.SetValue(GetTestValue(elementType, 0), 0);
                result.SetValue(GetTestValue(elementType, 1), 1);
                return result;
            }

            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>)))
            {
                var result = (IList)Activator.CreateInstance(type)!;
                result.Add(GetTestValue(type.GetGenericArguments()[0], 0));
                result.Add(GetTestValue(type.GetGenericArguments()[0], 1));
                return result;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var result = (IDictionary)Activator.CreateInstance(type)!;
                result.Add(GetTestValue(type.GetGenericArguments()[0], 0)!, GetTestValue(type.GetGenericArguments()[1], 0));
                result.Add(GetTestValue(type.GetGenericArguments()[0], 1)!, GetTestValue(type.GetGenericArguments()[1], 1));
                return Convert.ChangeType(result, type);
            }

            return null;
        }

        public static async Task<object> InvokeAsync(MethodInfo @this, object obj, params object[] parameters)
        {
            var task = (Task)@this.Invoke(obj, parameters);
            await task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty.GetValue(task);
        }
    }
}
