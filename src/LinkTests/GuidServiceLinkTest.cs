﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tavis;
using Xunit;

namespace LinkTests
{
    public class GuidServiceLinkTest
    {

        [Fact]
        public void TestLink()
        {
            var link = new GuidServiceLink()
                {
                    Target = new Uri("http://localhost/guidservice")
                };
            var httpClient = new HttpClient();
            httpClient.FollowLinkAsync(link);
            
        }

        [Fact]
        public void Throws_exception_if_response_not_OK()
        {
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var httpClient = new HttpClient(new FakeHandler
            {
                Response = response,
                InnerHandler = new HttpClientHandler()
            });
              var machine = new HttpResponseMachine();
              machine.AddResponseHandler((l, r) => { throw new Exception(); }, HttpStatusCode.BadRequest); 

            var task = httpClient.FollowLinkAsync(new GuidServiceLink(),machine);
            
            Assert.Throws<AggregateException>(() => task.Wait());
            
        }

        [Fact]
        public async Task Returns_content_if_response_is_OK()
        {
            string content = Guid.NewGuid().ToString();
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(content);

            var httpClient = new HttpClient(new FakeHandler
            {
                Response = response,
                InnerHandler = new HttpClientHandler()
            });

            var link = new GuidServiceLink();
            Guid guid = Guid.Empty;
            var machine = new HttpResponseMachine();
            machine.AddResponseHandler(async (lr, r) =>
            {
                {
                    
                    guid = Guid.Parse( r.Content.ReadAsStringAsync().Result);
                    return r;
                }
            }, HttpStatusCode.OK);


            await httpClient.FollowLinkAsync(link,machine);
            
            Assert.Equal(content, guid.ToString());



        }
    
    
    }

    
    public class FakeHandler : DelegatingHandler
    {
        public HttpResponseMessage Response { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                               CancellationToken cancellationToken)
        {
            if (Response == null)
            {
                return base.SendAsync(request, cancellationToken);
            }
            Response.RequestMessage = request;
            return Task.Factory.StartNew(() => Response);
        }
    }

    public class GuidServiceLink : Link
    {
        public GuidServiceLink()
        {

            Target = new Uri("http://localhost/guidservice");

        }

        public string Guid { get; set; }
    }
}
