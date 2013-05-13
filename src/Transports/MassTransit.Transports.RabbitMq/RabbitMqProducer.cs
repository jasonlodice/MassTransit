// Copyright 2007-2012 Chris Patterson, Dru Sellers, Travis Smith, et. al.
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
namespace MassTransit.Transports.RabbitMq
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using RabbitMQ.Client;


    public class RabbitMqProducer :
        ConnectionBinding<RabbitMqConnection>
    {
        readonly IRabbitMqEndpointAddress _address;
        readonly bool _bindToQueue;
        readonly object _channelLock = new object();
        readonly HashSet<ExchangeBinding> _exchangeBindings;
        readonly HashSet<string> _exchanges;
        IModel _channel;

        public RabbitMqProducer(IRabbitMqEndpointAddress address, bool bindToQueue)
        {
            _address = address;
            _bindToQueue = bindToQueue;
            _exchangeBindings = new HashSet<ExchangeBinding>();
            _exchanges = new HashSet<string>();
        }

        public void Bind(RabbitMqConnection connection)
        {
            lock (_channelLock)
            {
                IModel channel = null;
                try
                {
                    channel = connection.Connection.CreateModel();

                    DeclareAndBindQueue(channel);

                    RebindExchanges(channel);

                    _channel = channel;
                }
                catch (Exception ex)
                {
                    if (channel != null)
                    {
                        try
                        {
                            channel.Close(500, ex.Message);
                        }
                        catch
                        {
                        }
                        channel.Dispose();
                    }

                    throw new InvalidConnectionException(_address.Uri, "Invalid connection to host", ex);
                }
            }
        }

        public void Unbind(RabbitMqConnection connection)
        {
            lock (_channelLock)
            {
                if (_channel != null)
                {
                    if (_channel.IsOpen)
                        _channel.Close(200, "producer unbind");
                    _channel.Dispose();
                    _channel = null;
                }
            }
        }

        public void ExchangeDeclare(string name)
        {
            lock (_exchangeBindings)
                _exchanges.Add(name);
        }

        public void ExchangeBind(string destination, string source)
        {
            var binding = new ExchangeBinding(destination, source);

            lock (_exchangeBindings)
                _exchangeBindings.Add(binding);
        }

        void DeclareAndBindQueue(IModel channel)
        {
            channel.ExchangeDeclare(_address.Name, ExchangeType.Fanout, true);

            if (_bindToQueue)
            {
                string queue = channel.QueueDeclare(_address.Name, true, false, false, _address.QueueArguments());

                channel.QueueBind(queue, _address.Name, "");
            }
        }

        void RebindExchanges(IModel channel)
        {
            lock (_exchangeBindings)
            {
                IEnumerable<string> exchanges = _exchangeBindings.Select(x => x.Destination)
                                                                 .Concat(_exchangeBindings.Select(x => x.Source))
                                                                 .Concat(_exchanges)
                                                                 .Distinct();

                foreach (string exchange in exchanges)
                {
                    channel.ExchangeDeclare(exchange, ExchangeType.Fanout, true, false, null);
                }

                foreach (ExchangeBinding exchange in _exchangeBindings)
                {
                    channel.ExchangeBind(exchange.Destination, exchange.Source, "");
                }
            }
        }

        public IBasicProperties CreateProperties()
        {
            lock (_channelLock)
            {
                if (_channel == null)
                    throw new InvalidConnectionException(_address.Uri, "Channel should not be null");

                return _channel.CreateBasicProperties();
            }
        }

        public void Publish(string exchangeName, IBasicProperties properties, byte[] body)
        {
            lock (_channelLock)
            {
                if (_channel == null)
                    throw new InvalidConnectionException(_address.Uri, "No connection to RabbitMQ Host");

                _channel.BasicPublish(exchangeName, "", properties, body);
            }
        }
    }
}