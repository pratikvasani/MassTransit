// Copyright 2007-2016 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.Transports.InMemory
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using GreenPipes;
    using Pipeline;
    using Util;


    public class InMemoryPublishEndpointProvider :
        IPublishEndpointProvider
    {
        readonly PublishObservable _publishObservable;
        readonly IPublishPipe _publishPipe;
        readonly ISendEndpointProvider _sendEndpointProvider;
        readonly InMemoryTransportCache _transportCache;

        public InMemoryPublishEndpointProvider(ISendEndpointProvider sendEndpointProvider, ISendTransportProvider transportProvider, IPublishPipe publishPipe)
        {
            _sendEndpointProvider = sendEndpointProvider;
            _publishPipe = publishPipe;
            _transportCache = transportProvider as InMemoryTransportCache;
            _publishObservable = new PublishObservable();
        }

        public IPublishEndpoint CreatePublishEndpoint(Uri sourceAddress, Guid? correlationId, Guid? conversationId)
        {
            return new PublishEndpoint(sourceAddress, this, _publishObservable, _publishPipe, correlationId, conversationId);
        }

        public async Task<ISendEndpoint> GetPublishSendEndpoint(Type messageType)
        {
            if (!TypeMetadataCache.IsValidMessageType(messageType))
                throw new MessageException(messageType, "Anonymous types are not valid message types");

            ISendEndpoint[] result = await Task.WhenAll(_transportCache.TransportAddresses.Select(x => _sendEndpointProvider.GetSendEndpoint(x)))
                .ConfigureAwait(false);

            return new FanoutSendEndpoint(result);
        }

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
        {
            return _publishObservable.Connect(observer);
        }
    }
}