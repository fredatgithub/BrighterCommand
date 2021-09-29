﻿using System;
using System.Collections.Generic;
using FluentAssertions;
using Paramore.Brighter.Core.Tests.CommandProcessors.TestDoubles;
using Paramore.Brighter.ServiceActivator.TestHelpers;
using Polly;
using Polly.Registry;
using Xunit;

namespace Paramore.Brighter.Core.Tests.CommandProcessors
{
    [Collection("CommandProcessor")]
    public class CommandProcessorMissingOutMapperTests : IDisposable
    {
        private readonly CommandProcessor _commandProcessor;
        private readonly MyRequest _myRequest = new MyRequest();
        
        public CommandProcessorMissingOutMapperTests()
        {
             _myRequest.RequestValue = "Hello World";

            var messageMapperRegistry = new MessageMapperRegistry(new SimpleMessageMapperFactory((type) =>
            {
                if (type == typeof(MyResponseMessageMapper))
                    return new MyResponseMessageMapper();

                throw new ConfigurationException($"No mapper found for {type.Name}");
            }));
            
            messageMapperRegistry.Register<MyResponse, MyResponseMessageMapper>();

            var subscriberRegistry = new SubscriberRegistry();
            subscriberRegistry.Register<MyResponse, MyResponseHandler>();
            var handlerFactory = new TestHandlerFactorySync<MyResponse, MyResponseHandler>(() => new MyResponseHandler());

            var retryPolicy = Policy
                .Handle<Exception>()
                .Retry();

            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreaker(1, TimeSpan.FromMilliseconds(1));

            var replySubs = new List<Subscription>
            {
                new Subscription<MyResponse>()
            };

            _commandProcessor = new CommandProcessor(
                subscriberRegistry,
                handlerFactory,
                new InMemoryRequestContextFactory(),
                new PolicyRegistry
                {
                    {CommandProcessor.RETRYPOLICY, retryPolicy},
                    {CommandProcessor.CIRCUITBREAKER, circuitBreakerPolicy}
                },
                messageMapperRegistry,
                new InMemoryOutbox(),
                new FakeMessageProducer(),
                replySubs,
                responseChannelFactory: new InMemoryChannelFactory());
            
            PipelineBuilder<MyResponse>.ClearPipelineCache();
        }
           
        [Fact]
        public void When_Calling_A_Server_Via_The_Command_Processor_With_No_Out_Mapper()
        {
            var exception = Catch.Exception(() => _commandProcessor.Call<MyRequest, MyResponse>(_myRequest, 500));
            
            //should throw an exception as we require a mapper for the outgoing request 
            exception.Should().BeOfType<ArgumentOutOfRangeException>();
        }


        public void Dispose()
        {
            CommandProcessor.ClearExtServiceBus();
        }
  }
}
