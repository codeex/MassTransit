// Copyright 2007-2017 Chris Patterson, Dru Sellers, Travis Smith, et. al.
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
namespace MassTransit.AzureServiceBusTransport.Topology
{
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;
    using Builders;
    using GreenPipes;
    using Microsoft.ServiceBus.Messaging;
    using NewIdFormatters;


    /// <summary>
    /// Used to bind an exchange to the consuming queue's exchange
    /// </summary>
    public class SubscriptionConsumeTopologySpecification :
        IServiceBusConsumeTopologySpecification
    {
        static readonly INewIdFormatter _formatter = new ZBase32Formatter();
        readonly SubscriptionDescription _subscriptionDescription;
        readonly TopicDescription _topicDescription;

        public SubscriptionConsumeTopologySpecification(TopicDescription topicDescription, SubscriptionDescription subscriptionDescription)
        {
            _topicDescription = topicDescription;
            _subscriptionDescription = subscriptionDescription;
        }

        public IEnumerable<ValidationResult> Validate()
        {
            yield break;
        }

        public void Apply(IReceiveEndpointConsumeTopologyBuilder builder)
        {
            var topic = builder.CreateTopic(_topicDescription);

            var subscriptionDescription = _subscriptionDescription;

            subscriptionDescription.ForwardTo = builder.Queue.Queue.QueueDescription.Path;
            subscriptionDescription.Name = GetSubscriptionName(subscriptionDescription.Name, builder.Queue.Queue.QueueDescription.Path);

            var queueSubscription = builder.CreateQueueSubscription(topic, builder.Queue, subscriptionDescription);
        }

        static string GetSubscriptionName(string subscriptionName, string queuePath)
        {
            var subscriptionPath = subscriptionName.Replace("{queuePath}", queuePath);

            string name;
            if (subscriptionPath.Length > 50)
            {
                string hashed;
                using (var hasher = new SHA1Managed())
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(subscriptionPath);
                    byte[] hash = hasher.ComputeHash(buffer);
                    hashed = _formatter.Format(hash).Substring(0, 6);
                }

                name = $"{subscriptionPath.Substring(0, 43)}-{hashed}";
            }
            else
                name = subscriptionPath;

            return name;
        }
    }
}